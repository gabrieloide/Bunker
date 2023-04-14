using UnityEngine;

public class MenuManager : MonoBehaviour
{
    Transitions transitions;
    [SerializeField] RectTransform objectToFade;
    [SerializeField] float timeTofade;
    [SerializeField] string SceneName;
    [SerializeField] LeanTweenType easeTransitionOut;
    [SerializeField] LeanTweenType easeTransitionIn;

    private void Awake()
    {
        transitions = new Transitions();
    }
    private void Start()
    {
        StartCoroutine(transitions.FadeInTran(timeTofade, objectToFade, SceneName,
    Vector3.zero, easeTransitionIn,false));

    }
    public void PlayButton()
    {
        StartCoroutine(transitions.FadeInTran(timeTofade, objectToFade, SceneName,
            new Vector2(1024f, 768), easeTransitionOut,true));
    }
}