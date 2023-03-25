using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class towerSlotVerification : MonoBehaviour
{
    public Card[] card = new Card[7];
    private void Update()
    {
        for (int i = 0; i < card.Length; i++)
        {

        }
    }
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Decoration") || collision.CompareTag("Turret"))
        {
            //card.canDrop = false;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Decoration") || collision.CompareTag("Turret"))
        {
            //card.canDrop = true;
        }
    }
}

