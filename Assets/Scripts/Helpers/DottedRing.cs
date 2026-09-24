using UnityEngine;

// Quad rendered with the Bunker/DottedRing shader. Radius and color go through a property block,
// so every ring shares one material.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DottedRing : MonoBehaviour
{
    static readonly int RadiusId = Shader.PropertyToID("_Radius");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int MergeId = Shader.PropertyToID("_MergeWithFlags");

    [SerializeField] Material material;
    // Dark tone of the game's two-colour palette
    [SerializeField] Color color = new Color32(34, 35, 35, 255);
    [SerializeField] string sortingLayer = "Background";
    [SerializeField] int sortingOrder = 10;
    [Tooltip("Hide the parts of the ring that fall inside another flag's radius (flag rings only)")]
    [SerializeField] bool mergeWithFlags;

    MeshRenderer meshRenderer;
    MaterialPropertyBlock block;
    float radius = 1f;

    void Awake()
    {
        GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        meshRenderer = GetComponent<MeshRenderer>();
        if (material != null) meshRenderer.sharedMaterial = material;
        meshRenderer.sortingLayerName = sortingLayer;
        meshRenderer.sortingOrder = sortingOrder;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        block = new MaterialPropertyBlock();
        Apply();
    }

    public void SetRadius(float value)
    {
        radius = Mathf.Max(0f, value);
        // Quad covers the ring plus one pixel of slack; the shader measures from the origin
        float size = radius * 2f + 0.25f;
        transform.localScale = new Vector3(size, size, 1f);
        Apply();
    }

    public void SetColor(Color value)
    {
        color = value;
        Apply();
    }

    void Apply()
    {
        if (meshRenderer == null) return;
        block.SetFloat(RadiusId, radius);
        block.SetColor(ColorId, color);
        block.SetFloat(MergeId, mergeWithFlags ? 1f : 0f);
        meshRenderer.SetPropertyBlock(block);
    }
}
