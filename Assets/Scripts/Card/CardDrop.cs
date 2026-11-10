using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The draw pile, shared by every card type. Drawing takes the oldest card playable in the current
// phase (towers only in preparation, helicopters only during a wave), so cards for the other phase wait their turn.
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
    Label countLabel;
    float deckY, sliceY = SliceRestY;
    int deckTween = -1;
    const float SliceRestY = 1.79f, SliceTakenY = -82f;

    // Count tag: pops big when a card joins the pile, dips when one is drawn
    int shownCount = -1;
    float countScale = 1f;
    int countTween = -1;
    const float CountGrowScale = 1.6f, CountShrinkScale = 0.6f, CountPopTime = 0.3f, CountFlashTime = 0.12f;

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
        countLabel = root.Q<Label>("deck-count");
        deck.RegisterCallback<ClickEvent>(_ => TakeCard());
        deckY = posOutCamera;
        deck.style.display = DisplayStyle.None;
    }

    void LateUpdate()
    {
        if (deck == null) return;
        deck.style.translate = new Translate(0f, -deckY);
        deckSlice.style.translate = new Translate(0f, -sliceY);
        UpdateCount();
    }

    // Polled so every change shows up: draws, drops, dealt towers and phase changes
    void UpdateCount()
    {
        if (countLabel == null) return;
        int count = PlayableCount;
        if (count != shownCount)
        {
            // No pop the first time or while the pile is tucked away
            if (shownCount >= 0 && DeckShown)
                PopCount(count > shownCount);
            shownCount = count;
            countLabel.text = count.ToString();
        }
        countLabel.style.scale = new Scale(new Vector3(countScale, countScale, 1f));
    }

    void PopCount(bool grew)
    {
        if (countTween >= 0) LeanTween.cancel(gameObject, countTween);
        countTween = LeanTween.value(gameObject, s => countScale = s, grew ? CountGrowScale : CountShrinkScale, 1f, CountPopTime)
            .setEaseOutBack().uniqueId;
        if (!grew) return;
        countLabel.AddToClassList("deck-count--flash");
        countLabel.schedule.Execute(() => countLabel.RemoveFromClassList("deck-count--flash"))
            .StartingIn((long)(CountFlashTime * 1000f));
    }

    // Cards a click can draw right now (the other phase's cards wait in the pile uncounted)
    public int PlayableCount
    {
        get
        {
            bool preparation = SimulationBridge.IsPreparationPhase;
            int count = 0;
            foreach (var card in cardsQueue)
                if (Card.PlayableIn(card, preparation)) count++;
            return count;
        }
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
        if (!DeckShown) SoundBank.Play(Sfx.DeckAppear);
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
