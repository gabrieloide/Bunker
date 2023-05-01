using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Card : MonoBehaviour
{
    [SerializeField] protected LayerMask objectLayerMask;
    [SerializeField] protected TowersData towerData;
    [SerializeField] Sprite defaultCard, backCard;
    private Vector3 scaleChange() => new Vector3(transform.localScale.x / 2f
                                , transform.localScale.y / 2f,
                                    0f);
    private Deck dc;
    protected bool onDrag = false;
    private int index() => GetComponent<CardIndex>().HandIndex;
    protected abstract bool ReturnToHand();
    private void Start()
    {
        dc = FindObjectOfType<Deck>();
        //spriteRenderer = GetComponent<SpriteRenderer>();
        showCard();
    }
    private void Update() => UIManager.instance.ShowLastCardPosition(dc.cardSlots[index()].position, onDrag);
    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1) && !onDrag)
            UIManager.instance.ShowCardBox(towerData.Name, towerData.Description, transform.position);
    }
    private void OnMouseEnter()
    {
        if (!onDrag)
        {
            //MOUSE ENCIMA DE LA CARTA

            transform.position += new Vector3(0f, transform.localScale.y / 2f, 0f);
        }
    }
    private void OnMouseExit()
    {
        if (!onDrag)
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
        onDrag = true;
        UIManager.instance.ShowTowerSlot = true;
        transform.localScale -= scaleChange();
    }
    private void OnMouseUp()
    {
        if (!FindObjectOfType<Trash>().hit2D)
        {
            UIManager.instance.ShowTowerSlot = false;
            LeanTween.alpha(gameObject, 1f, 0.3f);
            UIManager.instance.TowerSlotAnimation.SetActive(false);
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
    void spawnCard()
    {
        if (!ReturnToHand())
        {
            //Usar carta
            dc.availableCardSlots[index()] = true;
            CardBehaviour();
            Destroy(gameObject);
        }
        else
        {
            onDrag = false;
            transform.position = dc.cardSlots[index()].position;
            transform.localScale = Vector3.one;
        }
    }
    protected abstract void CardBehaviour();
    public void showCard() => LeanTween.moveLocalY(gameObject, UIManager.instance.posInCamera, UIManager.instance.TimeMovement).
                              setEase(UIManager.instance.TweenDeckIn);
}
