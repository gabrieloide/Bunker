using UnityEngine;
using UnityEngine.UIElements;

// Drop it anywhere on the map: the ally always leaves from the bunker, so there is no preview,
// no flag territory and no obstacle check.
public class AllyCard : Card
{
    protected override void Awake()
    {
        base.Awake();
        if (definition is AllyCardDefinition card && card.count > 1)
        {
            var count = new Label($"x{card.count}") { pickingMode = PickingMode.Ignore };
            count.AddToClassList("card__count");
            view.Add(count);
        }
    }

    protected override bool TryGetPreviewPosition(out Vector3 position)
    {
        position = default;
        return false;
    }

    protected override bool InTerritory => true;
    protected override bool HasObstacleBelow() => false;

    protected override void ShowPlacementFeedback(bool valid)
    {
        // Allies can be dropped anywhere on the map, so only show X if not playable now
        if (!PlayableNow)
            PlacementCrossIndicator.Show(PointerWorld());
        else
            PlacementCrossIndicator.Hide();
    }

    protected override void CardBehaviour()
    {
        if (SimulationBridge.Instance != null && definition is AllyCardDefinition card)
            SimulationBridge.Instance.RequestAllies(card.ally, card.count, card.randomAlly);
        SoundBank.Play(Sfx.AllyDeploy);
    }
}
