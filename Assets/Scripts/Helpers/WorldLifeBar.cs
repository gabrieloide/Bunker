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

    VisualElement bar, fill;
    float value01 = 1f;

    public void SetValue(float current, float max)
    {
        value01 = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        if (fill != null) fill.style.width = Length.Percent(value01 * 100f);
    }

    void OnEnable()
    {
        if (bar != null) bar.style.display = DisplayStyle.Flex;
    }

    void OnDisable()
    {
        if (bar != null) bar.style.display = DisplayStyle.None;
    }

    void OnDestroy() => bar?.RemoveFromHierarchy();

    // Built lazily: scene objects can wake up before the panel does
    bool EnsureBar()
    {
        if (bar != null) return true;
        var panel = GameUIPanel.Instance;
        if (panel == null) return false;

        bar = new VisualElement { pickingMode = PickingMode.Ignore };
        bar.AddToClassList("world-bar");
        if (frame != null) bar.style.backgroundImage = new StyleBackground(frame);
        fill = new VisualElement { pickingMode = PickingMode.Ignore };
        fill.AddToClassList("world-bar__fill");
        fill.style.backgroundColor = fillColor;
        bar.Add(fill);
        panel.World.Add(bar);
        SetValue(value01, 1f);
        return true;
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
    }
}
