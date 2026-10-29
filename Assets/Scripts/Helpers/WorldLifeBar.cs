using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Life bar over a world object, drawn on the game UI panel (see GameUIPanel)
public class WorldLifeBar : MonoBehaviour
{
    [Tooltip("Centre of the bar relative to this object, in world units")]
    [SerializeField] Vector3 offset;
    [Tooltip("Frame size in world units")]
    [SerializeField] Vector2 size = new Vector2(1f, 0.1875f);
    [Tooltip("Gap between the frame and the fill, in world units (x: left and right, y: top and bottom)")]
    [SerializeField] Vector2 fillInset = new Vector2(0.06f, 0.04f);
    [SerializeField] Sprite frame;
    [SerializeField] Color fillColor = Color.black;

    VisualElement bar, track, fill;
    float target01 = 1f;
    float current01 = 1f;
    bool initialized = false;
    const float FillSpeed = 2.5f;

    private class ActiveChunk
    {
        public VisualElement element;
        public float elapsed;
        public float sign;
    }

    private readonly List<ActiveChunk> activeChunks = new();
    private int chunkCounter = 0;
    private const float ChunkDuration = 0.45f;
    private const float Phase1Duration = 0.15f;
    private float currentTrackHeight = 16f;

    public void SetValue(float current, float max)
    {
        float target = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (!initialized)
        {
            initialized = true;
            target01 = target;
            current01 = target;
            if (fill != null)
                fill.style.width = Length.Percent(current01 * 100f);
            return;
        }

        if (target < current01 - 0.0005f)
        {
            float prevLife = current01;
            float newLife = target;
            float delta = prevLife - newLife;
            current01 = newLife;
            target01 = newLife;
            if (fill != null)
                fill.style.width = Length.Percent(current01 * 100f);
            SpawnDamageChunk(newLife, delta);
        }
        else
        {
            target01 = target;
        }
    }

    void OnEnable()
    {
        if (bar != null) bar.style.display = DisplayStyle.Flex;
    }

    void OnDisable()
    {
        if (bar != null) bar.style.display = DisplayStyle.None;
        ClearChunks();
    }

    void OnDestroy()
    {
        ClearChunks();
        bar?.RemoveFromHierarchy();
    }

    // Built lazily: scene objects can wake up before the panel does
    bool EnsureBar()
    {
        if (bar != null) return true;
        var panel = GameUIPanel.Instance;
        if (panel == null) return false;

        bar = new VisualElement { pickingMode = PickingMode.Ignore };
        bar.AddToClassList("world-bar");
        if (frame != null) bar.style.backgroundImage = new StyleBackground(frame);

        track = new VisualElement { pickingMode = PickingMode.Ignore };
        track.AddToClassList("world-bar__track");
        bar.Add(track);

        fill = new VisualElement { pickingMode = PickingMode.Ignore };
        fill.AddToClassList("world-bar__fill");
        fill.style.backgroundColor = fillColor;
        track.Add(fill);

        panel.World.Add(bar);
        current01 = target01;
        fill.style.width = Length.Percent(current01 * 100f);
        return true;
    }

    void SpawnDamageChunk(float startLeft01, float width01)
    {
        if (track == null && !EnsureBar()) return;
        if (track == null || width01 <= 0.001f) return;

        var chunk = new VisualElement { pickingMode = PickingMode.Ignore };
        chunk.AddToClassList("world-bar__chunk");
        chunk.style.position = Position.Absolute;
        chunk.style.left = Length.Percent(Mathf.Clamp01(startLeft01) * 100f);
        chunk.style.width = Length.Percent(Mathf.Clamp01(width01) * 100f);
        chunk.style.top = 0f;
        chunk.style.bottom = 0f;
        chunk.style.height = Length.Percent(100f);
        chunk.style.backgroundColor = fillColor;
        chunk.style.borderLeftColor = new Color(0.941f, 0.965f, 0.941f, 1f); // #F0F6F0
        chunk.style.borderRightColor = new Color(0.941f, 0.965f, 0.941f, 1f);
        chunk.style.borderTopColor = new Color(0.941f, 0.965f, 0.941f, 1f);
        chunk.style.borderBottomColor = new Color(0.941f, 0.965f, 0.941f, 1f);
        chunk.style.borderLeftWidth = 1f;
        chunk.style.borderRightWidth = 1f;
        chunk.style.borderTopWidth = 1f;
        chunk.style.borderBottomWidth = 1f;
        chunk.style.transformOrigin = new StyleTransformOrigin(new TransformOrigin(Length.Percent(50), Length.Percent(50)));

        track.Add(chunk);

        float sign = (chunkCounter++ % 2 == 0) ? 1f : -1f;
        activeChunks.Add(new ActiveChunk
        {
            element = chunk,
            elapsed = 0f,
            sign = sign
        });
    }

    void UpdateChunks()
    {
        if (activeChunks.Count == 0) return;

        float phase1Drop = Mathf.Max(6f, currentTrackHeight * 0.45f);
        float extraDropMax = Mathf.Max(6f, currentTrackHeight * 0.50f);

        for (int i = activeChunks.Count - 1; i >= 0; i--)
        {
            var chunk = activeChunks[i];
            chunk.elapsed += Time.deltaTime;
            float t = chunk.elapsed;

            if (t >= ChunkDuration)
            {
                chunk.element.RemoveFromHierarchy();
                activeChunks.RemoveAt(i);
                continue;
            }

            if (t < Phase1Duration)
            {
                float p1 = t / Phase1Duration;
                float easeY = 1f - Mathf.Pow(1f - p1, 2f);
                float y = phase1Drop * easeY;

                float osc = Mathf.Sin(t * 48f);
                float amp = Mathf.Lerp(11f, 7f, p1);
                float angle = osc * amp * chunk.sign;

                chunk.element.style.translate = new Translate(0f, y);
                chunk.element.style.rotate = new Rotate(Angle.Degrees(angle));
                chunk.element.style.opacity = 1f;
                chunk.element.style.scale = new Scale(Vector2.one);
            }
            else
            {
                float p2 = (t - Phase1Duration) / (ChunkDuration - Phase1Duration);
                float extra = extraDropMax * (p2 * p2);
                float y = phase1Drop + extra;

                float decay = 1f - p2;
                float osc = Mathf.Sin(t * 32f);
                float jitterAmp = Mathf.Lerp(5f, 1f, p2);
                float baseTilt = 3.5f * chunk.sign;
                float angle = (osc * jitterAmp + baseTilt) * decay;

                float opacity = Mathf.Clamp01(1f - p2);
                float scaleY = Mathf.Clamp01(1f - p2 * 0.75f);

                chunk.element.style.translate = new Translate(0f, y);
                chunk.element.style.rotate = new Rotate(Angle.Degrees(angle));
                chunk.element.style.opacity = opacity;
                chunk.element.style.scale = new Scale(new Vector2(1f, scaleY));
            }
        }
    }

    void ClearChunks()
    {
        for (int i = 0; i < activeChunks.Count; i++)
        {
            activeChunks[i].element.RemoveFromHierarchy();
        }
        activeChunks.Clear();
    }

    void Update()
    {
        if (fill != null && !Mathf.Approximately(current01, target01))
        {
            current01 = Mathf.MoveTowards(current01, target01, FillSpeed * Time.deltaTime);
            fill.style.width = Length.Percent(current01 * 100f);
        }
        UpdateChunks();
    }

    void LateUpdate()
    {
        if (!EnsureBar()) return;
        var panel = GameUIPanel.Instance;
        float unit = panel.UnitsPerWorld;
        Vector2 centre = panel.WorldToPanel(transform.position + offset);
        bar.style.left = centre.x - size.x * unit * 0.5f;
        bar.style.top = centre.y - size.y * unit * 0.5f;
        bar.style.width = size.x * unit;
        bar.style.height = size.y * unit;
        bar.style.paddingLeft = bar.style.paddingRight = fillInset.x * unit;
        bar.style.paddingTop = bar.style.paddingBottom = fillInset.y * unit;
        currentTrackHeight = Mathf.Max(1f, (size.y - fillInset.y * 2f) * unit);
    }
}
