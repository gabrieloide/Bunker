using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool onDrag;
    public int ActualScore;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else Destroy(gameObject);
    }
    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Cursor.SetCursor(UIManager.instance.cursorTexture, Vector2.zero, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(UIManager.instance.cursorDefault, Vector2.zero, CursorMode.Auto);
        }
    }
    void Start()
    {
        AddCardToHand(1);
        AddCardToHand(1);
        AddCardToHand(1);
        AddCardToHand(1);
        AddCardToHand(1);
    }
    public void AddCardToHand(int cardNumber)
    {
        FindObjectOfType<CardDrop>().cardsQueue.Enqueue(cardNumber);
    }
}
