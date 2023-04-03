using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChangeCardText : MonoBehaviour
{
    public TMP_Text CardName;
    public TMP_Text CardDescription;
    public static ChangeCardText instance;
    [SerializeField] float moveY;
    [SerializeField] float time;
    public LeanTweenType leanTweenType;
    void Start()
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
    public void instantiateStats(string textName, string textDescription)
    {
        CardName.text = textName;
        CardDescription.text = textDescription;
        Vector3 sc = new Vector3(0.25f, 0.25f, 0.25f);
        LeanTween.scale(gameObject, sc, 0.2f);
        LeanTween.moveLocalY(this.gameObject, moveY, time).setEase(leanTweenType);
    }
}
