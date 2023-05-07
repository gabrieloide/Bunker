using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultCard : MonoBehaviour
{
    private void Awake() => transform.localScale = Vector2.zero;
    private void OnEnable() 
    {
        LeanTween.scale(gameObject, Vector2.one, 0.3f).setEaseInBack();
    }
}
