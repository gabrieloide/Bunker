using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Card : MonoBehaviour
{
    private Vector3 scaleChange;
    private BoxCollider2D B2D;
    bool canDrop;
    public GameObject CardToInstantiate;
    public bool onDrag;
    public int handIndex;
    private Deck dc;

    private void Start()
    {
        dc = FindObjectOfType<Deck>();
        scaleChange = new Vector3(transform.localScale.x / 2f, transform.localScale.y / 2f, 0f);
        B2D = gameObject.GetComponent<BoxCollider2D>();
        onDrag = false;
    }
    private void OnMouseEnter()
    {
        if (!onDrag)
        {
            gameObject.transform.position += new Vector3(0f, gameObject.transform.localScale.y / 5f, 0f);
            B2D.size += new Vector2(0f, 0.2f);
            B2D.offset += new Vector2(0f, -0.1f);
        }
    }
    private void OnMouseExit()
    {
        if (!onDrag)
        {
            transform.position = dc.cardSlots[handIndex].position;
            B2D.size -= new Vector2(0f, 0.2f);
            B2D.offset -= new Vector2(0f, -0.1f);
        }
    }
    public virtual void OnMouseDrag()
    {
        Vector3 MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0f, 0.9f, 10f);
        gameObject.transform.position = MousePosition;
    }

    private void OnMouseDown()
    {
        Cursor.SetCursor(UIManager.instance.cursorTexture, UIManager.instance.cursorHotspot, CursorMode.Auto);
        canDrop = true;
        onDrag = true;
        gameObject.transform.localScale -= scaleChange;
        
    }
    private void OnMouseUp()
    {
        //Obtiene la posicion del mouse
        Vector2 Mouseposition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Cursor.SetCursor(UIManager.instance.cursorDefault, UIManager.instance.cursorHotspot, CursorMode.Auto);
        //Calcula la distancia entre el slot de la carta y la posicion del mouse
        float d = Vector2.Distance(Deck.instance.cardSlots[handIndex].position, Mouseposition);

        if (canDrop && d > 1.5f && CardToInstantiate != null)
        {
             dc.availableCardSlots[handIndex] = true;
             Instantiate(CardToInstantiate, Mouseposition, transform.rotation);
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
        if (collision.CompareTag("Decoration")) canDrop = false;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Decoration")) canDrop = true;
    }
}
