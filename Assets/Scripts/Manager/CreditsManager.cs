using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsManager : MonoBehaviour
{
    void Start()
    {
        Invoke("PlayMainMenu", 5f);
    }
    public void PlayMainMenu()
    {
        CRTTransition.LoadScene("MainMenu 2");
    }
}
