using UnityEngine;

public class Deck : MonoBehaviour
{
    [SerializeField] AK.Wwise.Event TakeCard;
    [Tooltip("Arranges the cards in hand; they no longer sit in fixed slots")]
    [SerializeField] HandLayout hand;
    [Tooltip("Cards the hand can hold at once")]
    [Min(1)] public int maxCards = 5;
    public static Deck instance;

    public HandLayout Hand => hand;
    public bool HasRoom => hand != null && hand.Count < maxCards;

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
        if (!HasRoom) return;

        CardIndex newCard = Instantiate(card.handPrefab, hand.transform);
        hand.Add(newCard.GetComponent<Card>());
        TakeCard.Post(gameObject);
        if (GameManager.instance != null)
            GameManager.instance.CurrentCardAmount++;
    }
}
