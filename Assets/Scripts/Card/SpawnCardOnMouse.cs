using UnityEngine;

public class SpawnCardOnMouse : Card
{
    protected override void CardBehaviour()
    {
        Vector2 Mouseposition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        SimulationBridge.SpawnFromCard(towerData.CardToInstantiate, Mouseposition);
    }
}
