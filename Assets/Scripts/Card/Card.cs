using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Card : MonoBehaviour
{
    [SerializeField] TowersData towerData;
    protected Sprite defaultCard, backCard;
    private Vector3 scaleChange;
    public int handIndex;
    private Deck dc;
    [HideInInspector] public bool onDrag;
    [HideInInspector] public bool canDrop;

    SpriteRenderer spriteRenderer;
    TypeOfCard typeOfCard = TypeOfCard.TurretCard;
    private void Start()
    {
        scaleChange = new Vector3(transform.localScale.x / 2f
                                , transform.localScale.y / 2f,
                                    0f);
        dc = FindObjectOfType<Deck>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
            transform.position = dc.cardSlots[handIndex].position;
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

        UIManager.instance.ShowLastCardPosition(transform.position, true);
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
            UIManager.instance.ShowLastCardPosition(transform.position, false);
            LeanTween.alpha(gameObject, 1f, 0.3f);
            UIManager.instance.TowerSlotAnimation.SetActive(false);
            useCard();
        }
        else
        {
            //Metti la lettera nel cestino
            Deck.instance.CardsInHand--;
            dc.availableCardSlots[handIndex] = true;
            Destroy(gameObject);
        }
    }
    void useCard()
    {
        if (towerData.CardToInstantiate != null)
        {
            //Usar carta
            dc.availableCardSlots[handIndex] = true;
            CardBehaviour();
            Destroy(gameObject);
        }
        else
        {
            onDrag = false;
            transform.position = dc.cardSlots[handIndex].position;
            gameObject.transform.localScale += scaleChange;
        }
    }
    void CardBehaviour()
    {
        Vector2 Mouseposition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Instantiate(towerData.CardToInstantiate, Mouseposition, transform.rotation);
    }
    public void showCard() => LeanTween.moveLocalY(gameObject, UIManager.instance.posInCamera, UIManager.instance.TimeMovement).
                              setEase(UIManager.instance.TweenDeckIn);
}
