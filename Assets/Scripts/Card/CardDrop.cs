using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The draw pile, shared by every card type. Drawing takes the oldest card playable in the current
// phase (towers in preparation, allies during a wave), so cards for the other phase wait their turn.
// The pile is the "deck" element of UI/Game.uxml; clicking it draws.
public class CardDrop : MonoBehaviour
{
    public static CardDrop instance;
    [System.NonSerialized] public List<CardDefinition> cardsQueue = new List<CardDefinition>();
    [Tooltip("Deck height when shown and when tucked away, panel units (y up)")]
    public float posInCamera, posOutCamera;
    public LeanTweenType TweenDeck;
    private bool isHidingDeck;

    VisualElement deck, deckSlice;
    float deckY, sliceY = SliceRestY;
    int deckTween = -1;
    const float SliceRestY = 1.79f, SliceTakenY = -82f;

    public bool DeckShown { get; private set; }

    public float TimeMove()
    {
        return UIManager.instance != null ? UIManager.instance.TimeMovement : 0.4f;
    }

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

    void Start()
    {
        var root = GameUIPanel.Instance.Root;
        deck = root.Q("deck");
        deckSlice = root.Q("deck-slice");
        deck.RegisterCallback<ClickEvent>(_ => TakeCard());
        deckY = posOutCamera;
        deck.style.display = DisplayStyle.None;
    }

    void LateUpdate()
    {
        if (deck == null) return;
        deck.style.translate = new Translate(0f, -deckY);
        deckSlice.style.translate = new Translate(0f, -sliceY);
    }

    public bool HasPlayableCard => NextPlayableIndex() >= 0;

    int NextPlayableIndex()
    {
        bool preparation = SimulationBridge.IsPreparationPhase;
        return cardsQueue.FindIndex(c => Card.PlayableIn(c, preparation));
    }

    public void EnqueueCard(CardDefinition card)
    {
        if (card == null) return;
        cardsQueue.Add(card);
        if (HasPlayableCard)
            ShowDeck();
    }

    // Straight into the hand while it has room; the rest wait in the draw pile
    public void Deal(CardDefinition card)
    {
        if (card == null) return;
        if (Deck.instance != null && Deck.instance.HasRoom && Card.PlayableIn(card, SimulationBridge.IsPreparationPhase))
            Deck.instance.SearchAviableSlots(card);
        else
            EnqueueCard(card);
    }

    public void TakeCard()
    {
        if (Deck.instance != null && Deck.instance.HasRoom)
        {
            int index = NextPlayableIndex();
            if (index >= 0)
            {
                // Tomar carta del deck
                CardDefinition card = cardsQueue[index];
                cardsQueue.RemoveAt(index);
                Deck.instance.SearchAviableSlots(card);
                LeanTween.value(gameObject, y => sliceY = y, SliceRestY, SliceTakenY, TimeMove())
                    .setEase(UIManager.instance != null ? UIManager.instance.TweenDeckOut : LeanTweenType.easeOutQuad)
                    .setOnComplete(() => sliceY = SliceRestY);
            }
            if (!HasPlayableCard)
            {
                HideDeck();
            }
        }
    }

    public void ShowDeck()
    {
        if (deck == null) return;
        isHidingDeck = false;
        DeckShown = true;
        deck.style.display = DisplayStyle.Flex;

        MoveDeck(posInCamera).setEase(TweenDeck != LeanTweenType.notUsed ? TweenDeck : LeanTweenType.easeOutQuad);
    }

    public void HideDeck()
    {
        if (deck == null || !DeckShown || isHidingDeck)
            return;

        isHidingDeck = true;
        MoveDeck(posOutCamera)
            .setOnComplete(DeactivateDeck)
            .setEase(UIManager.instance != null ? UIManager.instance.TweenDeckOut : LeanTweenType.easeOutQuad);
    }

    LTDescr MoveDeck(float y)
    {
        if (deckTween >= 0) LeanTween.cancel(gameObject, deckTween);
        var tween = LeanTween.value(gameObject, v => deckY = v, deckY, y, TimeMove());
        deckTween = tween.uniqueId;
        return tween;
    }

    void DeactivateDeck()
    {
        isHidingDeck = false;
        DeckShown = false;
        deck.style.display = DisplayStyle.None;
    }
}
