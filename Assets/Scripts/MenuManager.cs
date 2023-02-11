using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public void PlayMainMenu()
    {
        Transition.instance.ChangeTransitionOutMainMenu();
    }
    public void PlayGame()
    {
        Transition.instance.ChangeTransitionOutGame();
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
