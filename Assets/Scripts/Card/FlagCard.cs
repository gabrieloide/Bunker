using UnityEngine;

// Plants a flag to win ground back. Unlike other cards it doesn't need to stand inside the territory:
// its circle has to touch an existing flag's, and no enemy may be inside it.
public class FlagCard : Card
{
    float Radius => definition is FlagCardDefinition flag ? flag.radius : 0f;

    protected override bool InTerritory
    {
        get
        {
            Vector3 point = GetRaycastOrigin();
            return FlagTerritory.IsConnected(point, Radius) && !SimulationBridge.AnyEnemyWithin(point, Radius);
        }
    }

    protected override float PlacedRange => Radius;

    protected override void CardBehaviour()
    {
        SpawnPlacement();
    }
}
