using UnityEngine;

public class Deck : MonoBehaviour
{
    [SerializeField] AK.Wwise.Event TakeCard;
<<<<<<< HEAD
    public Transform[] cardSlots;
    public bool[] availableCardSlots;
=======
    [Tooltip("Arranges the cards in hand; they no longer sit in fixed slots")]
    [SerializeField] HandLayout hand;
    [Tooltip("Cards the hand can hold at once")]
    [Min(1)] public int maxCards = 5;
>>>>>>> 1b0f21870329b922747c317aa3561b86f80f1c88
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
<<<<<<< HEAD
        {
            Debug.LogError($"[Deck] Card '{(card != null ? card.name : "null")}' has no hand prefab.", this);
            return;
        }

        for (int i = 0; i < availableCardSlots.Length; i++)
        {
            if (availableCardSlots[i] == true)
            {
                Transform slot = cardSlots[i];
                if (slot == null) continue;

                CardIndex newCard = Instantiate(card.handPrefab, slot);
                TakeCard.Post(gameObject);
                newCard.HandIndex = i;
                if (GameManager.instance != null)
                    GameManager.instance.CurrentCardAmount++;

                // Reset local transform under slot
                newCard.transform.localPosition = Vector3.zero;
                newCard.transform.localRotation = Quaternion.identity;
                newCard.transform.localScale = Vector3.one;

                availableCardSlots[i] = false;
                return;
            }
=======
        {
            Debug.LogError($"[Deck] Card '{(card != null ? card.name : "null")}' has no hand prefab.", this);
            return;
>>>>>>> 1b0f21870329b922747c317aa3561b86f80f1c88
        }
        if (!HasRoom) return;

        CardIndex newCard = Instantiate(card.handPrefab, hand.transform);
        hand.Add(newCard.GetComponent<Card>());
        TakeCard.Post(gameObject);
        if (GameManager.instance != null)
            GameManager.instance.CurrentCardAmount++;
    }
}
