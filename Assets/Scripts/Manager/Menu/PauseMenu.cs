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
}
