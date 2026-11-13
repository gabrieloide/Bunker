using System;
using System.Collections.Generic;
using Bunker.Simulation;
using UnityEngine;

// Tracks player-built ally road cells on the PlacementGrid, enforces the per-card and per-level
// tile budgets, and validates monochrome orthogonal L-segments within FlagTerritory.
public static class AllyRoadNetwork
{
    static readonly HashSet<Vector3Int> roadCells = new HashSet<Vector3Int>();
    static readonly List<List<Vector3Int>> segments = new List<List<Vector3Int>>();

    static Vector3Int bunkerCell;
    static Vector3 bunkerWorldPos;
    static int maxTotalTiles = 12;
    static bool initialized;

    public static event Action Changed;

    public static bool Initialized => initialized;
    public static int BuiltTileCount => roadCells.Count;
    public static int MaxTotalTiles => maxTotalTiles;
    public static int RemainingTiles => maxTotalTiles > 0 ? Mathf.Max(0, maxTotalTiles - roadCells.Count) : int.MaxValue;
    public static Vector3Int BunkerCell => bunkerCell;
    public static Vector3 BunkerWorldPosition => bunkerWorldPos;
    public static IReadOnlyCollection<Vector3Int> RoadCells => roadCells;
    public static IReadOnlyList<List<Vector3Int>> Segments => segments;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        roadCells.Clear();
        segments.Clear();
        bunkerCell = Vector3Int.zero;
        bunkerWorldPos = Vector3.zero;
        maxTotalTiles = 12;
        initialized = false;
        Changed = null;
    }

    public static void Clear()
    {
        roadCells.Clear();
        segments.Clear();
        initialized = false;
        Changed?.Invoke();
    }

    public static void Initialize(Vector3 bunkerPosition, int levelMaxTiles)
    {
        roadCells.Clear();
        segments.Clear();
        bunkerWorldPos = bunkerPosition;
        bunkerCell = PlacementGrid.Available ? PlacementGrid.WorldToCell(bunkerPosition) : Vector3Int.RoundToInt(bunkerPosition);
        maxTotalTiles = Mathf.Max(0, levelMaxTiles);
        initialized = true;
        Changed?.Invoke();
    }

    public static bool IsRoadCell(Vector3Int cell) => roadCells.Contains(cell);

    public static bool TryPlanSegment(
        Vector3 targetWorldPos,
        int maxCardTiles,
        out List<Vector3Int> plannedCells,
        out Vector3Int anchorCell)
    {
        Vector3Int targetCell = PlacementGrid.Available
            ? PlacementGrid.WorldToCell(targetWorldPos)
            : Vector3Int.RoundToInt(targetWorldPos);

        return AllyRoadPlanner.TryPlanSegment(
            bunkerCell,
            roadCells,
            targetCell,
            maxCardTiles,
            RemainingTiles,
            IsCellAllowedInScene,
            out plannedCells,
            out anchorCell);
    }

    public static bool CommitSegment(IReadOnlyList<Vector3Int> cells)
    {
        if (cells == null || cells.Count == 0)
            return false;
        if (maxTotalTiles > 0 && roadCells.Count + cells.Count > maxTotalTiles)
            return false;

        var copy = new List<Vector3Int>(cells.Count);
        for (int i = 0; i < cells.Count; i++)
        {
            if (roadCells.Add(cells[i]))
                copy.Add(cells[i]);
        }

        if (copy.Count == 0)
            return false;

        segments.Add(copy);
        Changed?.Invoke();
        return true;
    }

    public static List<Vector3> GetMainPolylineWorld()
    {
        var result = new List<Vector3>();
        if (segments.Count == 0)
            return result;

        result.Add(CellToWorld(bunkerCell));
        for (int s = 0; s < segments.Count; s++)
        {
            var seg = segments[s];
            for (int i = 0; i < seg.Count; i++)
                result.Add(CellToWorld(seg[i]));
        }
        return result;
    }

    public static Vector3 CellToWorld(Vector3Int cell) =>
        PlacementGrid.Available ? PlacementGrid.CellCenter(cell) : new Vector3(cell.x, cell.y, 0f);

    static bool IsCellAllowedInScene(Vector3Int cell)
    {
        if (PlacementGrid.Available)
        {
            if (!PlacementGrid.HasGround(cell)) return false;
            if (PlacementGrid.BlocksWalking(cell)) return false;
            if (PlacementGrid.IsOccupied(cell)) return false;
        }

        Vector3 world = CellToWorld(cell);
        if (!FlagTerritory.Contains(world))
            return false;
        if (EnemyTerritory.Contains(world))
            return false;

        return true;
    }
}
