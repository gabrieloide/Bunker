using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Card : MonoBehaviour
{
    [SerializeField] protected LayerMask objectLayerMask;
    public TowersData towerData;
    [SerializeField] Sprite defaultCard, backCard;
    private Vector3 scaleChange() => new Vector3(transform.localScale.x / 2f
                                , transform.localScale.y / 2f,
                                    0f);
    protected Deck dc;
    [SerializeField] protected Vector3 offset = new Vector3(0, -0.5f, 0);
    [SerializeField]protected float width = 1.2f;
    [SerializeField]protected float height = 1;
    protected virtual RaycastHit2D DetectObjectsBelow() => Physics2D.BoxCast(transform.position + offset, new Vector2(width, height), 0f, Vector2.down, 0.1f, objectLayerMask);
    [HideInInspector] public int index() => GetComponent<CardIndex>().HandIndex;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + offset, new Vector2(width, height));
    }
    private void Start()
    {
        dc = FindObjectOfType<Deck>();
        //spriteRenderer = GetComponent<SpriteRenderer>();
        showCard();
    }
    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1) && !GameManager.instance.onDrag)
            UIManager.instance.ShowCardBox(towerData.Name, towerData.Description, transform.position);
    }
    private void OnMouseEnter()
    {
        if (!GameManager.instance.onDrag)
        {
            //MOUSE ENCIMA DE LA CARTA

            transform.position += new Vector3(0f, transform.localScale.y / 2f, 0f);
        }
    }
    private void OnMouseExit()
    {
        if (!GameManager.instance.onDrag)
        {
            // MOUSE CUANDO SALE DE LA CARTA    
            transform.position = dc.cardSlots[index()].position;
            Destroy(UIManager.instance.cardInstantiate);
        }
    }
    private void OnMouseDrag()
    {
        //ARRASTRAR CARTA

        Vector3 MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0f, 0.9f, 10f);
        transform.position = MousePosition;
    }
    private void OnMouseDown()
    {
        //TOMAR CARTA

        LeanTween.alpha(gameObject, 0.87f, 0.3f);
        GameManager.instance.onDrag = true;
        UIManager.instance.ShowTowerSlot = true;
        transform.localScale -= scaleChange();
        UIManager.instance.ShowLastCardPosition(dc.cardSlots[index()].position);
    }
    private void OnMouseUp()
    {
        if (!FindObjectOfType<Trash>().hit2D)
        {
            LeanTween.alpha(gameObject, 1f, 0.3f);
            UIManager.instance.ShowTowerSlot = false;
            UIManager.instance.TowerSlotAnimation.SetActive(false);
            GameManager.instance.onDrag = false;
            UIManager.instance.ShowLastCardPosition(dc.cardSlots[index()].position);
            spawnCard();

        }
        else
        {
            //Metti la lettera nel cestino
            UIManager.instance.ShowTowerSlot = false;
            Deck.instance.CardsInHand--;
            dc.availableCardSlots[index()] = true;
            Destroy(gameObject);
        }
    }
    protected virtual void spawnCard()
    {
        if (!DetectObjectsBelow())
        {
            //Usar carta
            dc.availableCardSlots[index()] = true;
            CardBehaviour();
            Destroy(gameObject);
        }
        else
        {
            transform.position = dc.cardSlots[index()].position;
            transform.localScale = Vector3.one;
        }
    }
    protected abstract void CardBehaviour();
    public void showCard() => LeanTween.moveLocalY(gameObject, UIManager.instance.posInCamera, UIManager.instance.TimeMovement).
                              setEase(UIManager.instance.TweenDeckIn);
}
