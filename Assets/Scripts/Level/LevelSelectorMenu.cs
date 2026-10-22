using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Level selector scene: manages 5 world level nodes (interactive turrets),
// handling hover zoom, destroyed sprite states, and loading levels.
public class LevelSelectorMenu : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] LevelCatalog catalog;

    [Header("World Level Nodes")]
    [Tooltip("The 5 world spaces for the levels")]
    [SerializeField] List<WorldLevelNode> worldNodes = new List<WorldLevelNode>();
    [SerializeField] Sprite[] customTurretSprites;
    [SerializeField] Sprite destroyedSprite;

    [Header("UI Feedback")]
    [Tooltip("Shows the description of the level under the pointer")]
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] string backScene = "MainMenu 2";

    [Header("Legacy Fallback (Optional)")]
    [SerializeField] LevelButton buttonTemplate;
    [SerializeField] Transform buttonContainer;

    void Start()
    {
        Time.timeScale = 1f;
        ShowDescription("");

        // If worldNodes is empty, find them automatically in the scene
        if (worldNodes == null || worldNodes.Count == 0)
        {
            worldNodes = new List<WorldLevelNode>(FindObjectsByType<WorldLevelNode>(FindObjectsSortMode.InstanceID));
            worldNodes.Sort((a, b) => a.levelIndex.CompareTo(b.levelIndex));
        }

        // Setup World Nodes if present
        if (worldNodes != null && worldNodes.Count > 0)
        {
            // Disable old button UI container if present
            if (buttonContainer != null) buttonContainer.gameObject.SetActive(false);
            if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);

            for (int i = 0; i < worldNodes.Count; i++)
            {
                var node = worldNodes[i];
                if (node == null) continue;

                LevelDefinition level = (catalog != null && i < catalog.levels.Count) ? catalog.levels[i] : node.levelDefinition;
                Sprite turretSprite = (customTurretSprites != null && i < customTurretSprites.Length) ? customTurretSprites[i] : null;

                node.Setup(
                    i,
                    level,
                    turretSprite,
                    destroyedSprite,
                    OnNodeSelected,
                    ShowDescription
                );
            }
        }
        else if (catalog != null && buttonTemplate != null)
        {
            // Legacy button fallback
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
    }

    void OnNodeSelected(WorldLevelNode node)
    {
        if (node == null || node.levelDefinition == null || catalog == null) return;
        LevelProgress.Play(catalog, node.levelDefinition);
    }

    public void ShowDescription(string text)
    {
        if (descriptionText != null)
            descriptionText.text = text;
    }

    public void Back() => CRTTransition.LoadScene(backScene);
}
