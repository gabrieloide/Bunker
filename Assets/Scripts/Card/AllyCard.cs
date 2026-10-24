using UnityEngine;

// Drop it anywhere on the map: the ally always leaves from the bunker, so there is no preview,
// no flag territory and no obstacle check.
public class AllyCard : Card
{
    protected override bool TryGetPreviewPosition(out Vector3 position)
    {
        position = default;
        return false;
    }

    protected override bool InTerritory => true;
    protected override bool HasObstacleBelow() => false;

    protected override void CardBehaviour()
    {
        if (SimulationBridge.Instance != null && definition is AllyCardDefinition card)
            SimulationBridge.Instance.RequestAlly(card.ally);
    }
}
