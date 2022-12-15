using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
    Card1,
    Card2,
    Card3
}

public class DroppedCard : MonoBehaviour
{
    public CardType cardType = CardType.Card1;
    [SerializeField]
    private float dissapearTime;

    private void Start()
    {
        Invoke("DestroyInTime", dissapearTime);
    }

    private void OnMouseDown()
    {
        switch (cardType)
        {
            case CardType.Card1:
                Deck.instance.SearchAviableSlots(0);
                break;
            case CardType.Card2:
                Deck.instance.SearchAviableSlots(1);
                break;
            case CardType.Card3:
                Deck.instance.SearchAviableSlots(2);
                break;
        }
        Destroy(gameObject);
    }

    private void DestroyInTime()
    {
        Destroy(gameObject);
    }
}
