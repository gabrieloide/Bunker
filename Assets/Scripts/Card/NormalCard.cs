using UnityEngine;

public class NormalCard : Card
{
    LayerMask NormalCardLM() => LayerMask.GetMask("Decoration", "Path", "Limits");
    protected override bool SnapsToGrid => true;
    protected override bool BuiltBySoldier => true;
    protected override RaycastHit2D DetectObjectsBelow() => CastFootprint(NormalCardLM());

    TowerCardDefinition Tower => definition as TowerCardDefinition;
    bool reserved;

    bool UnderLimit => Tower == null || TowerLimits.CanPlace(Tower, GameManager.instance != null ? GameManager.instance.Catalog : null);

    // At the tower limit the preview hides, so the drop reads as invalid before letting go
    protected override bool TryGetPreviewPosition(out Vector3 position)
    {
        bool visible = base.TryGetPreviewPosition(out position);
        return visible && UnderLimit;
    }

    protected override bool AllowPlacement()
    {
        if (UnderLimit) return true;
        if (UIManager.instance != null) UIManager.instance.FlashTowerLimit();
        return false;
    }

    protected override void OnPlacementAccepted()
    {
        if (Tower == null) return;
        TowerLimits.Reserve(Tower);
        reserved = true;
    }

    protected override void CardBehaviour()
    {
        ReleaseReservation();
        SpawnPlacement();
    }

    protected override void OnDestroy()
    {
        ReleaseReservation();
        base.OnDestroy();
    }

    void ReleaseReservation()
    {
        if (!reserved) return;
        reserved = false;
        TowerLimits.Release(Tower);
    }
}
