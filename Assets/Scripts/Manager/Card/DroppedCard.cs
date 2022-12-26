using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
    Card1,
    Card2,
    Card3,
    Card4
}

public class DroppedCard : MonoBehaviour
{
    public CardType cardType = CardType.Card1;
    [SerializeField]
    private float dissapearTime;

    private void Start()
    {
        Destroy(gameObject, dissapearTime);
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
            case CardType.Card4:
                Deck.instance.SearchAviableSlots(3);
                break;
        }
        Destroy(gameObject);
    }
}
