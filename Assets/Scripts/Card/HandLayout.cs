using System.Collections.Generic;
using UnityEngine;

// The hand is a row of fixed slots filled from the left: the first card always sits in the leftmost
// slot and each new card goes right next to the last one. When a card is spent the ones to its right
// slide one slot left. Dragged cards keep their slot until they are spent.
// Cards that can't be played in the current phase (see Card.PlayableIn) are hidden and take no slot;
// they come back when the phase changes.
[RequireComponent(typeof(RectTransform))]
public class HandLayout : MonoBehaviour
{
    [Tooltip("Slots in the row; matches Deck.maxCards")]
    [Min(1)] [SerializeField] int slotCount = 5;
    [Tooltip("Distance between the centres of two neighbouring slots, in canvas units")]
    [SerializeField] float slotSpacing = 46f;
    [Tooltip("Seconds a card takes to slide to its new slot")]
    [SerializeField] float slideTime = 0.2f;

    readonly List<Card> cards = new List<Card>();
    bool? wasPreparation;

    public int Count => cards.Count;

    // Cards shown (and playable) right now
    public int VisibleCount
    {
        get
        {
            int count = 0;
            foreach (var card in cards)
                if (card != null && card.PlayableNow) count++;
            return count;
        }
    }

    public int IndexOf(Card card) => cards.IndexOf(card);

    // Centre of slot `index`; the row is centred on this RectTransform
    public Vector2 SlotPosition(int index) => new Vector2((index - (slotCount - 1) * 0.5f) * slotSpacing, 0f);

    void Update()
    {
        bool preparation = SimulationBridge.IsPreparationPhase;
        if (wasPreparation == preparation) return;
        wasPreparation = preparation;
        Relayout(slideTime);
    }

    public void Add(Card card)
    {
        if (card == null || cards.Contains(card)) return;
        cards.Add(card);
        card.transform.SetParent(transform, false);
        card.transform.localRotation = Quaternion.identity;
        card.transform.localScale = Vector3.one;
        card.transform.SetSiblingIndex(cards.Count - 1);
        if (!card.PlayableNow)
        {
            card.gameObject.SetActive(false);
            return;
        }
        // The newcomer appears straight in its slot (it plays its own deal-in)
        card.SetHandHome(SlotPosition(VisibleCount - 1), 0f);
    }

    public void Remove(Card card)
    {
        int index = cards.IndexOf(card);
        if (index < 0) return;
        cards.RemoveAt(index);
        Relayout(slideTime);
    }

    void Relayout(float time)
    {
        cards.RemoveAll(c => c == null);
        int slot = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (!card.PlayableNow)
            {
                card.CancelDrag();
                card.gameObject.SetActive(false);
                continue;
            }

            bool reappears = !card.gameObject.activeSelf;
            card.gameObject.SetActive(true);
            card.SetHandHome(SlotPosition(slot++), reappears ? 0f : time);
            if (card.transform.parent == transform)
                card.transform.SetSiblingIndex(i);
        }
    }
}
