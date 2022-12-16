using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public GameObject tower;
    private Vector3 scaleChange;
    private BoxCollider2D B2D;

    private bool trashCard;
    public bool onDrag;

    public int handIndex;

    private Deck dc;
    [SerializeField] CursorType cursor, defaultCursor;
        

    private void Start()
    {
        dc = FindObjectOfType<Deck>();
        scaleChange = new Vector3(transform.localScale.x / 3f, transform.localScale.y / 3f, 0f);
        B2D = gameObject.GetComponent<BoxCollider2D>();
        onDrag = false;
    }

    private void OnMouseEnter()
    {
        if (!onDrag)
        {
            Cursor.SetCursor(cursor.cursorTexture, cursor.cursorHotspot, CursorMode.Auto);
            gameObject.transform.position += new Vector3 (0f , gameObject.transform.localScale.y/5f, 0f);
            B2D.size += new Vector2(0f, 0.2f);
            B2D.offset += new Vector2(0f, -0.1f);
        }
    }
    private void OnMouseExit()
    {
        if (!onDrag)
        {
            Cursor.SetCursor(defaultCursor.cursorTexture, defaultCursor.cursorHotspot, CursorMode.Auto);
            transform.position = dc.cardSlots[handIndex].position;
            B2D.size -= new Vector2(0f, 0.2f);
            B2D.offset -= new Vector2(0f, -0.1f);
        }
    }
    
    private void OnMouseDrag()
    {
        gameObject.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3 (0f ,1.5f, 10f);
    }

    private void OnMouseDown()
    {
        onDrag = true;
        gameObject.transform.localScale -= scaleChange;
    }

    private void OnMouseUp()
    {
        if (trashCard)
        {
            dc.availableCardSlots[handIndex] = true;
            Destroy(gameObject);
        }
        else
        {
            onDrag = false;
            transform.position = dc.cardSlots[handIndex].position;
            gameObject.transform.localScale += scaleChange;
        }
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Trash"))
            trashCard = true;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Trash"))
            trashCard = false;
    }
}