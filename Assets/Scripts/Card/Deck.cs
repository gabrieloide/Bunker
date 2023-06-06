using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Deck : MonoBehaviour
{
    public List<CardIndex> deck = new List<CardIndex>();
    public Transform[] cardSlots;
    public bool[] availableCardSlots;
    public static Deck instance;
    [HideInInspector] public int CardsInHand;
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
    public void SearchAviableSlots(int card)
    {
        for (int i = 0; i < availableCardSlots.Length; i++)
        {
            if (availableCardSlots[i] == true)
            {
                CardIndex newCard = Instantiate(deck[card], cardSlots[i].position - new Vector3(default,
                                                                                5.5f,
                                                                                transform.position.z),
                                                                                transform.rotation);
                CardsInHand++;
                newCard.HandIndex = i;
                GameManager.instance.CurrentCardAmount++;
                LeanTween.moveY(newCard.gameObject, -5.57f, 0.3f);
                newCard.transform.SetParent(CameraMovement.instance.transform);
                //Agregar carta a la mano

                availableCardSlots[i] = false;
                return;
            }
        }
    }
}
