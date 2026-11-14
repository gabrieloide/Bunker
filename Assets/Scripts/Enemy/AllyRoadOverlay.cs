using System.Collections.Generic;
using Bunker.Simulation;
using UnityEngine;

// Renders player-built ally roads and the live monochrome drag preview using the project's
// black-and-white RouteLine material with a lateral lane offset so ally roads stay legible
// even when sharing terrain with enemy routes (solid dark backing + outward white chevrons).
public class AllyRoadOverlay : MonoBehaviour
{
    const string LineMaterialPath = "RouteLine";
    const string SortingLayer = "Background";
    const int BackingSortingOrder = 12;
    const int BuiltSortingOrder = 13;
    const int PreviewSortingOrder = 14;
    const float LaneOffset = 0.14f;
    const float BackingHalfWidth = 0.1875f;
    const float InnerHalfWidth = 0.125f;

    static readonly Color BackingColor = new Color32(34, 35, 35, 255);
    static readonly Color BuiltColor = new Color32(240, 246, 240, 255);
    static readonly Color PreviewColor = Color.white;
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int DashedId = Shader.PropertyToID("_Dashed");
    static readonly int LengthId = Shader.PropertyToID("_Length");
    static readonly int SpacingId = Shader.PropertyToID("_Spacing");
    static readonly int SpeedId = Shader.PropertyToID("_Speed");

    public static AllyRoadOverlay Instance { get; private set; }

    Material lineMaterial;
    Mesh backingMesh;
    Mesh builtMesh;
    Mesh previewMesh;
    GameObject previewObject;
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<Vector2> uvs = new List<Vector2>();
    readonly List<int> triangles = new List<int>();
    readonly List<Vector3> polyline = new List<Vector3>();

    public static AllyRoadOverlay EnsureExists()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("AllyRoadOverlay");
        return go.AddComponent<AllyRoadOverlay>();
    }

    void Awake()
    {
        Instance = this;
        lineMaterial = Resources.Load<Material>(LineMaterialPath);
        if (lineMaterial == null)
        {
            Debug.LogError($"AllyRoadOverlay: missing Resources/{LineMaterialPath} material.");
            enabled = false;
            return;
        }

        backingMesh = CreateLayer("BuiltAllyRoadBacking", BackingColor, BackingSortingOrder, length: 16f, spacing: 8f);
        builtMesh = CreateLayer("BuiltAllyRoads", BuiltColor, BuiltSortingOrder, length: 2f, spacing: 8f, speed: 6f);
        previewMesh = CreateLayer("AllyRoadPreview", PreviewColor, PreviewSortingOrder, length: 3f, spacing: 8f, speed: 10f, out previewObject);
        previewObject.SetActive(false);

        AllyRoadNetwork.Changed += RebuildBuilt;
        RebuildBuilt();
    }

    void OnDestroy()
    {
        AllyRoadNetwork.Changed -= RebuildBuilt;
        if (Instance == this) Instance = null;
        if (backingMesh != null) Destroy(backingMesh);
        if (builtMesh != null) Destroy(builtMesh);
        if (previewMesh != null) Destroy(previewMesh);
    }

    public void ShowPreview(Vector3Int anchorCell, IReadOnlyList<Vector3Int> plannedCells, float halfWidth = InnerHalfWidth)
    {
        if (previewMesh == null || previewObject == null) return;
        if (plannedCells == null || plannedCells.Count == 0)
        {
            HidePreview();
            return;
        }

        vertices.Clear();
        uvs.Clear();
        triangles.Clear();
        polyline.Clear();

        polyline.Add(AllyRoadNetwork.CellToWorld(anchorCell));
        for (int i = 0; i < plannedCells.Count; i++)
            polyline.Add(AllyRoadNetwork.CellToWorld(plannedCells[i]));

        float previewHalfWidth = halfWidth > 0f ? Mathf.Min(halfWidth, InnerHalfWidth) : InnerHalfWidth;
        RouteGeometry.AppendStrip(polyline, previewHalfWidth, LaneOffset, vertices, uvs, triangles);
        ApplyMesh(previewMesh, vertices, uvs, triangles);
        previewObject.SetActive(true);
    }

    public void HidePreview()
    {
        if (previewObject != null && previewObject.activeSelf)
            previewObject.SetActive(false);
    }

    void RebuildBuilt()
    {
        if (backingMesh == null || builtMesh == null) return;

        RebuildBuiltLayer(backingMesh, BackingHalfWidth);
        RebuildBuiltLayer(builtMesh, InnerHalfWidth);
    }

    void RebuildBuiltLayer(Mesh mesh, float halfWidth)
    {
        vertices.Clear();
        uvs.Clear();
        triangles.Clear();

        var segments = AllyRoadNetwork.Segments;
        for (int s = 0; s < segments.Count; s++)
        {
            var seg = segments[s];
            if (seg.Count == 0) continue;

            // Connect from the nearest prior cell (bunker or earlier segment) toward the tip so chevrons point outward
            Vector3Int anchor = FindSegmentAnchor(seg[0], s);
            polyline.Clear();
            polyline.Add(AllyRoadNetwork.CellToWorld(anchor));
            for (int i = 0; i < seg.Count; i++)
                polyline.Add(AllyRoadNetwork.CellToWorld(seg[i]));

            RouteGeometry.AppendStrip(polyline, halfWidth, LaneOffset, vertices, uvs, triangles);
        }

        ApplyMesh(mesh, vertices, uvs, triangles);
    }

    static Vector3Int FindSegmentAnchor(Vector3Int firstCell, int segmentIndex)
    {
        Vector3Int best = AllyRoadNetwork.BunkerCell;
        int bestDist = Manhattan(best, firstCell);
        var segments = AllyRoadNetwork.Segments;
        for (int s = 0; s < segmentIndex; s++)
        {
            var prev = segments[s];
            for (int i = 0; i < prev.Count; i++)
            {
                int d = Manhattan(prev[i], firstCell);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = prev[i];
                }
            }
        }
        return best;
    }

    static int Manhattan(Vector3Int a, Vector3Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    Mesh CreateLayer(string layerName, Color color, int order, float length, float spacing, float? speed = null)
    {
        return CreateLayer(layerName, color, order, length, spacing, speed, out _);
    }

    Mesh CreateLayer(string layerName, Color color, int order, float length, float spacing, float? speed, out GameObject layerObject)
    {
        layerObject = new GameObject(layerName, typeof(MeshFilter), typeof(MeshRenderer));
        layerObject.transform.SetParent(transform, false);

        var mesh = new Mesh { name = layerName };
        layerObject.GetComponent<MeshFilter>().sharedMesh = mesh;

        var renderer = layerObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = lineMaterial;
        renderer.sortingLayerName = SortingLayer;
        renderer.sortingOrder = order;

        var block = new MaterialPropertyBlock();
        block.SetColor(ColorId, color);
        block.SetFloat(DashedId, 0f);
        block.SetFloat(LengthId, length);
        block.SetFloat(SpacingId, spacing);
        if (speed.HasValue)
            block.SetFloat(SpeedId, speed.Value);
        renderer.SetPropertyBlock(block);
        return mesh;
    }

    static void ApplyMesh(Mesh mesh, List<Vector3> verts, List<Vector2> uvList, List<int> tris)
    {
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvList);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }
}
