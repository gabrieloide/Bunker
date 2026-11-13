using System.Collections.Generic;
using Bunker.Simulation;
using UnityEngine;

// Renders player-built ally roads and the live monochrome drag preview using the project's
// black-and-white RouteLine material (no colors: dark ink #222323 for built roads, white dashed for preview).
public class AllyRoadOverlay : MonoBehaviour
{
    const string LineMaterialPath = "RouteLine";
    const string SortingLayer = "Background";
    const int SortingOrder = 12;
    const float DefaultHalfWidth = 0.16f;

    static readonly Color BuiltColor = new Color32(34, 35, 35, 255);
    static readonly Color PreviewColor = Color.white;
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int DashedId = Shader.PropertyToID("_Dashed");

    public static AllyRoadOverlay Instance { get; private set; }

    Material lineMaterial;
    Mesh builtMesh;
    Mesh previewMesh;
    GameObject previewObject;
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<Vector2> uvs = new List<Vector2>();
    readonly List<int> triangles = new List<int>();

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

        builtMesh = CreateLayer("BuiltAllyRoads", BuiltColor, dashed: true, SortingOrder);
        previewMesh = CreateLayer("AllyRoadPreview", PreviewColor, dashed: true, SortingOrder + 1, out previewObject);
        previewObject.SetActive(false);

        AllyRoadNetwork.Changed += RebuildBuilt;
        RebuildBuilt();
    }

    void OnDestroy()
    {
        AllyRoadNetwork.Changed -= RebuildBuilt;
        if (Instance == this) Instance = null;
        if (builtMesh != null) Destroy(builtMesh);
        if (previewMesh != null) Destroy(previewMesh);
    }

    public void ShowPreview(Vector3Int anchorCell, IReadOnlyList<Vector3Int> plannedCells, float halfWidth = DefaultHalfWidth)
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

        var polyline = new List<Vector3>(plannedCells.Count + 1) { AllyRoadNetwork.CellToWorld(anchorCell) };
        for (int i = 0; i < plannedCells.Count; i++)
            polyline.Add(AllyRoadNetwork.CellToWorld(plannedCells[i]));

        RouteGeometry.AppendStrip(polyline, halfWidth, vertices, uvs, triangles);
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
        if (builtMesh == null) return;

        vertices.Clear();
        uvs.Clear();
        triangles.Clear();

        var segments = AllyRoadNetwork.Segments;
        for (int s = 0; s < segments.Count; s++)
        {
            var seg = segments[s];
            if (seg.Count == 0) continue;

            // Connect from the nearest prior cell (bunker or earlier segment) so corners render seamlessly
            Vector3Int anchor = FindSegmentAnchor(seg[0], s);
            var polyline = new List<Vector3>(seg.Count + 1) { AllyRoadNetwork.CellToWorld(anchor) };
            for (int i = 0; i < seg.Count; i++)
                polyline.Add(AllyRoadNetwork.CellToWorld(seg[i]));

            RouteGeometry.AppendStrip(polyline, DefaultHalfWidth, vertices, uvs, triangles);
        }

        ApplyMesh(builtMesh, vertices, uvs, triangles);
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

    Mesh CreateLayer(string layerName, Color color, bool dashed, int order)
    {
        return CreateLayer(layerName, color, dashed, order, out _);
    }

    Mesh CreateLayer(string layerName, Color color, bool dashed, int order, out GameObject layerObject)
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
        block.SetFloat(DashedId, dashed ? 1f : 0f);
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
