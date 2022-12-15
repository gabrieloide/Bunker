using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerSlot : MonoBehaviour
{
    public bool slotAvailable;

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<Card>() != null && !collision.GetComponent<Card>().onDrag && slotAvailable)
        {
            Instantiate(collision.GetComponent<Card>().tower, transform.position, transform.rotation);
            Destroy(collision.gameObject);
            slotAvailable = false;
        }
    }
}