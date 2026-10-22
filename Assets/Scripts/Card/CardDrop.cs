using System.Collections.Generic;
using UnityEngine;

public class CardDrop : MonoBehaviour
{
    public static CardDrop instance;
    public Queue<CardDefinition> cardsQueue = new Queue<CardDefinition>();
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

    public void EnqueueCard(CardDefinition card)
    {
        if (card == null) return;
        cardsQueue.Enqueue(card);
        ShowDeck();
    }

    public void DealInitialHand()
    {
        while (Deck.instance != null && Deck.instance.HasRoom && cardsQueue.Count > 0)
        {
            Deck.instance.SearchAviableSlots(cardsQueue.Dequeue());
        }

        if (cardsQueue.Count <= 0)
        {
            HideDeck();
        }
        else
        {
            ShowDeck();
        }
    }

    public void TakeCard()
    {
        if (Deck.instance != null && Deck.instance.HasRoom)
        {
            if (cardsQueue.Count > 0)
            {
                // Tomar carta del deck
                Deck.instance.SearchAviableSlots(cardsQueue.Dequeue());
                if (deckSliceAnimation != null)
                {
                    LeanTween.moveY(deckSliceAnimation, -82, TimeMove())
                        .setEase(UIManager.instance != null ? UIManager.instance.TweenDeckOut : LeanTweenType.easeOutQuad)
                        .setOnComplete(ResetTweenAnim);
                }
            }
            if (cardsQueue.Count <= 0)
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
