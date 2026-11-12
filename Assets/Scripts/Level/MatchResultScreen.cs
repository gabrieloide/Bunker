using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// End-of-match panel (UI/MatchResult.uxml): victory when the enemy base falls, defeat when the bunker does. Freezes the game.
// A MatchEndSequence on the same object plays first; then the title types itself out and the buttons follow.
[RequireComponent(typeof(UIDocument))]
public class MatchResultScreen : MonoBehaviour
{
    [SerializeField] string victoryTitle = "YOU WON";
    [SerializeField] string defeatTitle = "YOU LOSE";
    [Tooltip("Real seconds per title letter")]
    [SerializeField] float letterTime = 0.07f;
    [Tooltip("Real seconds between the full title and the buttons, and before the buttons take clicks")]
    [SerializeField] float buttonsDelay = 0.35f;

    VisualElement panel;
    Label titleText;
    Button nextLevelButton; // only offered after a victory when there is a next level
    VisualElement[] buttons;
    MatchEndSequence sequence;

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
        buttons = new VisualElement[] { nextLevelButton, retryBtn, levelsBtn };
    }

    void OnDisable()
    {
        SimEventDispatcher.OnVictory -= ShowVictory;
        SimEventDispatcher.OnGameOver -= ShowDefeat;
    }

    void ShowVictory()
    {
        LevelProgress.MarkSelectedCompleted();
        End(true, victoryTitle, LevelProgress.Next != null);
    }

    void ShowDefeat() => End(false, defeatTitle, false);

    void End(bool victory, string title, bool offerNext)
    {
        if (sequence == null) sequence = GetComponent<MatchEndSequence>();
        if (sequence != null) sequence.Play(victory, () => Show(title, offerNext));
        else
        {
            if (!victory) SoundBank.Play(Sfx.Defeat);
            Show(title, offerNext);
        }
    }

    void Show(string title, bool offerNext)
    {
        EnsureCompositedSettings();
        if (panel == null || !panel.ClassListContains("hidden")) return;

        // The pause menu would unfreeze the game underneath
        var pause = FindAnyObjectByType<PauseMenu>();
        if (pause != null) pause.enabled = false;
        Time.timeScale = 0f;
        StartCoroutine(Reveal(title, offerNext));
    }

    // The title types itself out, then the buttons appear and only take clicks a moment later
    IEnumerator Reveal(string title, bool offerNext)
    {
        foreach (var button in buttons)
            if (button != null) { button.AddToClassList("hidden"); button.SetEnabled(false); }
        if (titleText != null) titleText.text = "";
        panel.RemoveFromClassList("hidden");

        if (titleText != null)
        {
            for (int i = 1; i <= title.Length; i++)
            {
                titleText.text = title.Substring(0, i);
                if (title[i - 1] != ' ') SoundBank.Play(Sfx.UiClick);
                yield return new WaitForSecondsRealtime(letterTime);
            }
        }
        yield return new WaitForSecondsRealtime(buttonsDelay);

        foreach (var button in buttons)
            if (button != null) button.RemoveFromClassList("hidden");
        if (nextLevelButton != null) nextLevelButton.EnableInClassList("hidden", !offerNext);
        yield return new WaitForSecondsRealtime(buttonsDelay);
        foreach (var button in buttons)
            if (button != null) button.SetEnabled(true);
    }

    public void NextLevel() => LevelProgress.Play(LevelProgress.Catalog, LevelProgress.Next);
    public void Retry() => LevelProgress.Retry();
    public void Levels() => LevelProgress.BackToSelector();
}
