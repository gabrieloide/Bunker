using UnityEngine;
using UnityEngine.UIElements;

// Pause screen (UI/Pause.uxml): P or Escape toggles it and freezes the game.
[RequireComponent(typeof(UIDocument))]
public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;
    [SerializeField] string creditsScene = "Credits";

    VisualElement pauseMenuUI, panelPauseMain, panelPauseOptions;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        // Nested under the HUD document: fill the screen without catching clicks meant for the game
        root.StretchToParentSize();
        root.pickingMode = PickingMode.Ignore;
        pauseMenuUI = root.Q("pause");
        panelPauseMain = root.Q("main");
        panelPauseOptions = root.Q("options");

        root.Q<Button>("resume").clicked += Resume;
        root.Q<Button>("settings").clicked += () => ShowOptions(true);
        root.Q<Button>("credits").clicked += () => Leave(() => CRTTransition.LoadScene(creditsScene));
        root.Q<Button>("quit").clicked += () => Leave(LevelProgress.BackToSelector);
        OptionSettings.Bind(panelPauseOptions, () => ShowOptions(false));
    }

    void OnDisable()
    {
        if (GameIsPaused) Resume();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
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
        pauseMenuUI.AddToClassList("hidden");
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    void Pause()
    {
        pauseMenuUI.RemoveFromClassList("hidden");
        ShowOptions(false);
        Time.timeScale = 0f;
        GameIsPaused = true;
    }

    void ShowOptions(bool show)
    {
        panelPauseMain.EnableInClassList("hidden", show);
        panelPauseOptions.EnableInClassList("hidden", !show);
    }

    void Leave(System.Action load)
    {
        GameIsPaused = false;
        load();
    }
}
