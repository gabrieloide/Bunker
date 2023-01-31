using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
    Card1,
    Card2,
    Card3,
    Card4,
    Card5,
    Card6,
    Card7,
    Card8,
    Card9
}

public class DroppedCard : MonoBehaviour
{
    public CardType cardType = CardType.Card1;
    [SerializeField]
    private float dissapearTime;

    [SerializeField]TowersData towersData;
    
    private void Start()
    {
        Destroy(gameObject, dissapearTime);
    }
    private void OnMouseEnter()
    {
        UIManager.instance.showStatsCards(towersData.Name, transform.position);

    }
    private void OnMouseExit()
    {

        UIManager.instance.Stats.SetActive(false);
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
            case CardType.Card5:
                Deck.instance.SearchAviableSlots(4);
                break;
            case CardType.Card6:
                Deck.instance.SearchAviableSlots(5);
                break;
            case CardType.Card7:
                Deck.instance.SearchAviableSlots(6);
                break;
            case CardType.Card8:
                Deck.instance.SearchAviableSlots(7);
                break;
            case CardType.Card9:
                Deck.instance.SearchAviableSlots(8);
                break;
        }
        UIManager.instance.Stats.SetActive(false);
        Destroy(gameObject);
    }
}
