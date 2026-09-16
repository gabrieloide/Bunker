using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool onDrag;
    public int ActualScore;
    [HideInInspector]public int CurrentCardAmount;
    private CardDrop cardDrop;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();
    }

    private void Start()
    {
        if (UIManager.instance != null && UIManager.instance.cursorDefault != null)
        {
            Cursor.SetCursor(UIManager.instance.cursorDefault, Vector2.zero, CursorMode.Auto);
        }

        AddCardToHand(3);
        AddCardToHand(3);
        AddCardToHand(3);
        AddCardToHand(9);
        AddCardToHand(9);
        AddCardToHand(9);
    }

    private void Update()
    {
        if (UIManager.instance == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.SetCursor(UIManager.instance.cursorTexture, Vector2.zero, CursorMode.Auto);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Cursor.SetCursor(UIManager.instance.cursorDefault, Vector2.zero, CursorMode.Auto);
        }
    }

    public void AddCardToHand(int cardNumber)
    {
        if (cardDrop == null)
        {
            cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();
        }

        if (cardDrop != null)
        {
            cardDrop.cardsQueue.Enqueue(cardNumber);
        }
    }
}
