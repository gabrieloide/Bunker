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
            UIManager.instance.ShowCardBox(td.Name, td.Description, transform.position);
    }
    private void OnMouseEnter()
    {
        if (!onDrag)
        {
            gameObject.transform.position += new Vector3(0f, gameObject.transform.localScale.y / 2f, 0f);
        }
    }
    private void OnMouseExit()
    {
        if (!onDrag)
        {
            transform.position = dc.cardSlots[handIndex].position;
            Destroy(UIManager.instance.cardInstantiate);
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
        UIManager.instance.ShowLastCardPosition(transform.position);
        LeanTween.alpha(gameObject, 0.87f, 0.3f);
        canDrop = true;
        onDrag = true;
        UIManager.instance.ShowTowerSlot = true;
        gameObject.transform.localScale -= scaleChange;
    }
    private void OnMouseUp()
    {
        spriteRenderer.sprite = defaultCard;
        LeanTween.alpha(gameObject, 1f, 0.3f);
        UIManager.instance.TowerSlotAnimation.SetActive(false);
        Cursor.SetCursor(UIManager.instance.cursorDefault, Vector2.zero, CursorMode.Auto);
        useCard();
    }
    void useCard()
    {
        Vector2 Mouseposition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
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
        }
    }
}
