using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The hand is a row of fixed slots filled from the left: the first card always sits in the leftmost
// slot and each new card goes right next to the last one. When a card is spent the ones to its right
// slide one slot left. Dragged cards keep their slot until they are spent.
// Cards that can't be played in the current phase (see Card.PlayableIn) are hidden and take no slot;
// they come back when the phase changes. The cards are drawn in the "hand" element of UI/Game.uxml.
public class HandLayout : MonoBehaviour
{
    [Tooltip("Slots in the row; matches Deck.maxCards")]
    [Min(1)] [SerializeField] int slotCount = 5;
    [Tooltip("Distance between the centres of two neighbouring slots, in panel units")]
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

    // Centre of slot `index` (y up); the row is centred on the hand element
    public Vector2 SlotPosition(int index) => new Vector2((index - (slotCount - 1) * 0.5f) * slotSpacing, 0f);

    public VisualElement Container => GameUIPanel.Instance.Hand;

    // Placeholder left in the slot of the card being dragged
    public void ShowGhost(Vector2 slot)
    {
        var ghost = Container.Q("slot-ghost");
        ghost.style.translate = new Translate(slot.x, -slot.y);
        ghost.AddToClassList("slot-ghost--shown");
    }

    public void HideGhost() => Container.Q("slot-ghost").RemoveFromClassList("slot-ghost--shown");

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
        card.SetDrawOrder(cards.Count - 1);
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
            card.transform.SetSiblingIndex(i);
            card.SetDrawOrder(i);
        }
    }
}
