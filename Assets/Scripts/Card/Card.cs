using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    [SerializeField]
    private GameObject towwer;
    private Vector3 scaleChange;
    private BoxCollider2D B2D;

    public int handIndex;

    private Deck dc;

    private void Start()
    {
        dc = FindObjectOfType<Deck>();
        scaleChange = new Vector3(transform.localScale.x / 3f, transform.localScale.y / 3f, 0f);
        B2D = gameObject.GetComponent<BoxCollider2D>();
    }

    private void OnMouseEnter()
    {
        gameObject.transform.position += new Vector3 (0f , gameObject.transform.localScale.y/5f, 0f);
        B2D.size += new Vector2(0f, 0.2f);
        B2D.offset += new Vector2(0f, -0.1f);
    }
    private void OnMouseExit()
    {
        transform.position = dc.cardSlots[handIndex].position;
        B2D.size -= new Vector2(0f, 0.2f);
        B2D.offset -= new Vector2(0f, -0.1f);
    }
    
    private void OnMouseDrag()
    {
        gameObject.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3 (0f ,0f, 10f);
    }

    private void OnMouseDown()
    {
        gameObject.transform.localScale -= scaleChange;
    }

    private void OnMouseUp()
    {
        if (false)
        {
            dc.availableCardSlots[handIndex] = true;
        }
        else
        {
            transform.position = dc.cardSlots[handIndex].position;
        gameObject.transform.localScale += scaleChange;
        }
    }
}
