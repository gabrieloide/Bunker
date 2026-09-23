using UnityEngine;

public class SpawnCardOnMouse : Card
{
    protected override bool SnapsToGrid => true;

    protected override void CardBehaviour()
    {
        SpawnPlacement(towerData.CardToInstantiate);
    }
}
