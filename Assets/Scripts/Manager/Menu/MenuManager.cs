using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;
    [SerializeField] RectTransform objectToFade;
    [SerializeField] float timeTofade;
    [SerializeField] string SceneName;
    [SerializeField] LeanTweenType easeTransitionOut;
    [SerializeField] LeanTweenType easeTransitionIn;
    [SerializeField] Vector3 WitdhHeight;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        StartCoroutine(Transitions.FadeInTran(timeTofade, objectToFade, SceneName,
    Vector3.zero, easeTransitionIn, false));

    }
    public void PlayButton()
    {
        StartCoroutine(Transitions.FadeInTran(timeTofade, objectToFade, SceneName,
            WitdhHeight, easeTransitionOut, true));
    }
}