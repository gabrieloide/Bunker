using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The hand is a row of fixed slots filled from the left: the first card always sits in the leftmost
// slot and each new card goes right next to the last one. When a card is spent the ones to its right
// slide one slot left. Dragged cards keep their slot until they are spent.
// During battle the whole hand container slides off-screen downward; in preparation it slides back up.
// The cards are drawn in the "hand" element of UI/Game.uxml.
public class HandLayout : MonoBehaviour
{
    [Tooltip("Slots in the row; matches Deck.maxCards")]
    [Min(1)] [SerializeField] int slotCount = 5;
    [Tooltip("Distance between the centres of two neighbouring slots, in panel units")]
    [SerializeField] float slotSpacing = 46f;
    [Tooltip("Seconds a card takes to slide to its new slot")]
    [SerializeField] float slideTime = 0.2f;
    [Tooltip("Seconds the whole hand takes to slide off-screen for battle and back for preparation")]
    [SerializeField] float phaseSlideTime = 0.3f;
    [Tooltip("Vertical offset in panel units (y down) when the hand is lowered during battle")]
    [SerializeField] float loweredOffsetY = 120f;

    readonly List<Card> cards = new List<Card>();
    bool? wasPreparation;
    bool wasFlagStep;
    Vector2 baseHandTranslate;
    bool hasBaseTranslate;
    float currentHandY;
    int handTweenId = -1;

    public bool Lowered { get; private set; }
    public int Count => cards.Count;

    // Cards shown in the hand right now
    public int VisibleCount
    {
        get
        {
            int count = 0;
            foreach (var card in cards)
                if (card != null && card.gameObject.activeSelf) count++;
            return count;
        }
    }

    public int IndexOf(Card card) => cards.IndexOf(card);

    public bool HasCard<T>() where T : CardDefinition
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null && cards[i].definition is T)
                return true;
        }
        return false;
    }

    // Centre of slot `index` (y up); the row is centred on the hand element
    public Vector2 SlotPosition(int index) => new Vector2((index - (slotCount - 1) * 0.5f) * slotSpacing, 0f);

    // Null while the scene is being torn down (the panel may go first)
    public VisualElement Container => GameUIPanel.Instance != null ? GameUIPanel.Instance.Hand : null;

    // Placeholder left in the slot of the card being dragged
    public void ShowGhost(Vector2 slot)
    {
        var ghost = Container?.Q("slot-ghost");
        if (ghost == null) return;
        ghost.style.translate = new Translate(slot.x, -slot.y);
        ghost.AddToClassList("slot-ghost--shown");
    }

    public void HideGhost() => Container?.Q("slot-ghost")?.RemoveFromClassList("slot-ghost--shown");

    // During the flag step only flag cards are shown; the rest wait hidden in their place
    static bool Shown(Card card) => FlagCard.Allows(card.definition);

    void Update()
    {
        bool flagStep = FlagCard.StepActive;
        if (flagStep != wasFlagStep)
        {
            wasFlagStep = flagStep;
            Relayout(slideTime);
        }

        bool preparation = SimulationBridge.IsPreparationPhase;
        if (wasPreparation == preparation) return;
        bool first = !wasPreparation.HasValue;
        wasPreparation = preparation;

        if (!preparation)
        {
            CancelAllDrags();
            SlideHand(true);
        }
        else
        {
            if (!first)
                SlideHand(false);
            Relayout(slideTime);
        }
    }

    void CancelAllDrags()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null)
                cards[i].CancelDrag();
        }
    }

    void SlideHand(bool lower)
    {
        Lowered = lower;
        var container = Container;
        if (container == null) return;

        if (!hasBaseTranslate)
        {
            var t = container.resolvedStyle.translate;
            baseHandTranslate = new Vector2(t.x, t.y);
            currentHandY = baseHandTranslate.y;
            hasBaseTranslate = true;
        }

        float startY = currentHandY;
        float targetY = lower ? baseHandTranslate.y + loweredOffsetY : baseHandTranslate.y;

        if (handTweenId >= 0)
            LeanTween.cancel(gameObject, handTweenId);

        handTweenId = LeanTween.value(gameObject, y =>
        {
            currentHandY = y;
            var c = Container;
            if (c != null)
                c.style.translate = new Translate(baseHandTranslate.x, y);
        }, startY, targetY, phaseSlideTime)
        .setEase(lower ? LeanTweenType.easeInBack : LeanTweenType.easeOutBack)
        .uniqueId;
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

        int slot = 0;
        for (int i = 0; i < cards.Count - 1; i++)
            if (cards[i] != null && cards[i].gameObject.activeSelf) slot++;
        if (slot >= slotCount || !Shown(card))
        {
            card.gameObject.SetActive(false);
            return;
        }
        // The newcomer appears straight in its slot (it plays its own deal-in)
        card.SetHandHome(SlotPosition(slot), 0f);
    }

    public void Remove(Card card)
    {
        int index = cards.IndexOf(card);
        if (index < 0) return;
        cards.RemoveAt(index);
        // While the hand itself goes away (leaving play mode, reloading the level) the other cards are being destroyed too:
        // moving or toggling them trips Unity's "!IsDestroying()" assertions. Teardown disables the whole hierarchy before
        // any OnDestroy runs, so a disabled hand means there is nothing left to lay out.
        if (isActiveAndEnabled)
            Relayout(slideTime);
    }

    void Relayout(float time)
    {
        cards.RemoveAll(c => c == null);
        int slot = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            // More cards than slots, or held back by the flag step: they wait hidden until they can show
            if (slot >= slotCount || !Shown(card))
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
