using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class towerSlotVerification : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Decoration")/* || collision.CompareTag("Turret")*/)
        {
            FindObjectOfType<Card>().canDrop = false;
        } 
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Decoration")/* || collision.CompareTag("Turret")*/)
        {
            FindObjectOfType<Card>().canDrop = true;
        }
    }
}
