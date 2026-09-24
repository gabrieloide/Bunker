using UnityEngine;

// Darkens the ground outside the flags' territory while a card is being dragged.
// Each flag paints its circle into a mask with a soft edge centred on its radius; overlapping flags
// keep the highest value per pixel, so their circles merge into one area without seams.
[RequireComponent(typeof(SpriteRenderer))]
public class TerritoryOverlay : MonoBehaviour
{
    [Tooltip("World area covered by the overlay")]
    [SerializeField] Rect worldBounds = new Rect(-8f, -18f, 72f, 46f);
    [SerializeField] int pixelsPerUnit = 16;
    [Tooltip("Width of the fade at the territory border, in overlay pixels")]
    [SerializeField] float softEdgePixels = 16f;
    // Dark tone of the game's two-colour palette
    [SerializeField] Color outsideColor = new Color32(34, 35, 35, 255);
    [Range(0f, 1f)] [SerializeField] float outsideAlpha = 0.55f;
    [SerializeField] float fadeSpeed = 8f;

    SpriteRenderer spriteRenderer;
    Texture2D texture;
    float[] mask;
    Color32[] pixels;
    bool dirty = true;
    float visibility;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        int width = Mathf.Max(1, Mathf.CeilToInt(worldBounds.width * pixelsPerUnit));
        int height = Mathf.Max(1, Mathf.CeilToInt(worldBounds.height * pixelsPerUnit));
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "TerritoryMask"
        };
        mask = new float[width * height];
        pixels = new Color32[width * height];

        transform.position = new Vector3(worldBounds.xMin, worldBounds.yMin, transform.position.z);
        spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.zero, pixelsPerUnit);
        SetVisibility(0f);
    }

    void OnEnable() => FlagTerritory.Changed += MarkDirty;
    void OnDisable() => FlagTerritory.Changed -= MarkDirty;

    void OnDestroy()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null) Destroy(spriteRenderer.sprite);
        if (texture != null) Destroy(texture);
    }

    void MarkDirty() => dirty = true;

    void LateUpdate()
    {
        if (dirty)
        {
            dirty = false;
            Rebuild();
        }

        bool dragging = GameManager.instance != null && GameManager.instance.onDrag && FlagTerritory.Flags.Count > 0;
        float target = dragging ? 1f : 0f;
        if (!Mathf.Approximately(visibility, target))
            SetVisibility(Mathf.MoveTowards(visibility, target, fadeSpeed * Time.unscaledDeltaTime));
    }

    void SetVisibility(float value)
    {
        visibility = value;
        spriteRenderer.enabled = visibility > 0f;
        var tint = Color.white;
        tint.a = visibility * outsideAlpha;
        spriteRenderer.color = tint;
    }

    void Rebuild()
    {
        System.Array.Clear(mask, 0, mask.Length);
        int width = texture.width;
        int height = texture.height;
        float halfEdge = softEdgePixels * 0.5f;

        foreach (var flag in FlagTerritory.Flags)
        {
            if (flag == null) continue;
            Vector2 center = (flag.Center - worldBounds.min) * pixelsPerUnit;
            float radius = flag.Radius * pixelsPerUnit;
            float reach = radius + halfEdge;

            int xMin = Mathf.Max(0, Mathf.FloorToInt(center.x - reach));
            int xMax = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + reach));
            int yMin = Mathf.Max(0, Mathf.FloorToInt(center.y - reach));
            int yMax = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + reach));

            for (int y = yMin; y <= yMax; y++)
            {
                float dy = y + 0.5f - center.y;
                int row = y * width;
                for (int x = xMin; x <= xMax; x++)
                {
                    float dx = x + 0.5f - center.x;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float value = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(radius - halfEdge, radius + halfEdge, distance));
                    int i = row + x;
                    if (value > mask[i]) mask[i] = value;
                }
            }
        }

        Color32 outside = outsideColor;
        for (int i = 0; i < mask.Length; i++)
        {
            outside.a = (byte)Mathf.RoundToInt((1f - mask[i]) * 255f);
            pixels[i] = outside;
        }
        texture.SetPixels32(pixels);
        texture.Apply(false);
    }
}
