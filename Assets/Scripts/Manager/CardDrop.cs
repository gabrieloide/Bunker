using System.Collections.Generic;
using UnityEngine;
public class CardDrop : MonoBehaviour
{
    public static CardDrop instance;
    public Queue<int> cardsQueue = new Queue<int>();
    public float posInCamera, posOutCamera;
    [SerializeField] RectTransform deckSliceAnimation;
    public LeanTweenType TweenDeck;

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
        LeanTween.move(GetComponent<RectTransform>(), new Vector3(transform.position.x, posInCamera,default), 0.7f).setEase(TweenDeck);
    }
    public void TakeCard()
    {
        if (cardsQueue.Count > 0)
        {
            //Tomar carta del deck
            Deck.instance.SearchAviableSlots(cardsQueue.Dequeue());
            LeanTween.moveY(deckSliceAnimation, -300, 0.7f).setEase(UIManager.instance.TweenDeckOut).setOnComplete(ResetTweenAnim);
            
        }
        if (cardsQueue.Count <= 0)
        {
            LeanTween.moveY(gameObject, posOutCamera, 0.7f).setOnComplete(DeactivateDeck)
                    .setEase(UIManager.instance.TweenDeckOut);
        }
    }
    void DeactivateDeck()
    {
        gameObject.SetActive(false);
    }
    void ResetTweenAnim()
    {
        deckSliceAnimation.transform.position = transform.position + new Vector3(default, 4.7f, default);
    }
}