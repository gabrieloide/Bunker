using UnityEngine;
using UnityEngine.UIElements;

// Pause screen (UI/Pause.uxml): P or Escape toggles it and freezes the game.
[RequireComponent(typeof(UIDocument))]
public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;
    [SerializeField] string creditsScene = "Credits";

    VisualElement pauseMenuUI, panelPauseMain, panelPauseOptions;

    void Awake()
    {
        EnsureCompositedSettings();
    }

    void OnEnable()
    {
        EnsureCompositedSettings();
        BindUI();
    }

    public void EnsureCompositedSettings()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;
        var targetSettings = UIManager.instance != null ? UIManager.instance.HudPanelSettings : null;
        if (targetSettings != null && doc.panelSettings != targetSettings)
        {
            doc.panelSettings = targetSettings;
            BindUI();
        }
    }

    void BindUI()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;
        var root = doc.rootVisualElement;
        if (root == null) return;

        // Fill the screen without catching clicks meant for the game
        root.StretchToParentSize();
        root.pickingMode = PickingMode.Ignore;
        pauseMenuUI = root.Q("pause");
        panelPauseMain = root.Q("main");
        panelPauseOptions = root.Q("options");

        var resumeBtn = root.Q<Button>("resume");
        if (resumeBtn != null)
        {
            resumeBtn.clicked -= Resume;
            resumeBtn.clicked += Resume;
        }

        var restartBtn = root.Q<Button>("restart") ?? root.Q<Button>("retry");
        if (restartBtn != null)
        {
            restartBtn.clicked -= OnRestartClicked;
            restartBtn.clicked += OnRestartClicked;
        }

        var settingsBtn = root.Q<Button>("settings");
        if (settingsBtn != null)
        {
            settingsBtn.clicked -= OnSettingsClicked;
            settingsBtn.clicked += OnSettingsClicked;
        }

        var creditsBtn = root.Q<Button>("credits");
        if (creditsBtn != null)
        {
            creditsBtn.clicked -= OnCreditsClicked;
            creditsBtn.clicked += OnCreditsClicked;
        }

        var quitBtn = root.Q<Button>("quit");
        if (quitBtn != null)
        {
            quitBtn.clicked -= OnQuitClicked;
            quitBtn.clicked += OnQuitClicked;
        }

        if (panelPauseOptions != null)
            OptionSettings.Bind(panelPauseOptions, () => ShowOptions(false));
    }

    void OnRestartClicked() => Leave(LevelProgress.Retry);
    void OnSettingsClicked() => ShowOptions(true);
    void OnCreditsClicked() => Leave(() => CRTTransition.LoadScene(creditsScene));
    void OnQuitClicked() => Leave(LevelProgress.BackToSelector);

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
        if (pauseMenuUI != null) pauseMenuUI.AddToClassList("hidden");
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    void Pause()
    {
        EnsureCompositedSettings();
        if (pauseMenuUI != null) pauseMenuUI.RemoveFromClassList("hidden");
        ShowOptions(false);
        Time.timeScale = 0f;
        GameIsPaused = true;
    }

    void ShowOptions(bool show)
    {
        if (panelPauseMain != null) panelPauseMain.EnableInClassList("hidden", show);
        if (panelPauseOptions != null) panelPauseOptions.EnableInClassList("hidden", !show);
    }

    void Leave(System.Action load)
    {
        Time.timeScale = 1f;
        GameIsPaused = false;
        load();
    }
}
