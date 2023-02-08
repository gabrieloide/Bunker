using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Transition : MonoBehaviour
{
    public float time;
    public GameObject circleTransition;
    private void Start()
    {
        ChangeTransitionIn();
    }
    public void ChangeTransitionIn()
    {
        LeanTween.scale(circleTransition, Vector3.zero, time);
    }
    public void ChangeTransitionOut()
    {
        circleTransition.SetActive(true);
        LeanTween.scale(circleTransition, Vector3.zero, time);
    }
}
