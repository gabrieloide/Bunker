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

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        // Nested under the HUD document: fill the screen without catching clicks meant for the game
        root.StretchToParentSize();
        root.pickingMode = PickingMode.Ignore;
        panel = root.Q("result");
        titleText = root.Q<Label>("title");
        nextLevelButton = root.Q<Button>("next");
        nextLevelButton.clicked += NextLevel;
        root.Q<Button>("retry").clicked += Retry;
        root.Q<Button>("levels").clicked += Levels;

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
        if (!panel.ClassListContains("hidden")) return;
        titleText.text = title;
        nextLevelButton.EnableInClassList("hidden", !offerNext);
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
