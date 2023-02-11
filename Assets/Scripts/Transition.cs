using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Transition : MonoBehaviour
{
    public static Transition instance;
    public float time;
    public GameObject circleTransition;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    public void ChangeTransitionIn()
    {
        LeanTween.scale(circleTransition, Vector3.zero, time);
    }
    public void ChangeTransitionOutGame()
    {
        circleTransition.SetActive(true);
        LeanTween.scale(circleTransition, new Vector3(20,20,20), time).setOnComplete(Scene);
    }
    void Scene()
    {
        SceneManager.LoadScene("SceneGame");
    }

    public void ChangeTransitionOutMainMenu()
    {
        circleTransition.SetActive(true);
        LeanTween.scale(circleTransition, new Vector3(20, 20, 20), time).setOnComplete(MainMenu);
    }
    void MainMenu()
    {
        SceneManager.LoadScene("SceneGame");
    }
}
