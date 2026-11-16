using UnityEngine;

// Plants a flag to win ground back. Unlike other cards it doesn't need to stand inside the territory:
// its circle has to touch an existing flag's, no enemy may be inside it and it can't stand in enemy territory.
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
        PlacementCrossIndicator.Hide();
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
        base.HidePlacementFeedback();
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
            return FlagTerritory.IsConnected(point, Radius) && !EnemyTerritory.Contains(point)
                && !SimulationBridge.AnyEnemyWithin(point, Radius);
        }
    }

    protected override float PlacedRange => Radius;
    protected override bool SnapsToGrid => true;

    // The flag step: every preparation opens by planting a flag. Until one is planted only flag cards
    // are shown and playable; the rest wait hidden in the hand (see HandLayout, Card.PlayableNow).
    static bool awaitingPlant;

    // Raised when the planted flag ends the flag step
    public static event System.Action StepCompleted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        awaitingPlant = false;
        StepCompleted = null;
    }

    public static void BeginStep() => awaitingPlant = true;

    // Only while a flag card can actually be reached (hand or draw pile): trashing it must not lock the hand
    public static bool StepActive => awaitingPlant && SimulationBridge.IsPreparationPhase && !SimEventDispatcher.IsGameOver && FlagAvailable();

    public static bool Allows(CardDefinition card) => !StepActive || card is FlagCardDefinition;

    static bool FlagAvailable()
    {
        var hand = Deck.instance != null ? Deck.instance.Hand : null;
        if (hand != null && hand.HasCard<FlagCardDefinition>()) return true;
        return CardDrop.instance != null && CardDrop.instance.cardsQueue.Exists(c => c is FlagCardDefinition);
    }

    protected override void CardBehaviour()
    {
        bool planted = SpawnPlacement() != null;
        SoundBank.Play(Sfx.FlagPlant);
        if (!planted || !awaitingPlant) return;
        awaitingPlant = false;
        StepCompleted?.Invoke();
    }
}
