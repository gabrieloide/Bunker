using TMPro;
using UnityEngine;

// End-of-match panel: victory when the enemy base falls, defeat when the bunker does. Freezes the game.
public class MatchResultScreen : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text titleText;
    [Tooltip("Only offered after a victory when there is a next level")]
    [SerializeField] GameObject nextLevelButton;
    [SerializeField] string victoryTitle = "VICTORIA";
    [SerializeField] string defeatTitle = "DERROTA";

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnEnable()
    {
        SimEventDispatcher.OnVictory += ShowVictory;
        SimEventDispatcher.OnGameOver += ShowDefeat;
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
        if (panel == null || panel.activeSelf) return;
        if (titleText != null) titleText.text = title;
        if (nextLevelButton != null) nextLevelButton.SetActive(offerNext);
        panel.SetActive(true);

        // The pause menu would unfreeze the game underneath
        var pause = FindAnyObjectByType<PauseMenu>();
        if (pause != null) pause.enabled = false;
        Time.timeScale = 0f;
    }

    public void NextLevel() => LevelProgress.Play(LevelProgress.Catalog, LevelProgress.Next);
    public void Retry() => LevelProgress.Retry();
    public void Levels() => LevelProgress.BackToSelector();
}
