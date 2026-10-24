using UnityEngine;

// Plants a flag to win ground back. Unlike other cards it doesn't need to stand inside the territory:
// its circle has to touch an existing flag's, and no enemy may be inside it.
public class FlagCard : Card
{
    [Tooltip("Flag ghost with a check/cross, shown where the flag would go while dragging")]
    [SerializeField] FlagPlacementPreview previewPrefab;
    FlagPlacementPreview preview;
    [Tooltip("Mouse cursor while dragging this card: the hand holding the flag (hotspot at its centre)")]
    [SerializeField] Texture2D dragCursor;
    bool cursorSwapped;

    float Radius => definition is FlagCardDefinition flag ? flag.radius : 0f;

    protected override void ShowPlacementFeedback(bool valid)
    {
        if (dragCursor != null && !cursorSwapped)
        {
            Cursor.SetCursor(dragCursor, new Vector2(dragCursor.width * 0.5f, dragCursor.height * 0.5f), CursorMode.Auto);
            cursorSwapped = true;
        }
        if (previewPrefab == null) return;
        if (preview == null) preview = Instantiate(previewPrefab);
        preview.Show(GetRaycastOrigin(), valid);
    }

    protected override void HidePlacementFeedback()
    {
        RestoreCursor();
        if (preview != null) preview.Hide();
    }

    void RestoreCursor()
    {
        if (!cursorSwapped) return;
        cursorSwapped = false;
        Cursor.SetCursor(UIManager.instance != null ? UIManager.instance.cursorDefault : null, Vector2.zero, CursorMode.Auto);
    }

    protected override void OnDestroy()
    {
        RestoreCursor();
        if (preview != null) Destroy(preview.gameObject);
        base.OnDestroy();
    }

    protected override bool InTerritory
    {
        get
        {
            Vector3 point = GetRaycastOrigin();
            return FlagTerritory.IsConnected(point, Radius) && !SimulationBridge.AnyEnemyWithin(point, Radius);
        }
    }

    protected override float PlacedRange => Radius;
    protected override bool BuiltBySoldier => true;
    protected override bool CarriesFlag => true;
    protected override bool SnapsToGrid => true;

    protected override void CardBehaviour()
    {
        SpawnPlacement();
    }
}
