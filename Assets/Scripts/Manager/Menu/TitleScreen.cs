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
    [SerializeField] CRTTurnOnEffect turnOnEffect;

    bool isTransitioning;

    void Update()
    {
        if (isTransitioning) return;

        // Wait until the CRT TV turning on animation completes
        if (turnOnEffect != null && turnOnEffect.IsPlaying) return;

        if (Input.anyKeyDown)
        {
            isTransitioning = true;
            StartCoroutine(Transitions.FadeInTran(timeToFade, objectToFade, SceneName, sizeWidthHeight, leanTweenType, true));
        }        
    }
}
