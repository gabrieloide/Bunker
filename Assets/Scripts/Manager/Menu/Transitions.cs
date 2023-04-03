using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Transitions
{
    public IEnumerator FadeInTran(float timeToFade, RectTransform objectToFade,
        string SceneName, Vector3 sizeWidthHeight,LeanTweenType EaseTransition, bool changeScene)
    {
        LeanTween.size(objectToFade, sizeWidthHeight, timeToFade).setEase(EaseTransition);
        yield return new WaitForSeconds(timeToFade);
        if (changeScene)
        {
            SceneManager.LoadScene(SceneName);
        }
    }
}
