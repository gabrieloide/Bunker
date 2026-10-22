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
        if (rectTransform == null) return;
        LeanTween.cancel(rectTransform);
        LeanTween.moveLocalY(rectTransform,
            0,
            0.7f).
            setEaseInCubic();
    }
}
