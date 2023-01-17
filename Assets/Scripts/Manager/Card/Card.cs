using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    
public class Card : MonoBehaviour
{
    private Vector3 scaleChange;
    private BoxCollider2D B2D;
    [SerializeField]
    bool canDrop;
    public GameObject CardToInstantiate;

    public bool onDrag;

    public int handIndex;

    private Deck dc;
   //[SerializeField] CursorType cursor, defaultCursor;


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
            //Cursor.SetCursor(cursor.cursorTexture, cursor.cursorHotspot, CursorMode.Auto);
            gameObject.transform.position += new Vector3(0f, gameObject.transform.localScale.y / 5f, 0f);
            B2D.size += new Vector2(0f, 0.2f);
            B2D.offset += new Vector2(0f, -0.1f);
        }
    }
    private void OnMouseExit()
    {
        if (!onDrag)
        {
            //Cursor.SetCursor(defaultCursor.cursorTexture, defaultCursor.cursorHotspot, CursorMode.Auto);
            transform.position = dc.cardSlots[handIndex].position;
            B2D.size -= new Vector2(0f, 0.2f);
            B2D.offset -= new Vector2(0f, -0.1f);
        }
    }
    public virtual void OnMouseDrag()
    {
        gameObject.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0f, 0.2f, 10f);
    }

    private void OnMouseDown()
    {
        canDrop = true;
        onDrag = true;
        gameObject.transform.localScale -= scaleChange;
        B2D.size += new Vector2(0f, 2f);
        B2D.offset += new Vector2(0f, 1f);
    }
    private void OnMouseUp()
    {
        //Obtiene la posicion del mouse
        Vector2 Mouseposition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //Calcula la distancia entre el slot de la carta y la posicion del mouse
        float d = Vector2.Distance(Deck.instance.cardSlots[handIndex].position, Mouseposition);

        if (canDrop && d > 1.5f && CardToInstantiate != null)
        {
             Instantiate(CardToInstantiate, Mouseposition, transform.rotation);
             dc.availableCardSlots[handIndex] = true;
             Destroy(gameObject);
        }
        else
        {
            onDrag = false;
            transform.position = dc.cardSlots[handIndex].position;
            gameObject.transform.localScale += scaleChange;
            B2D.size -= new Vector2(0f, 2f);
            B2D.offset -= new Vector2(0f, 1f);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Decoration") || collision.CompareTag("Turret"))
        {
            canDrop = false;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Decoration") || collision.CompareTag("Turret"))
        {
            canDrop = true;
        }
    }
}
