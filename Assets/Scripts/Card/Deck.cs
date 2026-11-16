using UnityEngine;

public class Deck : MonoBehaviour
{
    [SerializeField] Sfx TakeCard = Sfx.CardDraw;
    [Tooltip("Arranges the cards in hand; they no longer sit in fixed slots")]
    [SerializeField] HandLayout hand;
    [Tooltip("Cards the hand can hold at once")]
    [Min(1)] public int maxCards = 5;
    public static Deck instance;

    public HandLayout Hand => hand;
    // Every held card counts, including the ones hidden during the flag step
    public bool HasRoom => hand != null && hand.Count < maxCards;

    // During the flag step a flag always fits: it's the only card shown, and planting it frees the extra slot
    public bool HasRoomFor(CardDefinition card) => HasRoom || (hand != null && card is FlagCardDefinition && FlagCard.StepActive);

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SearchAviableSlots(CardDefinition card)
    {
        if (card == null || card.handPrefab == null)
        {
            Debug.LogError($"[Deck] Card '{(card != null ? card.name : "null")}' has no hand prefab.", this);
            return;
        }
        if (!HasRoomFor(card)) return;

        CardIndex newCard = Instantiate(card.handPrefab, hand.transform);
        hand.Add(newCard.GetComponent<Card>());
        SoundBank.Play(TakeCard);
        if (GameManager.instance != null)
            GameManager.instance.CurrentCardAmount++;
    }
}
