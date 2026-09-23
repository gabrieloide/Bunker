using UnityEngine;

public class AirAttackCard : Card
{
    protected override void CardBehaviour()
    {
        SimulationBridge.SpawnFromCard(definition, Vector3.zero);
    }
}
