// Air support, played during a wave: drop it anywhere and the helicopter flies out of the bunker,
// hovers over that spot shooting every enemy in range for a while, then flies home.
public class HelicopterCard : Card
{
    protected override bool InTerritory => true;
    protected override bool HasObstacleBelow() => false;
    protected override float PlacedRange => definition is HelicopterCardDefinition heli ? heli.range : 0f;

    protected override void CardBehaviour()
    {
        SpawnPlacement();
    }
}
