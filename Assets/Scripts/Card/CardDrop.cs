using System.Collections.Generic;
using UnityEngine;
public class CardDrop : MonoBehaviour
{
    public static CardDrop instance;
    public Queue<int> cardsQueue = new Queue<int>();
    public float posInCamera, posOutCamera;
    [SerializeField] RectTransform deckSliceAnimation;
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
    private void OnEnable()
    {
        LeanTween.moveY(gameObject.GetComponent<RectTransform>(), posInCamera, TimeMove()).setEase(TweenDeck);
    }
    public void TakeCard()
    {
        if (cardsQueue.Count > 0)
        {
            //Tomar carta del deck
            Deck.instance.SearchAviableSlots(cardsQueue.Dequeue());
            LeanTween.moveY(deckSliceAnimation, -82, TimeMove()).setEase(UIManager.instance.TweenDeckOut).setOnComplete(ResetTweenAnim);
            
        }
        if (cardsQueue.Count <= 0)
        {
            LeanTween.moveY(gameObject.GetComponent<RectTransform>(), posOutCamera, TimeMove()).setOnComplete(DeactivateDeck)
                    .setEase(UIManager.instance.TweenDeckOut);
        }
    }
    void DeactivateDeck()
    {
        gameObject.SetActive(false);
    }
    void ResetTweenAnim()
    {
        LeanTween.moveY(deckSliceAnimation, 1.7895f, 0);
    }
}