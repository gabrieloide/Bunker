using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDrop : MonoBehaviour
{
    public Queue<int> cardsQueue = new Queue<int>();
    [SerializeField] GameObject arrowTS;
    public float moveX;
    public float time;
    public LeanTweenType leanTweenType;
    public LeanTweenType leanTweenArrow;

    private void Update()
    {
        UIManager.instance.ShowDeckSlot(leanTweenType, leanTweenArrow, cardsQueue.Count, GetComponent<RectTransform>(),arrowTS.GetComponent<RectTransform>());
    }
    private void OnMouseDown()
    {
        if (cardsQueue.Count > 1)
        {
            Debug.Log("HH");
            Deck.instance.SearchAviableSlots(cardsQueue.Dequeue());
        }
    }
}
