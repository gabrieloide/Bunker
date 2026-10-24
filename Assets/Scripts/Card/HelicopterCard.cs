// Air support, played during a wave: drop it on any cell (path and scenery included) and the helicopter
// flies out of the bunker, patrols a zone around that cell shooting enemies for a while, then flies home.
public class HelicopterCard : Card
{
    HelicopterCardDefinition Heli => definition as HelicopterCardDefinition;

    protected override bool SnapsToGrid => true;
    // It flies off, so it doesn't hold the cell
    protected override bool OccupiesCell => false;
    protected override bool InTerritory => true;
    protected override bool HasObstacleBelow()
    {
        if (!PlacementGrid.Available) return false;
        var cell = FootprintCell();
        return !PlacementGrid.HasGround(cell) && !PlacementGrid.IsPath(cell);
    }
    // The ring shows the patrol zone
    protected override float PlacedRange => Heli != null ? Heli.patrolRadius : 0f;

    protected override void CardBehaviour()
    {
        SpawnPlacement();
    }
}
