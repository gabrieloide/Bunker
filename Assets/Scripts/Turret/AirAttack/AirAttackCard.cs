using UnityEngine;

public class AirAttackCard : Card
{
    protected override void CardBehaviour()
    {
        SimulationBridge.SpawnFromCard(towerData.CardToInstantiate, Vector3.zero);
    }
}
