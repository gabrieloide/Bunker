using UnityEngine;
using UnityEngine.UIElements;

// End-of-match panel (UI/MatchResult.uxml): victory when the enemy base falls, defeat when the bunker does. Freezes the game.
[RequireComponent(typeof(UIDocument))]
public class MatchResultScreen : MonoBehaviour
{
    [SerializeField] string victoryTitle = "YOU WON";
    [SerializeField] string defeatTitle = "YOU LOSE";

    VisualElement panel;
    Label titleText;
    Button nextLevelButton; // only offered after a victory when there is a next level

    void Awake()
    {
        EnsureCompositedSettings();
    }

    void OnEnable()
    {
        EnsureCompositedSettings();
        BindUI();

        SimEventDispatcher.OnVictory += ShowVictory;
        SimEventDispatcher.OnGameOver += ShowDefeat;
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
        panel = root.Q("result");
        titleText = root.Q<Label>("title");
        nextLevelButton = root.Q<Button>("next");
        if (nextLevelButton != null)
        {
            nextLevelButton.clicked -= NextLevel;
            nextLevelButton.clicked += NextLevel;
        }

        var retryBtn = root.Q<Button>("retry");
        if (retryBtn != null)
        {
            retryBtn.clicked -= Retry;
            retryBtn.clicked += Retry;
        }

        var levelsBtn = root.Q<Button>("levels");
        if (levelsBtn != null)
        {
            levelsBtn.clicked -= Levels;
            levelsBtn.clicked += Levels;
        }
    }

    void OnDisable()
    {
        SimEventDispatcher.OnVictory -= ShowVictory;
        SimEventDispatcher.OnGameOver -= ShowDefeat;
    }

    void ShowVictory()
    {
        LevelProgress.MarkSelectedCompleted();
        Show(victoryTitle, LevelProgress.Next != null);
    }

    void ShowDefeat() => Show(defeatTitle, false);

    void Show(string title, bool offerNext)
    {
        EnsureCompositedSettings();
        if (panel == null || !panel.ClassListContains("hidden")) return;
        if (titleText != null) titleText.text = title;
        if (nextLevelButton != null) nextLevelButton.EnableInClassList("hidden", !offerNext);
        panel.RemoveFromClassList("hidden");

        // The pause menu would unfreeze the game underneath
        var pause = FindAnyObjectByType<PauseMenu>();
        if (pause != null) pause.enabled = false;
        Time.timeScale = 0f;
    }

    public void NextLevel() => LevelProgress.Play(LevelProgress.Catalog, LevelProgress.Next);
    public void Retry() => LevelProgress.Retry();
    public void Levels() => LevelProgress.BackToSelector();
}
