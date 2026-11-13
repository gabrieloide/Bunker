using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Routes UI Toolkit UIDocument panels into screen-sized RenderTextures during PlayMode
// so RetroCRTFilter applies curvature, scanlines, phosphor glow, and vignette to menus and HUD too.
public class RetroCRTUIBridge : MonoBehaviour
{
    public static RetroCRTUIBridge Instance { get; private set; }

    class BoundPanelState
    {
        public PanelSettings panel;
        public RenderTexture rt;
        public bool originalClearColor;
        public Color originalClearColorValue;
        public bool hasActiveDoc;
    }

    readonly List<BoundPanelState> boundPanels = new List<BoundPanelState>();
    Material compositeMat;
    float curvatureCache = 0.05f;

    Material CompositeMat
    {
        get
        {
            if (compositeMat == null)
            {
                var shader = Shader.Find("Hidden/AlphaComposite");
                if (shader != null)
                {
                    compositeMat = new Material(shader);
                    compositeMat.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            return compositeMat;
        }
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(this);
    }

    void Start()
    {
        Refresh();
    }

    void OnDestroy()
    {
        ReleaseAll();
        if (Instance == this)
            Instance = null;
    }

    void OnDisable()
    {
        ReleaseAll();
    }

    void LateUpdate()
    {
        if (!Application.isPlaying) return;

        if (GlobalRetroCRT.Instance != null)
            curvatureCache = GlobalRetroCRT.Instance.curvature;

        Refresh();
    }

    public void Refresh()
    {
        if (!Application.isPlaying) return;
        BindActiveUIDocuments();
        EnsureRenderTextures();
    }

    void EnsureRenderTextures()
    {
        int w = Mathf.Max(1, Screen.width);
        int h = Mathf.Max(1, Screen.height);

        for (int i = 0; i < boundPanels.Count; i++)
        {
            var state = boundPanels[i];
            if (state.panel == null) continue;

            if (state.rt == null || state.rt.width != w || state.rt.height != h)
            {
                if (state.rt != null)
                {
                    if (state.panel.targetTexture == state.rt)
                        state.panel.targetTexture = null;
                    state.rt.Release();
                    Destroy(state.rt);
                }

                state.rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
                {
                    name = "RetroCRT_UI_RT_" + state.panel.name,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                state.rt.Create();
            }

            if (state.panel.targetTexture != state.rt)
                state.panel.targetTexture = state.rt;
        }
    }

    void BindActiveUIDocuments()
    {
        for (int i = 0; i < boundPanels.Count; i++)
            boundPanels[i].hasActiveDoc = false;

        var docs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (var doc in docs)
        {
            if (doc == null || !doc.isActiveAndEnabled) continue;

            var ps = doc.panelSettings;
            if (ps == null) continue;

            BoundPanelState state = null;
            for (int i = 0; i < boundPanels.Count; i++)
            {
                if (boundPanels[i].panel == ps)
                {
                    state = boundPanels[i];
                    break;
                }
            }

            if (state == null)
            {
                state = new BoundPanelState
                {
                    panel = ps,
                    originalClearColor = ps.clearColor,
                    originalClearColorValue = ps.colorClearValue
                };
                ps.clearColor = true;
                ps.colorClearValue = Color.clear;
                ps.SetScreenToPanelSpaceFunction(MapScreenToPanel);
                boundPanels.Add(state);
            }

            state.hasActiveDoc = true;
        }

        boundPanels.Sort((a, b) =>
        {
            float oa = a.panel != null ? a.panel.sortingOrder : 0f;
            float ob = b.panel != null ? b.panel.sortingOrder : 0f;
            return oa.CompareTo(ob);
        });
    }

    Vector2 MapScreenToPanel(Vector2 screenPos)
    {
        float w = Mathf.Max(1f, Screen.width);
        float h = Mathf.Max(1f, Screen.height);
        Vector2 uv = new Vector2(screenPos.x / w, screenPos.y / h);

        if (curvatureCache > 0.0001f)
        {
            Vector2 centered = uv * 2f - Vector2.one;
            float denom = curvatureCache * 15f + 8f;
            Vector2 offset = new Vector2(centered.y / denom, centered.x / denom);
            centered += new Vector2(centered.x * offset.x * offset.x, centered.y * offset.y * offset.y);
            uv = centered * 0.5f + new Vector2(0.5f, 0.5f);
        }

        if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
            return new Vector2(float.NaN, float.NaN);

        return new Vector2(uv.x * w, uv.y * h);
    }

    public bool TryComposite(RenderTexture source, out RenderTexture composited)
    {
        composited = null;
        if (!Application.isPlaying || CompositeMat == null)
            return false;

        RenderTexture current = source;
        RenderTexture tempA = null;
        RenderTexture tempB = null;

        for (int i = 0; i < boundPanels.Count; i++)
        {
            var state = boundPanels[i];
            if (!state.hasActiveDoc || state.panel == null || state.rt == null)
                continue;

            RenderTexture dst = (current == tempA)
                ? (tempB ??= RenderTexture.GetTemporary(source.width, source.height, 0, source.format))
                : (tempA ??= RenderTexture.GetTemporary(source.width, source.height, 0, source.format));

            CompositeMat.SetTexture("_OverlayTex", state.rt);
            Graphics.Blit(current, dst, CompositeMat);
            current = dst;
        }

        if (current == source)
            return false;

        if (current == tempA && tempB != null)
            RenderTexture.ReleaseTemporary(tempB);
        else if (current == tempB && tempA != null)
            RenderTexture.ReleaseTemporary(tempA);

        composited = current;
        return true;
    }

    void ReleaseAll()
    {
        for (int i = 0; i < boundPanels.Count; i++)
        {
            var state = boundPanels[i];
            if (state.panel == null) continue;
            if (state.panel.targetTexture == state.rt)
                state.panel.targetTexture = null;
            state.panel.clearColor = state.originalClearColor;
            state.panel.colorClearValue = state.originalClearColorValue;
            state.panel.SetScreenToPanelSpaceFunction(null);

            if (state.rt != null)
            {
                state.rt.Release();
                if (Application.isPlaying) Destroy(state.rt);
                else DestroyImmediate(state.rt);
                state.rt = null;
            }
        }
        boundPanels.Clear();

        if (compositeMat != null)
        {
            if (Application.isPlaying) Destroy(compositeMat);
            else DestroyImmediate(compositeMat);
            compositeMat = null;
        }
    }
}
