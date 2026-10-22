using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Transitions
{
    public static IEnumerator FadeInTran(float timeToFade, RectTransform objectToFade,
        string SceneName, Vector3 sizeWidthHeight, LeanTweenType EaseTransition, bool changeScene)
    {
        if (changeScene)
        {
            CRTTransition.LoadScene(SceneName);
            yield break;
        }

        if (objectToFade != null)
        {
            LeanTween.size(objectToFade, sizeWidthHeight, timeToFade).setEase(EaseTransition);
            yield return new WaitForSeconds(timeToFade);
        }
    }
}