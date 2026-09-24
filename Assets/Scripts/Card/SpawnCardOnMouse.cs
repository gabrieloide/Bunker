using UnityEngine;

public class SpawnCardOnMouse : Card
{
    protected override bool SnapsToGrid => true;
    protected override bool BuiltBySoldier => true;

    protected override void CardBehaviour()
    {
        SpawnPlacement();
    }
}
