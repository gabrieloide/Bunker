using System.Collections.Generic;
using UnityEngine;

// Cell snapping + one-occupant-per-cell registry for card placement, backed by the scene's tilemap Grid.
public static class PlacementGrid
{
    static Grid grid;
    static readonly Dictionary<Vector3Int, GameObject> occupants = new Dictionary<Vector3Int, GameObject>();

    static Grid Grid => grid != null ? grid : (grid = Object.FindAnyObjectByType<Grid>());

    public static bool Available => Grid != null;
    public static Vector2 CellSize => Grid.cellSize;

    public static Vector3Int WorldToCell(Vector3 world) => Grid.WorldToCell(world);

    public static Vector3 CellCenter(Vector3Int cell)
    {
        Vector3 center = Grid.GetCellCenterWorld(cell);
        center.z = 0f;
        return center;
    }

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
}
