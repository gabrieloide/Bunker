using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckUpAnim : MonoBehaviour
{
    [SerializeField] GameObject rectTransform;
    private void OnEnable()
    {
        InitializedAnimation(rectTransform);
    }
    public void InitializedAnimation(GameObject rectTransform)
    {
        LeanTween.moveLocalY(rectTransform,
            0,
            0.7f).
            setEaseInCubic();
    }
}
