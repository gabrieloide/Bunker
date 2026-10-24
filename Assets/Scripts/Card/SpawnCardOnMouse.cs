using UnityEngine;

// Land mine: buried on the enemy path (one per cell), the only ground it can go on
public class SpawnCardOnMouse : Card
{
    protected override bool SnapsToGrid => true;
    protected override bool BuiltBySoldier => true;

    protected override bool HasObstacleBelow()
    {
        if (!PlacementGrid.Available) return base.HasObstacleBelow();
        Vector3Int cell = FootprintCell();
        return !PlacementGrid.IsPath(cell) || PlacementGrid.IsOccupied(cell);
    }

    protected override void CardBehaviour()
    {
        SpawnPlacement();
    }
}
