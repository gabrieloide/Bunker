using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    [SerializeField] AK.Wwise.Event TakeCard;
    [HideInInspector] public List<CardIndex> deck = new List<CardIndex>(); // LEGACY: cards now come as CardDefinitions, dropped after migration
    public Transform[] cardSlots;
    public bool[] availableCardSlots;
    public static Deck instance;

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
        }
    }
}
