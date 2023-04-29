using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Card : MonoBehaviour
{
    [SerializeField] protected LayerMask objectLayerMask;
    [SerializeField] protected TowersData towerData;
    [SerializeField] Sprite defaultCard, backCard;
    private Vector3 scaleChange;
    private Deck dc;
    protected bool onDrag;
    private int index() => GetComponent<CardIndex>().HandIndex;
    protected abstract bool ReturnToHand();
    private void Start()
    {
        scaleChange = new Vector3(transform.localScale.x / 2f
                                , transform.localScale.y / 2f,
                                    0f);
        dc = FindObjectOfType<Deck>();
        //spriteRenderer = GetComponent<SpriteRenderer>();
        onDrag = false;
        showCard();
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1) && !FindObjectOfType<Card>().onDrag)
            UIManager.instance.ShowCardBox(towerData.Name, towerData.Description, transform.position);
    }
    private void OnMouseEnter()
    {
        if (!onDrag)
        {
            //MOUSE ENCIMA DE LA CARTA

            gameObject.transform.position += new Vector3(0f, gameObject.transform.localScale.y / 2f, 0f);
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
        gameObject.transform.position = MousePosition;
    }
    private void OnMouseDown()
    {
        //TOMAR CARTA
        UIManager.instance.ShowLastCardPosition(transform.position, !ReturnToHand());
        LeanTween.alpha(gameObject, 0.87f, 0.3f);
        onDrag = true;
        UIManager.instance.ShowTowerSlot = true;
        gameObject.transform.localScale -= scaleChange;
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
            gameObject.transform.localScale += scaleChange;
        }
    }
    protected abstract void CardBehaviour();
    public void showCard() => LeanTween.moveLocalY(gameObject, UIManager.instance.posInCamera, UIManager.instance.TimeMovement).
                              setEase(UIManager.instance.TweenDeckIn);
}
