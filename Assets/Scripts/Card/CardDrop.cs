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

    public void TakeCard()
    {
        if (!SimulationBridge.IsPreparationPhase)
            return;

        if (Deck.instance != null && Deck.instance.HasRoom)
        {
            if (cardsQueue.Count > 0)
            {
                //Tomar carta del deck
                Deck.instance.SearchAviableSlots(cardsQueue.Dequeue());
                LeanTween.moveY(deckSliceAnimation, -82, TimeMove())
                    .setEase(UIManager.instance.TweenDeckOut)
                    .setOnComplete(ResetTweenAnim);

            }
            if (cardsQueue.Count <= 0)
            {
                HideDeck();
            }
        }
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
        LeanTween.moveY(deckSliceAnimation, 1.7895f, 0);
    }

}