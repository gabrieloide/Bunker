using System.Collections.Generic;
using Bunker.Simulation;
using UnityEngine;
using UnityEngine.Tilemaps;

// An enemy route: the flags soldiers walk through, in order, to the bunker. A procedural route plans itself when the level
// loads: the shortest walk from the enemy spawner to the bunker door around the decoration and limits (trees, cliffs),
// turning as little as possible; its flags are the corners. In edit mode the gizmo previews it and follows every tile change.
// Runs before the SimulationBridge, which reads the route on Start.
[DefaultExecutionOrder(-600)]
public class EnemyMovement : MonoBehaviour
{
    public Color pathColor = new Color(0.15f, 0.85f, 1f, 1f);
    public float waypointRadius = 0.35f;
    public bool showLabels = true;

    [Header("Procedural route")]
    [Tooltip("Plan the shortest walk from start to end around the decoration and limits when the level loads. The points below are then ignored")]
    public bool procedural = true;
    [Tooltip("Where soldiers start; empty = the EnemySpawner")]
    public Transform start;
    [Tooltip("Where they end; empty = the AllySpawner (the bunker door)")]
    public Transform end;

    [Header("Route")]
    [Tooltip("Enemy flags in walking order; the last one is the bunker. Every flag is a circle the wave spreads over. Filled by the planner when procedural")]
    public Vector3[] points = new Vector3[0];
    public Vector3[] Points => points;
    [Tooltip("Radius of every enemy flag circle")]
    [Min(0f)] public float flagRadius = 0.5f;
    [Tooltip("Radius of the last point (the bunker): arriving enemies surround it instead of stacking on one spot")]
    [Min(0f)] public float bunkerRadius = 0.6f;
    [Tooltip("Between two flags that are not in line, walk vertically first instead of horizontally first")]
    public bool verticalFirst;
    [Tooltip("Routes this one forks into at its last flag: arriving soldiers take them in turn, so the group splits. Empty = the last point is the bunker")]
    public EnemyMovement[] branches = new EnemyMovement[0];

    public bool Forks => branches != null && branches.Length > 0;

    // Cells every procedural route walks: the path for placement (towers can't go there, mines only there)
    static readonly HashSet<Vector3Int> routeCells = new HashSet<Vector3Int>();
    public static bool HasProceduralRoute => routeCells.Count > 0;
    public static bool IsRouteCell(Vector3Int cell) => routeCells.Contains(cell);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRouteCells() => routeCells.Clear();

    List<Vector3Int> plannedCells;

    // The route as soldiers walk it: soldiers never move diagonally, so every pair of flags out of line gets an "L" corner between them
    public List<(Vector3 position, float radius)> BuildRoute()
    {
        var route = new List<(Vector3, float)>();
        if (points == null) return route;
        for (int i = 0; i < points.Length; i++)
        {
            if (i > 0 && RouteGeometry.TryGetCorner(points[i - 1], points[i], verticalFirst, out Vector3 corner))
                route.Add((corner, flagRadius));
            route.Add((points[i], i == points.Length - 1 && !Forks ? bunkerRadius : flagRadius));
        }
        return route;
    }
    public Vector3 CurrentPosition => _gameStarted ? _currentPosition : transform.position;
    Vector3 _currentPosition;
    bool _gameStarted;

    private void Awake()
    {
        _gameStarted = true;
        _currentPosition = transform.position;
        if (!procedural) return;

        if (TryPlan(out var cells, out var corners))
        {
            points = corners;
            plannedCells = cells;
            routeCells.UnionWith(cells);
        }
        else
        {
            Debug.LogError($"EnemyMovement '{name}': no walkable route from {StartPoint} to {EndPoint} around the decoration and limits; keeping the hand-placed points.", this);
        }
    }

    private void OnDestroy()
    {
        if (plannedCells != null)
            routeCells.ExceptWith(plannedCells);
    }

    private void Start()
    {
        _gameStarted = true;
        _currentPosition = transform.position;
    }

    public Vector3 GetPointsPosition(int index)
    {
        if (points == null || index < 0 || index >= points.Length)
            return CurrentPosition;
        return CurrentPosition + points[index];
    }

    Vector3 StartPoint
    {
        get
        {
            if (start != null) return start.position;
            var spawner = FindAnyObjectByType<EnemySpawner>();
            return spawner != null ? spawner.transform.position : transform.position;
        }
    }

    Vector3 EndPoint
    {
        get
        {
            if (end != null) return end.position;
            var door = FindAnyObjectByType<AllySpawner>();
            return door != null ? door.transform.position : transform.position;
        }
    }

    // Plans the walk; corners are world positions at cell centres, keeping the authored depth of the points
    bool TryPlan(out List<Vector3Int> cells, out Vector3[] corners)
    {
        cells = null;
        corners = null;
        PlacementGrid.CacheTilemaps();
        if (!PlacementGrid.Available) return false;

        var startCell = PlacementGrid.WorldToCell(StartPoint);
        var endCell = PlacementGrid.WorldToCell(EndPoint);
        var bounds = PlacementGrid.TilemapBounds();
        var min = Vector3Int.Min(Vector3Int.Min(bounds.min, startCell), endCell) - new Vector3Int(1, 1, 0);
        var max = Vector3Int.Max(Vector3Int.Max(bounds.max, startCell + Vector3Int.one), endCell + Vector3Int.one) + new Vector3Int(1, 1, 0);
        bounds.SetMinMax(new Vector3Int(min.x, min.y, startCell.z), new Vector3Int(max.x, max.y, startCell.z + 1));

        cells = RoutePlanner.FindPath(startCell, endCell, PlacementGrid.BlocksWalking, bounds);
        if (cells.Count == 0) return false;

        float depth = points != null && points.Length > 0 ? points[0].z : 0f;
        var cornerCells = RoutePlanner.Corners(cells);
        corners = new Vector3[cornerCells.Count];
        for (int i = 0; i < cornerCells.Count; i++)
        {
            var position = PlacementGrid.CellCenter(cornerCells[i]);
            position.z = depth;
            corners[i] = position;
        }
        return true;
    }

    // ------------------------------------------------ Edit-mode preview

    Vector3[] preview;
    bool previewFailed;
    bool previewDirty = true;
    Vector3 previewStart, previewEnd;
    int previewTilesVersion = -1;

    // Bumped by every tile painted or erased. Watched from a static editor hook: OnEnable never runs in edit mode
    // on a component without [ExecuteAlways], so a subscription there missed every brush stroke
    static int tilesVersion;

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    static void WatchTiles()
    {
        Tilemap.tilemapTileChanged -= OnAnyTilesChanged;
        Tilemap.tilemapTileChanged += OnAnyTilesChanged;
        // Undo/redo restores tiles without raising tilemapTileChanged
        UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
        UnityEditor.Undo.undoRedoPerformed += OnUndoRedo;
    }

    static void OnUndoRedo() => OnAnyTilesChanged(null, null);

    static void OnAnyTilesChanged(Tilemap tilemap, Tilemap.SyncTile[] tiles)
    {
        tilesVersion++;
        UnityEditor.SceneView.RepaintAll();
    }
#endif

    void OnValidate() => previewDirty = true;

    // What the gizmo draws: the live route in play mode, the planner's preview in edit mode
    Vector3[] GizmoPoints()
    {
        if (Application.isPlaying || !procedural)
            return points;

        Vector3 startNow = StartPoint, endNow = EndPoint;
        if (previewDirty || previewTilesVersion != tilesVersion || startNow != previewStart || endNow != previewEnd)
        {
            previewDirty = false;
            previewTilesVersion = tilesVersion;
            previewStart = startNow;
            previewEnd = endNow;
            previewFailed = !TryPlan(out _, out preview);
        }
        return previewFailed ? null : preview;
    }

    private void OnDrawGizmos()
    {
        var drawn = GizmoPoints();
        if (procedural && !Application.isPlaying && previewFailed)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(previewStart, previewEnd);
#if UNITY_EDITOR
            var style = new GUIStyle { fontStyle = FontStyle.Bold, fontSize = 12 };
            style.normal.textColor = Color.red;
            UnityEditor.Handles.Label((previewStart + previewEnd) * 0.5f, "NO ROUTE: the decoration closes every way", style);
#endif
            return;
        }
        if (drawn == null || drawn.Length == 0) return;

        // Procedural points are world positions already; hand-placed ones hang from the object
        Vector3 origin = procedural ? Vector3.zero : CurrentPosition;

        for (int i = 0; i < drawn.Length; i++)
        {
            Vector3 worldPos = origin + drawn[i];
            if (i < drawn.Length - 1 || Forks)
            {
                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
                Gizmos.DrawWireSphere(worldPos, flagRadius);
            }
            if (i == drawn.Length - 1 && Forks)
            {
                Gizmos.color = pathColor;
                foreach (var branch in branches)
                    if (branch != null && branch.points != null && branch.points.Length > 0)
                        Gizmos.DrawLine(worldPos, branch.CurrentPosition + branch.points[0]);
            }

            // Color-code start, waypoints, and end (bunker)
            if (i == 0)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f); // Green for Spawner/Start
                Gizmos.DrawSphere(worldPos, waypointRadius * 1.3f);
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(worldPos, waypointRadius * 1.6f);
            }
            else if (i == drawn.Length - 1)
            {
                Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.95f); // Red for Bunker/End
                Gizmos.DrawSphere(worldPos, waypointRadius * 1.4f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(worldPos, waypointRadius * 1.7f);
            }
            else
            {
                Gizmos.color = pathColor;
                Gizmos.DrawSphere(worldPos, waypointRadius);
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(worldPos, waypointRadius * 1.15f);
            }

            // Draw line to next point
            if (i < drawn.Length - 1)
            {
                Vector3 nextPos = origin + drawn[i + 1];
                Gizmos.color = pathColor;
                Gizmos.DrawLine(worldPos, nextPos);

                // Draw directional marker along line
                Vector3 midPoint = (worldPos + nextPos) * 0.5f;
                Vector3 dir = (nextPos - worldPos).normalized;
                Vector3 perp = new Vector3(-dir.y, dir.x, 0f) * 0.25f;
                Gizmos.color = Color.white;
                Gizmos.DrawLine(midPoint - dir * 0.3f + perp, midPoint);
                Gizmos.DrawLine(midPoint - dir * 0.3f - perp, midPoint);
            }

#if UNITY_EDITOR
            if (showLabels)
            {
                string label = i == 0 ? $"{gameObject.name} [START]" :
                               i == drawn.Length - 1 ? $"{gameObject.name} [END]" :
                               $"{i}";
                var style = new GUIStyle();
                style.normal.textColor = (i == 0) ? Color.green : (i == drawn.Length - 1) ? Color.red : Color.cyan;
                style.fontStyle = FontStyle.Bold;
                style.fontSize = 11;
                UnityEditor.Handles.Label(worldPos + Vector3.up * 0.5f, label, style);
            }
#endif
        }
    }

    private void OnDrawGizmosSelected()
    {
        var drawn = GizmoPoints();
        if (drawn == null || drawn.Length < 2) return;
#if UNITY_EDITOR
        Vector3 origin = procedural ? Vector3.zero : CurrentPosition;
        Vector3[] worldPoints = new Vector3[drawn.Length];
        for (int i = 0; i < drawn.Length; i++)
            worldPoints[i] = origin + drawn[i];

        UnityEditor.Handles.color = pathColor;
        UnityEditor.Handles.DrawAAPolyLine(6f, worldPoints);
#endif
    }
}
