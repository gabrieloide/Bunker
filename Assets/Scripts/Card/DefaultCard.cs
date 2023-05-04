using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultCard : MonoBehaviour
{
    [SerializeField]Vector3 cardPos;
    Deck deck;
    private void OnEnable() 
    {
        LeanTween.scale(gameObject, Vector2.one, 0.3f).setEaseInBack();
    }
}
