using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Card : MonoBehaviour
{
    private Vector3 scaleChange;
    [SerializeField] Sprite defaultCard, backCard;
    public TowersData td;
    public bool canDrop;
    public bool onDrag;
    public int handIndex;
    private Deck dc;
    SpriteRenderer spriteRenderer;
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
            UIManager.instance.ShowCardBox(td.Name, td.Description, transform.position);
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
    public void OnMouseDrag()
    {
        //ARRASTRAR CARTA

        Vector3 MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0f, 0.9f, 10f);
        gameObject.transform.position = MousePosition;
    }
    private void OnMouseDown()
    {
        //TOMAR CARTA

        spriteRenderer.sprite = backCard;
        UIManager.instance.ShowLastCardPosition(transform.position, true);
        LeanTween.alpha(gameObject, 0.87f, 0.3f);
        canDrop = true;
        onDrag = true;
        UIManager.instance.ShowTowerSlot = true;
        gameObject.transform.localScale -= scaleChange;
    }
    private void OnMouseUp()
    {
        if (!FindObjectOfType<Trash>().hit2D)
        {
            spriteRenderer.sprite = defaultCard;
            UIManager.instance.ShowTowerSlot = false;
            UIManager.instance.ShowLastCardPosition(transform.position, false);
            LeanTween.alpha(gameObject, 1f, 0.3f);
            UIManager.instance.TowerSlotAnimation.SetActive(false);
            useCard();
        }
        else
        {
            //Metti la lettera nel cestino

            dc.availableCardSlots[handIndex] = true;
            Destroy(gameObject);
        }
    }
    void useCard()
    {
        Vector2 Mouseposition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (canDrop && td.CardToInstantiate != null)
        {
            //Usar carta

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
    public void showCard() => LeanTween.moveLocalY(gameObject, UIManager.instance.posInCamera, UIManager.instance.TimeMovement).
                              setEase(UIManager.instance.TweenDeckIn);
}