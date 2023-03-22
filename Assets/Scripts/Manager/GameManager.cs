using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
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
        Transition.instance.ChangeTransitionIn();
        AddCardToHand(8);
        AddCardToHand(8);
        AddCardToHand(8);
    }
    public void AddCardToHand(int cardNumber)
    {
        FindObjectOfType<CardDrop>().cardsQueue.Enqueue(cardNumber);
    }
}
