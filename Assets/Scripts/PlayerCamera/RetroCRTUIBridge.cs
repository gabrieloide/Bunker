using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Routes UI Toolkit UIDocument panels into a screen-sized RenderTexture during PlayMode
// so RetroCRTFilter applies curvature, scanlines, phosphor glow, and vignette to menus too.
public class RetroCRTUIBridge : MonoBehaviour
{
    public static RetroCRTUIBridge Instance { get; private set; }

    struct BoundPanelState
    {
        public PanelSettings panel;
        public bool originalClearColor;
        public Color originalClearColorValue;
    }

    readonly List<BoundPanelState> boundPanels = new List<BoundPanelState>();
    readonly List<UIDocument> activeDocs = new List<UIDocument>();

    RenderTexture uiRT;
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

        EnsureRenderTexture();
        BindActiveUIDocuments();
    }

    public void Refresh()
    {
        if (!Application.isPlaying) return;
        EnsureRenderTexture();
        BindActiveUIDocuments();
    }

    void EnsureRenderTexture()
    {
        int w = Mathf.Max(1, Screen.width);
        int h = Mathf.Max(1, Screen.height);
        if (uiRT != null && uiRT.width == w && uiRT.height == h)
            return;

        if (uiRT != null)
        {
            foreach (var state in boundPanels)
            {
                if (state.panel != null && state.panel.targetTexture == uiRT)
                    state.panel.targetTexture = null;
            }
            uiRT.Release();
            Destroy(uiRT);
        }

        uiRT = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
        {
            name = "RetroCRT_UI_RT",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        uiRT.Create();

        foreach (var state in boundPanels)
        {
            if (state.panel != null)
                state.panel.targetTexture = uiRT;
        }
    }

    void BindActiveUIDocuments()
    {
        activeDocs.Clear();
        var docs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (var doc in docs)
        {
            if (doc == null || !doc.isActiveAndEnabled) continue;
            activeDocs.Add(doc);

            var ps = doc.panelSettings;
            if (ps == null) continue;

            bool alreadyBound = false;
            for (int i = 0; i < boundPanels.Count; i++)
            {
                if (boundPanels[i].panel == ps)
                {
                    alreadyBound = true;
                    break;
                }
            }

            if (!alreadyBound)
            {
                boundPanels.Add(new BoundPanelState
                {
                    panel = ps,
                    originalClearColor = ps.clearColor,
                    originalClearColorValue = ps.colorClearValue
                });
                ps.clearColor = true;
                ps.colorClearValue = Color.clear;
                ps.SetScreenToPanelSpaceFunction(MapScreenToPanel);
            }

            if (ps.targetTexture != uiRT)
                ps.targetTexture = uiRT;
        }
    }

    Vector2 MapScreenToPanel(Vector2 screenPos)
    {
        if (uiRT == null) return screenPos;

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

        return new Vector2(uv.x * uiRT.width, uv.y * uiRT.height);
    }

    public bool TryComposite(RenderTexture source, out RenderTexture composited)
    {
        composited = null;
        if (!Application.isPlaying || uiRT == null || CompositeMat == null)
            return false;

        bool anyActive = false;
        for (int i = 0; i < activeDocs.Count; i++)
        {
            if (activeDocs[i] != null && activeDocs[i].isActiveAndEnabled)
            {
                anyActive = true;
                break;
            }
        }
        if (!anyActive)
            return false;

        composited = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
        CompositeMat.SetTexture("_OverlayTex", uiRT);
        Graphics.Blit(source, composited, CompositeMat);
        return true;
    }

    void ReleaseAll()
    {
        for (int i = 0; i < boundPanels.Count; i++)
        {
            var state = boundPanels[i];
            if (state.panel == null) continue;
            if (state.panel.targetTexture == uiRT)
                state.panel.targetTexture = null;
            state.panel.clearColor = state.originalClearColor;
            state.panel.colorClearValue = state.originalClearColorValue;
            state.panel.SetScreenToPanelSpaceFunction(null);
        }
        boundPanels.Clear();
        activeDocs.Clear();

        if (uiRT != null)
        {
            uiRT.Release();
            if (Application.isPlaying) Destroy(uiRT);
            else DestroyImmediate(uiRT);
            uiRT = null;
        }

        if (compositeMat != null)
        {
            if (Application.isPlaying) Destroy(compositeMat);
            else DestroyImmediate(compositeMat);
            compositeMat = null;
        }
    }
}
