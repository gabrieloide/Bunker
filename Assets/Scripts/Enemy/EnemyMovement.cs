using System.Collections;
using System.Collections.Generic;
using Bunker.Simulation;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public Color pathColor = new Color(0.15f, 0.85f, 1f, 1f);
    public float waypointRadius = 0.35f;
    public bool showLabels = true;

    [Tooltip("Enemy flags in walking order; the last one is the bunker. Every flag is a circle the wave spreads over")]
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

    // Cells every route walks (its flags and the straight legs between them): the path for placement, so towers can't go
    // there and mines only can. Follows the drawn route; the old Path tilemap no longer has to be painted to match
    static readonly HashSet<Vector3Int> routeCells = new HashSet<Vector3Int>();
    public static bool HasRoute => routeCells.Count > 0;
    public static bool IsRouteCell(Vector3Int cell) => routeCells.Contains(cell);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRouteCells() => routeCells.Clear();

    List<Vector3Int> registeredCells;

    // Every cell the soldiers walk through, flag to flag along the L legs (including outgoing fork links)
    public List<Vector3Int> RouteCells()
    {
        var cells = new List<Vector3Int>();
        if (!PlacementGrid.Available) return cells;
        var route = BuildRoute();
        for (int i = 0; i < route.Count; i++)
        {
            var to = PlacementGrid.WorldToCell(route[i].position);
            if (i == 0)
            {
                cells.Add(to);
                continue;
            }
            AppendLegCells(PlacementGrid.WorldToCell(route[i - 1].position), to, cells);
        }

        if (Forks && route.Count > 0)
        {
            Vector3 fork = route[route.Count - 1].position;
            Vector3Int forkCell = PlacementGrid.WorldToCell(fork);
            foreach (var branch in branches)
            {
                if (branch == null || branch.points == null || branch.points.Length == 0) continue;
                Vector3 branchStart = branch.points[0];
                if (RouteGeometry.TryGetCorner(fork, branchStart, false, out Vector3 corner))
                {
                    Vector3Int cornerCell = PlacementGrid.WorldToCell(corner);
                    AppendLegCells(forkCell, cornerCell, cells);
                    AppendLegCells(cornerCell, PlacementGrid.WorldToCell(branchStart), cells);
                }
                else
                {
                    AppendLegCells(forkCell, PlacementGrid.WorldToCell(branchStart), cells);
                }
            }
        }
        return cells;
    }

    static void AppendLegCells(Vector3Int at, Vector3Int to, List<Vector3Int> cells)
    {
        while (at.x != to.x) { at.x += System.Math.Sign(to.x - at.x); cells.Add(at); }
        while (at.y != to.y) { at.y += System.Math.Sign(to.y - at.y); cells.Add(at); }
    }

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
        registeredCells = RouteCells();
        routeCells.UnionWith(registeredCells);
    }

    private void OnDestroy()
    {
        if (registeredCells != null)
            routeCells.ExceptWith(registeredCells);
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
        return points[index];
    }

    private void OnDrawGizmos()
    {
        if (points == null || points.Length == 0) return;

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 worldPos = points[i];
            if (i < points.Length - 1 || Forks)
            {
                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
                Gizmos.DrawWireSphere(worldPos, flagRadius);
            }
            if (i == points.Length - 1 && Forks)
            {
                Gizmos.color = pathColor;
                foreach (var branch in branches)
                {
                    if (branch == null || branch.points == null || branch.points.Length == 0) continue;
                    Vector3 branchStart = branch.points[0];
                    Vector3 legStart = worldPos;
                    if (RouteGeometry.TryGetCorner(worldPos, branchStart, false, out Vector3 corner))
                    {
                        Gizmos.DrawLine(worldPos, corner);
                        legStart = corner;
                    }
                    Gizmos.DrawLine(legStart, branchStart);
                }
            }

            // Color-code start, waypoints, and end (bunker)
            if (i == 0)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f); // Green for Spawner/Start
                Gizmos.DrawSphere(worldPos, waypointRadius * 1.3f);
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(worldPos, waypointRadius * 1.6f);
            }
            else if (i == points.Length - 1)
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
            if (i < points.Length - 1)
            {
                Vector3 nextPos = points[i + 1];
                Gizmos.color = pathColor;
                // Soldiers never walk diagonally: draw the L they take
                Vector3 legStart = worldPos;
                if (RouteGeometry.TryGetCorner(worldPos, nextPos, verticalFirst, out Vector3 corner))
                {
                    Gizmos.DrawLine(worldPos, corner);
                    legStart = corner;
                }
                Gizmos.DrawLine(legStart, nextPos);

                // Direction marker on the leg that reaches the next flag
                Vector3 midPoint = (legStart + nextPos) * 0.5f;
                Vector3 dir = (nextPos - legStart).normalized;
                Vector3 perp = new Vector3(-dir.y, dir.x, 0f) * 0.25f;
                Gizmos.color = Color.white;
                Gizmos.DrawLine(midPoint - dir * 0.3f + perp, midPoint);
                Gizmos.DrawLine(midPoint - dir * 0.3f - perp, midPoint);
            }

#if UNITY_EDITOR
            if (showLabels)
            {
                string label = i == 0 ? $"{gameObject.name} [START]" :
                               i == points.Length - 1 ? $"{gameObject.name} [END]" :
                               $"{i}";
                var style = new GUIStyle();
                style.normal.textColor = (i == 0) ? Color.green : (i == points.Length - 1) ? Color.red : Color.cyan;
                style.fontStyle = FontStyle.Bold;
                style.fontSize = 11;
                UnityEditor.Handles.Label(worldPos + Vector3.up * 0.5f, label, style);
            }
#endif
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (points == null || points.Length < 2) return;
#if UNITY_EDITOR
        UnityEditor.Handles.color = pathColor;
        UnityEditor.Handles.DrawAAPolyLine(6f, points);
#endif
    }
}
