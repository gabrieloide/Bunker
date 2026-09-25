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
    public float TimeMove()
    {
        return UIManager.instance.TimeMovement;
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
                LeanTween.moveY(DeckGameObject.GetComponent<RectTransform>(), posOutCamera, TimeMove())
                    .setOnComplete(DeactivateDeck)
                        .setEase(UIManager.instance.TweenDeckOut);
            }
        }
    }
    void DeactivateDeck()
    {
        DeckGameObject.SetActive(false);
    }
    void ResetTweenAnim()
    {
        LeanTween.moveY(deckSliceAnimation, 1.7895f, 0);
    }

}