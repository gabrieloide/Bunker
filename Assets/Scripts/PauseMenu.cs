using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;
    public GameObject pauseMenuUI, inGameMenu;
    [SerializeField]
    private GameObject panelPauseMain, panelPauseOptions;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)|| Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            SlowTime();
        }
    }
    public void Resume()
    {
        inGameMenu.SetActive(true);
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }
    void Pause()
    {
        inGameMenu.SetActive(false);
        pauseMenuUI.SetActive(true);
        panelPauseMain.SetActive(true);
        panelPauseOptions.SetActive(false);
        Time.timeScale = 0f;
        GameIsPaused = true;
    }

    public void SlowTime()
    {
        Time.timeScale = 0.1f;
    }
}
