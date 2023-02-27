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
    void Start()
    {
        Transition.instance.ChangeTransitionIn();
        FindObjectOfType<CardDrop>().cardsQueue.Enqueue(8);
        FindObjectOfType<CardDrop>().cardsQueue.Enqueue(8);
        FindObjectOfType<CardDrop>().cardsQueue.Enqueue(8);
    }
}
