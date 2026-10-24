using System.Collections.Generic;
using UnityEngine;

// The draw pile, shared by every card type. Drawing takes the oldest card playable in the current
// phase (towers in preparation, allies during a wave), so cards for the other phase wait their turn.
public class CardDrop : MonoBehaviour
{
    public static CardDrop instance;
    [System.NonSerialized] public List<CardDefinition> cardsQueue = new List<CardDefinition>();
    public float posInCamera, posOutCamera;
    [SerializeField] RectTransform deckSliceAnimation;
    [SerializeField] GameObject DeckGameObject;
    public LeanTweenType TweenDeck;
    private bool isHidingDeck;

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
                if (deckSliceAnimation != null)
                {
                    LeanTween.moveY(deckSliceAnimation, -82, TimeMove())
                        .setEase(UIManager.instance != null ? UIManager.instance.TweenDeckOut : LeanTweenType.easeOutQuad)
                        .setOnComplete(ResetTweenAnim);
                }
            }
            if (!HasPlayableCard)
            {
                HideDeck();
            }
        }
    }

    public void ShowDeck()
    {
        if (DeckGameObject == null) return;
        isHidingDeck = false;
        if (!DeckGameObject.activeSelf)
            DeckGameObject.SetActive(true);

        LeanTween.cancel(DeckGameObject);
        LeanTween.moveY(DeckGameObject.GetComponent<RectTransform>(), posInCamera, TimeMove())
            .setEase(TweenDeck != LeanTweenType.notUsed ? TweenDeck : LeanTweenType.easeOutQuad);
    }

    public void HideDeck()
    {
        if (DeckGameObject == null || !DeckGameObject.activeSelf || isHidingDeck)
            return;

        isHidingDeck = true;
        LeanTween.cancel(DeckGameObject);
        LeanTween.moveY(DeckGameObject.GetComponent<RectTransform>(), posOutCamera, TimeMove())
            .setOnComplete(DeactivateDeck)
            .setEase(UIManager.instance != null ? UIManager.instance.TweenDeckOut : LeanTweenType.easeOutQuad);
    }

    void DeactivateDeck()
    {
        isHidingDeck = false;
        if (DeckGameObject != null)
            DeckGameObject.SetActive(false);
    }

    void ResetTweenAnim()
    {
        if (deckSliceAnimation != null)
            LeanTween.moveY(deckSliceAnimation, 1.7895f, 0);
    }
}
