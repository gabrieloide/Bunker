using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Transition.instance.ChangeTransitionIn();
        FindObjectOfType<CardDrop>().cardsQueue.Enqueue(0);
        FindObjectOfType<CardDrop>().cardsQueue.Enqueue(0);
        FindObjectOfType<CardDrop>().cardsQueue.Enqueue(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
