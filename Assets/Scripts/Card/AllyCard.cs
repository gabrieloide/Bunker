using UnityEngine;
using UnityEngine.UIElements;

// Sends allies out along the player-built ally road. If no ally road has been built yet,
// blocks placement and shows a monochrome notice telling the player to build a road first.
public class AllyCard : Card
{
    static Label noticeLabel;
    static IVisualElementScheduledItem hideSchedule;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        hideSchedule?.Pause();
        hideSchedule = null;
        noticeLabel = null;
    }

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

    bool HasAllyRoad => AllyRoadNetwork.Initialized && AllyRoadNetwork.BuiltTileCount > 0;

    public override bool CanPlaceNow => base.CanPlaceNow && HasAllyRoad;

    protected override bool AllowPlacement()
    {
        if (HasAllyRoad) return true;
        ShowNoticeOnDrop("BUILD AN ALLY ROAD FIRST");
        return false;
    }

    protected override void ShowPlacementFeedback(bool valid)
    {
        if (!PlayableNow || !HasAllyRoad)
            PlacementCrossIndicator.Show(PointerWorld());
        else
            PlacementCrossIndicator.Hide();
    }

    protected override void HidePlacementFeedback()
    {
        base.HidePlacementFeedback();
    }

    static void ShowNoticeOnDrop(string message)
    {
        if (GameUIPanel.Instance == null || GameUIPanel.Instance.Root == null) return;

        var root = GameUIPanel.Instance.Root;
        if (noticeLabel == null || noticeLabel.panel != root.panel)
        {
            noticeLabel = new Label { pickingMode = PickingMode.Ignore };
            noticeLabel.AddToClassList("ally-road-notice");
            root.Add(noticeLabel);
        }

        noticeLabel.text = message;
        noticeLabel.style.display = DisplayStyle.Flex;
        noticeLabel.BringToFront();

        hideSchedule?.Pause();
        hideSchedule = noticeLabel.schedule.Execute(() => noticeLabel.style.display = DisplayStyle.None).StartingIn(1600);
    }

    protected override void CardBehaviour()
    {
        if (!HasAllyRoad) return;
        if (SimulationBridge.Instance != null && definition is AllyCardDefinition card)
        {
            Unity.Entities.Entity preferred = ResolvePreferredRoad();
            if (preferred == Unity.Entities.Entity.Null) return;
            SimulationBridge.Instance.RequestAllies(card.ally, card.count, card.mix, preferred);
        }
        SoundBank.Play(Sfx.AllyDeploy);
    }

    Unity.Entities.Entity ResolvePreferredRoad()
    {
        if (AllyRoadNetwork.BuiltTileCount == 0 || SimulationBridge.Instance == null)
            return default;

        return SimulationBridge.Instance.AllyRoadPathEntity;
    }
}
