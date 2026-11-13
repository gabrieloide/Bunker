using UnityEngine;
using UnityEngine.UIElements;

// Sends allies out along the player-built ally road. If no ally road has been built yet,
// blocks placement and shows a monochrome notice telling the player to build a road first.
public class AllyCard : Card
{
    Label noticeLabel;

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

    protected override void OnDestroy()
    {
        HideNotice();
        noticeLabel?.RemoveFromHierarchy();
        base.OnDestroy();
    }

    protected override bool TryGetPreviewPosition(out Vector3 position)
    {
        position = default;
        return false;
    }

    protected override bool InTerritory => true;
    protected override bool HasObstacleBelow() => false;

    bool HasAllyRoad => !AllyRoadNetwork.Initialized || AllyRoadNetwork.BuiltTileCount > 0;

    public override bool CanPlaceNow => base.CanPlaceNow && HasAllyRoad;

    protected override void ShowPlacementFeedback(bool valid)
    {
        if (!PlayableNow)
        {
            PlacementCrossIndicator.Show(PointerWorld());
            HideNotice();
        }
        else if (!HasAllyRoad)
        {
            PlacementCrossIndicator.Show(PointerWorld());
            ShowNotice("BUILD AN ALLY ROAD FIRST");
        }
        else
        {
            PlacementCrossIndicator.Hide();
            HideNotice();
        }
    }

    protected override void HidePlacementFeedback()
    {
        HideNotice();
        base.HidePlacementFeedback();
    }

    void ShowNotice(string message)
    {
        if (GameUIPanel.Instance == null || GameUIPanel.Instance.Root == null) return;
        if (noticeLabel == null)
        {
            noticeLabel = new Label { pickingMode = PickingMode.Ignore };
            noticeLabel.style.position = Position.Absolute;
            noticeLabel.style.left = 0;
            noticeLabel.style.right = 0;
            noticeLabel.style.bottom = 175;
            noticeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noticeLabel.style.fontSize = 26;
            noticeLabel.style.color = Color.white;
            noticeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            noticeLabel.style.textShadow = new TextShadow { offset = new Vector2(2, 2), blurRadius = 0, color = Color.black };
            GameUIPanel.Instance.Root.Add(noticeLabel);
        }
        noticeLabel.text = message;
        noticeLabel.style.display = DisplayStyle.Flex;
    }

    void HideNotice()
    {
        if (noticeLabel != null)
            noticeLabel.style.display = DisplayStyle.None;
    }

    protected override void CardBehaviour()
    {
        HideNotice();
        if (SimulationBridge.Instance != null && definition is AllyCardDefinition card)
        {
            Unity.Entities.Entity preferred = ResolvePreferredRoad();
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
