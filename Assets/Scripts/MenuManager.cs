using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MenuManager : MonoBehaviour
{
    void changeScene(string SceneName)
    {
        SceneManager.LoadScene(SceneName);
        FindObjectOfType<Transition>().ChangeTransitionOut();
    }
    public void PlayMainMenu()
    {
        changeScene("MainMenu");
    }
    public void PlayGame()
    {
        changeScene("SceneGame");
    }
    public void PlayCredits()
    {
        changeScene("SceneGame");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
