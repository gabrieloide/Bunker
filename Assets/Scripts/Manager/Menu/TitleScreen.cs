using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScreen : MonoBehaviour
{
    [SerializeField] float timeToFade;
    [SerializeField] RectTransform objectToFade;
    [SerializeField] string SceneName;
    [SerializeField] Vector3 sizeWidthHeight;
    [SerializeField] LeanTweenType leanTweenType;

    void Update()
    {
        if (Input.anyKeyDown)
        {
            StartCoroutine(Transitions.FadeInTran(timeToFade, objectToFade, SceneName, sizeWidthHeight, leanTweenType, true));
        }        
    }
}
