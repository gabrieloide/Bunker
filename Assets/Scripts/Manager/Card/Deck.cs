using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    [SerializeField] towerSlotVerification TowerSlotVerification;
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
        for (int i = 0; i < availableCardSlots.Length; i++)
        {
            if (availableCardSlots[i] == true)
            {
                Card newCard = Instantiate(deck[card], cardSlots[i].position - 
                                                                            new Vector3(default,
                                                                            5.5f,
                                                                            transform.position.z), transform.rotation);
                newCard.handIndex = i;
                TowerSlotVerification.card.Add(newCard);
                newCard.transform.SetParent(CameraMovement.instance.transform);
                //Agregar carta a la mano

                availableCardSlots[i] = false;
                return;
            }
        }
    }
}
