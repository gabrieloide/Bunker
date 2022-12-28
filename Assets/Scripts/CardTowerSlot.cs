using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardTowerSlot : MonoBehaviour
{
    bool inHand;
    bool canDrop;
    public GameObject CardToInstantiate;
    private void OnMouseDown()
    {
            canDrop = true;
    }
    private void OnMouseUp()
    {
        Vector2 Mouseposition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float d = Vector2.Distance(transform.position, Mouseposition);
        if (canDrop && d > 1.5f)
        {
            Debug.Log("Puede dropear");
            Instantiate(CardToInstantiate, Mouseposition, transform.rotation);
            Destroy(gameObject);
        }
    }
}
