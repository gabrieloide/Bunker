using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum PlacementStatus
{
    Valid,
    NoGround,
    Obstacle,
    Occupied
}

// Cell snapping + one-occupant-per-cell registry for card placement, backed by the scene's tilemap Grid.
// Optimized for zero physics colliders: detects valid ground and obstacles directly via Tilemap.HasTile.
public static class PlacementGrid
{
    static Grid grid;
    static readonly Dictionary<Vector3Int, GameObject> occupants = new Dictionary<Vector3Int, GameObject>();

    static Tilemap groundTilemap;
    static readonly List<Tilemap> obstacleTilemaps = new List<Tilemap>();
    // The enemy path: an obstacle for towers, the only valid ground for mines
    static readonly List<Tilemap> pathTilemaps = new List<Tilemap>();
    static bool tilemapsCached;

    public static Grid Grid
    {
        get
        {
            if (grid == null)
            {
                grid = Object.FindAnyObjectByType<Grid>();
                tilemapsCached = false;
            }
            return grid;
        }
    }

    public static bool Available => Grid != null;
    public static Vector2 CellSize => Grid != null ? (Vector2)Grid.cellSize : Vector2.one;

    public static Vector3Int WorldToCell(Vector3 world) => Grid != null ? Grid.WorldToCell(world) : Vector3Int.zero;

    public static Vector3 CellCenter(Vector3Int cell)
    {
        if (Grid == null) return Vector3.zero;
        Vector3 center = Grid.GetCellCenterWorld(cell);
        center.z = 0f;
        return center;
    }

    // ------------------------------------------------ Tilemap Caching & Setup
    public static void CacheTilemaps()
    {
        if (Grid == null) return;

        obstacleTilemaps.Clear();
        pathTilemaps.Clear();
        groundTilemap = null;

        var tilemaps = Grid.GetComponentsInChildren<Tilemap>(true);
        foreach (var tm in tilemaps)
        {
            string n = tm.gameObject.name.ToLowerInvariant();
            if (n.Contains("path") || n.Contains("limit") || n.Contains("decoration") || n.Contains("obstacle"))
            {
                obstacleTilemaps.Add(tm);
                if (n.Contains("path"))
                    pathTilemaps.Add(tm);
            }
            else if (n.Contains("background") || n.Contains("ground") || n.Contains("floor") || n.Contains("terrain"))
            {
                groundTilemap = tm;
            }
        }

        // If groundTilemap was not found by name, pick the first tilemap not listed as an obstacle
        if (groundTilemap == null)
        {
            foreach (var tm in tilemaps)
            {
                if (!obstacleTilemaps.Contains(tm))
                {
                    groundTilemap = tm;
                    break;
                }
            }
        }

        tilemapsCached = true;
    }

    static void EnsureTilemapsCached()
    {
        if (!tilemapsCached || grid == null || (groundTilemap == null && obstacleTilemaps.Count == 0))
        {
            CacheTilemaps();
        }
    }

    // Manual overrides if needed
    public static void SetGroundTilemap(Tilemap tm) => groundTilemap = tm;
    public static void AddObstacleTilemap(Tilemap tm)
    {
        if (tm != null && !obstacleTilemaps.Contains(tm))
            obstacleTilemaps.Add(tm);
    }

    // ------------------------------------------------ Direct Placement Validation (O(1), No Colliders)
    public static bool HasGround(Vector3Int cell)
    {
        EnsureTilemapsCached();
        return groundTilemap == null || groundTilemap.HasTile(cell);
    }

    public static bool HasObstacle(Vector3Int cell)
    {
        EnsureTilemapsCached();
        for (int i = 0; i < obstacleTilemaps.Count; i++)
        {
            var tm = obstacleTilemaps[i];
            if (tm != null && tm.HasTile(cell))
                return true;
        }
        return false;
    }

    public static bool IsPath(Vector3Int cell)
    {
        EnsureTilemapsCached();
        for (int i = 0; i < pathTilemaps.Count; i++)
        {
            var tm = pathTilemaps[i];
            if (tm != null && tm.HasTile(cell))
                return true;
        }
        return false;
    }

    public static bool CanPlaceAt(Vector3Int cell)
    {
        if (!Available) return true;

        if (!HasGround(cell)) return false;
        if (HasObstacle(cell)) return false;
        if (IsOccupied(cell)) return false;

        return true;
    }

    // The free cell closest to `cell` (itself when free), searching rings up to `maxRings` away;
    // `cell` itself when none is free
    public static Vector3Int NearestFreeCell(Vector3Int cell, int maxRings = 8)
    {
        for (int ring = 0; ring <= maxRings; ring++)
        {
            bool found = false;
            Vector3Int best = cell;
            float bestDist = float.MaxValue;
            for (int dx = -ring; dx <= ring; dx++)
                for (int dy = -ring; dy <= ring; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ring) continue;
                    var candidate = new Vector3Int(cell.x + dx, cell.y + dy, cell.z);
                    float dist = dx * dx + dy * dy;
                    if (dist < bestDist && CanPlaceAt(candidate))
                    {
                        best = candidate;
                        bestDist = dist;
                        found = true;
                    }
                }
            if (found) return best;
        }
        return cell;
    }

    public static PlacementStatus GetPlacementStatus(Vector3Int cell)
    {
        if (!Available) return PlacementStatus.Valid;
        if (!HasGround(cell)) return PlacementStatus.NoGround;
        if (HasObstacle(cell)) return PlacementStatus.Obstacle;
        if (IsOccupied(cell)) return PlacementStatus.Occupied;
        return PlacementStatus.Valid;
    }

    // ------------------------------------------------ Occupant Tracking
    public static bool IsOccupied(Vector3Int cell) => TryGetOccupant(cell, out _);

    public static bool TryGetOccupant(Vector3Int cell, out GameObject occupant)
    {
        // Unity null check also drops entries whose object died without releasing (e.g. disabled domain reload)
        if (occupants.TryGetValue(cell, out occupant) && occupant != null)
            return true;
        occupants.Remove(cell);
        occupant = null;
        return false;
    }

    public static void Occupy(Vector3Int cell, GameObject occupant)
    {
        if (occupant == null) return;
        occupants[cell] = occupant;
        occupant.AddComponent<GridOccupant>().Cell = cell;
    }

    internal static void Release(Vector3Int cell, GameObject occupant)
    {
        if (occupants.TryGetValue(cell, out var current) && current == occupant)
            occupants.Remove(cell);
    }

    public static void Clear()
    {
        occupants.Clear();
        obstacleTilemaps.Clear();
        pathTilemaps.Clear();
        groundTilemap = null;
        grid = null;
        tilemapsCached = false;
    }
}
