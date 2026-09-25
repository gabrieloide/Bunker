using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Level selector scene: one LevelButton per catalog entry, locked until the previous level is beaten.
public class LevelSelectorMenu : MonoBehaviour
{
    [SerializeField] LevelCatalog catalog;
    [Tooltip("Disabled template cloned for every level")]
    [SerializeField] LevelButton buttonTemplate;
    [SerializeField] Transform buttonContainer;
    [Tooltip("Shows the description of the level under the pointer")]
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] string backScene = "MainMenu 2";

    void Start()
    {
        Time.timeScale = 1f;
        ShowDescription("");
        if (catalog == null || buttonTemplate == null) return;

        buttonTemplate.gameObject.SetActive(false);
        Transform parent = buttonContainer != null ? buttonContainer : buttonTemplate.transform.parent;
        for (int i = 0; i < catalog.levels.Count; i++)
        {
            var level = catalog.levels[i];
            if (level == null) continue;

            var button = Instantiate(buttonTemplate, parent);
            button.name = $"Level_{i + 1:00}";
            button.gameObject.SetActive(true);
            button.Bind(i, level, LevelProgress.IsUnlocked(i), () => LevelProgress.Play(catalog, level), ShowDescription);
        }
    }

    void ShowDescription(string text)
    {
        if (descriptionText != null) descriptionText.text = text;
    }

    public void Back() => SceneManager.LoadScene(backScene);
}
