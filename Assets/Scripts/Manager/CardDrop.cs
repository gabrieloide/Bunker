using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CardDrop : MonoBehaviour
{
    public static CardDrop instance;
    public Queue<int> cardsQueue = new Queue<int>();
    [SerializeField] GameObject arrowTS;
    public float moveX;
    public LeanTweenType leanTweenType;
    [SerializeField] RectTransform rectTransform;
    public float InitialMoveX, ReturnMoveX;
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
    private void Update()
    {
        ShowDeckSlot();
    }
    public void ShowDeckSlot()
    {
        if (cardsQueue.Count != 0)
        {
            //Mostrar deck para tomar carta
            
            LeanTween.moveX(rectTransform, InitialMoveX, 0.7f).setEase(leanTweenType);
        }
        else
        {
            //No mostrar deck para tomar carta
            LeanTween.moveX(rectTransform, ReturnMoveX, 0.7f).setEase(leanTweenType);
        }
    }
    public void TakeCard()
    {
        if (cardsQueue.Count >= 1)
        {
            //Tomar carta del deck
            Deck.instance.SearchAviableSlots(cardsQueue.Dequeue());
        }
    }
}
