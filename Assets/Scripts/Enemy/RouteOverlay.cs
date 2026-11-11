using System.Collections.Generic;
using Bunker.Simulation;
using UnityEngine;

// Shows the player where everybody will walk: the enemy routes (allies walk them too, backwards). While the player plans (preparation,
// dragging a card) they are chevrons moving towards the bunker with a ring on every enemy flag; during combat a thin still dashed line,
// because both sides walk it and arrows towards the bunker would read as allies going the wrong way.
public class RouteOverlay : MonoBehaviour
{
    const string LineMaterialPath = "RouteLine";
    const string RingMaterialPath = "EnemyFlagRing";
    const string FlagPrefabPath = "EnemyFlagStand";
    const string SortingLayer = "Background";
    const int SortingOrder = 11;
    const float EnemyHalfWidth = 0.1875f; // 3 art pixels

    // The dark tone of the game's palette, the only one that reads on the light ground
    static readonly Color EnemyColor = new Color32(34, 35, 35, 255);
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int DashedId = Shader.PropertyToID("_Dashed");

    GameObject visuals, rings, flagPrefab;
    MeshRenderer routeRenderer;
    MaterialPropertyBlock routeBlock;
    bool? planning;
    Material lineMaterial;
    readonly List<Mesh> meshes = new List<Mesh>();

    public static RouteOverlay Create(IEnumerable<EnemyMovement> routes)
    {
        var overlay = new GameObject("RouteOverlay").AddComponent<RouteOverlay>();
        overlay.Build(routes);
        return overlay;
    }

    void Build(IEnumerable<EnemyMovement> routes)
    {
        lineMaterial = Resources.Load<Material>(LineMaterialPath);
        var ringMaterial = Resources.Load<Material>(RingMaterialPath);
        if (lineMaterial == null || ringMaterial == null)
        {
            Debug.LogError($"RouteOverlay: missing Resources/{LineMaterialPath} or Resources/{RingMaterialPath} material.");
            enabled = false;
            return;
        }

        flagPrefab = Resources.Load<GameObject>(FlagPrefabPath);
        visuals = new GameObject("Visuals");
        visuals.transform.SetParent(transform, false);
        rings = new GameObject("Rings");
        rings.transform.SetParent(visuals.transform, false);

        var enemyMesh = CreateMeshObject("EnemyRoutes", EnemyColor);
        routeRenderer = visuals.transform.Find("EnemyRoutes").GetComponent<MeshRenderer>();
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        foreach (var route in routes)
        {
            var polyline = Polyline(route);
            RouteGeometry.AppendStrip(polyline, EnemyHalfWidth, vertices, uvs, triangles);
            AppendForkLinks(route, polyline, vertices, uvs, triangles);
            AddRings(route, ringMaterial);
        }
        Apply(enemyMesh, vertices, uvs, triangles);
    }

    static List<Vector3> Polyline(EnemyMovement route)
    {
        var polyline = new List<Vector3>();
        foreach (var (position, _) in route.BuildRoute())
            polyline.Add(position);
        return polyline;
    }

    // From the last flag of a forking route to the first flag of each branch
    static void AppendForkLinks(EnemyMovement route, IReadOnlyList<Vector3> polyline, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
    {
        if (!route.Forks || polyline.Count == 0) return;

        Vector3 fork = polyline[polyline.Count - 1];
        var link = new List<Vector3>();
        foreach (var branch in route.branches)
        {
            if (branch == null || branch.points == null || branch.points.Length == 0) continue;
            link.Clear();
            link.Add(fork);
            if (RouteGeometry.TryGetCorner(fork, branch.points[0], false, out Vector3 corner))
                link.Add(corner);
            link.Add(branch.points[0]);
            RouteGeometry.AppendStrip(link, EnemyHalfWidth, vertices, uvs, triangles);
        }
    }

    void AddRings(EnemyMovement route, Material ringMaterial)
    {
        if (route.points == null) return;
        int ringCount = route.Forks ? route.points.Length : route.points.Length - 1; // the bunker is no flag
        for (int i = 0; i < ringCount; i++)
        {
            var ringObject = new GameObject("EnemyFlagRing", typeof(MeshFilter), typeof(MeshRenderer));
            ringObject.transform.SetParent(rings.transform, false);
            Vector3 position = route.points[i];
            position.z = 0f;
            ringObject.transform.position = position;

            var ring = ringObject.AddComponent<DottedRing>();
            ringObject.GetComponent<MeshRenderer>().sharedMaterial = ringMaterial;
            ring.SetColor(EnemyColor);
            ring.SetRadius(route.flagRadius);

            // The white flag marks the enemy territory; the ring around it only shows while planning
            if (flagPrefab != null)
                Instantiate(flagPrefab, position, Quaternion.identity, transform);
        }
    }

    Mesh CreateMeshObject(string objectName, Color color)
    {
        var meshObject = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
        meshObject.transform.SetParent(visuals.transform, false);

        var mesh = new Mesh { name = objectName };
        meshes.Add(mesh);
        meshObject.GetComponent<MeshFilter>().sharedMesh = mesh;

        var meshRenderer = meshObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = lineMaterial;
        meshRenderer.sortingLayerName = SortingLayer;
        meshRenderer.sortingOrder = SortingOrder;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        var block = new MaterialPropertyBlock();
        block.SetColor(ColorId, color);
        meshRenderer.SetPropertyBlock(block);
        return mesh;
    }

    static void Apply(Mesh mesh, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
    {
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }

    void Update()
    {
        if (visuals == null || routeRenderer == null) return;
        bool plan = SimulationBridge.IsPreparationPhase || (GameManager.instance != null && GameManager.instance.onDrag);
        if (planning == plan) return;
        planning = plan;

        rings.SetActive(plan);
        routeBlock ??= new MaterialPropertyBlock();
        routeRenderer.GetPropertyBlock(routeBlock);
        routeBlock.SetFloat(DashedId, plan ? 0f : 1f);
        routeRenderer.SetPropertyBlock(routeBlock);
    }

    void OnDestroy()
    {
        foreach (var mesh in meshes)
            if (mesh != null) Destroy(mesh);
    }
}
