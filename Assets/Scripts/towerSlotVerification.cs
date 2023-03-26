using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class towerSlotVerification : MonoBehaviour
{
    public List<Card> card = new List<Card>();
    [SerializeField] LayerMask DecorationLayer;
    public float widthBox;
    public float heightBox;
    private void Update()
    {
        if (card.Count > 1)
        {
            CheckTouchDecoration();
            RemoveCardFromList();
        }
    }
    public void CheckTouchDecoration()
    {
        foreach (var item in card)
        {
            RaycastHit2D raycastHit2d = Physics2D.BoxCast(transform.position, new Vector2(widthBox, heightBox), 0, Vector2.one, 0.7f, DecorationLayer);
            if (!raycastHit2d)
            {
                item.canDrop = true;
            }
            else
            {
                item.canDrop = false;
            }
        }
    }
    public void RemoveCardFromList()
    {
        for (int i = 0; i < card.Count; i++)
        {
            if (card[i]==null)
            {
                card.RemoveAt(i);
            }
        }
    }
}

