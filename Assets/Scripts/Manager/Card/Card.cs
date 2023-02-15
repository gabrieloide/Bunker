using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    private Vector3 scaleChange;
    private BoxCollider2D B2D;
    [SerializeField] Sprite defaultCard, backCard;
    public TowersData td;
    public bool canDrop;
    public bool onDrag;
    public int handIndex;
    private Deck dc;
    SpriteRenderer spriteRenderer;
    public bool ShowStatsCard;
    private void Start()
    {
        scaleChange = new Vector3(transform.localScale.x / 2f, transform.localScale.y / 2f, 0f);
        dc = FindObjectOfType<Deck>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        B2D = gameObject.GetComponent<BoxCollider2D>();
        onDrag = false;
    }
    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1) && !FindObjectOfType<Card>().onDrag)
        {
            //instanciar recuadro de stats de cartas
            UIManager.instance.ShowCardBox(td.Name, td.Description, transform.position);
        }
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
            Destroy(UIManager.instance.c);
        }
    }
    public virtual void OnMouseDrag()
    {
        Vector3 MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0f, 0.9f, 10f);
        gameObject.transform.position = MousePosition;
    }
    private void OnMouseDown()
    {

        spriteRenderer.sprite = backCard;
        Cursor.SetCursor(UIManager.instance.cursorTexture, Vector2.zero, CursorMode.Auto);
        LeanTween.alpha(gameObject, 0.87f, 0.3f);
        canDrop = true;
        onDrag = true;
        UIManager.instance.ShowTowerSlot = true;
        gameObject.transform.localScale -= scaleChange;
        B2D.size += new Vector2(0f, 2f);
        B2D.offset += new Vector2(0f, 1f);
    }
    private void OnMouseUp()
    {
        spriteRenderer.sprite = defaultCard;
        LeanTween.alpha(gameObject, 1f, 0.3f);
        UIManager.instance.TowerSlotAnimation.SetActive(false);
        Cursor.SetCursor(UIManager.instance.cursorDefault, Vector2.zero, CursorMode.Auto);//Cambiar de cursor al normal
        useCard();
    }
    void useCard()
    {
        //Obtiene la posicion del mouse
        Vector2 Mouseposition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //Calcula la distancia entre el slot de la carta y la posicion del mouse
        float d = Vector2.Distance(Deck.instance.cardSlots[handIndex].position, Mouseposition);
        if (canDrop && d > 1.5f && td.CardToInstantiate != null)
        {
            dc.availableCardSlots[handIndex] = true;
            Instantiate(td.CardToInstantiate, Mouseposition, transform.rotation);
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
}
