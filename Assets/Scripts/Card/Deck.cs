using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    public List<Card> deck = new List<Card>();
    public Transform[] cardSlots;
    public bool[] availableCardSlots;

    public static Deck instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void SearchAviableSlots(int card)
    {
        for(int i = 0; i < availableCardSlots.Length; i++)
        {
            if(availableCardSlots[i] == true)
            {
                Card newCard = Instantiate(deck[card], cardSlots[i].position, transform.rotation);
                newCard.handIndex = i;
                newCard.transform.SetParent(CameraMovement.instance.transform);
                availableCardSlots[i] = false;
                return;
            }
        }
    }
}
