using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Level selector scene: manages 5 world level nodes (interactive turrets),
// handling hover zoom, destroyed sprite states, and loading levels. Text and Back live in UI/LevelSelector.uxml.
[RequireComponent(typeof(UIDocument))]
public class LevelSelectorMenu : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] LevelCatalog catalog;

    [Header("World Level Nodes")]
    [Tooltip("The 5 world spaces for the levels")]
    [SerializeField] List<WorldLevelNode> worldNodes = new List<WorldLevelNode>();
    [SerializeField] Sprite[] customTurretSprites;
    [SerializeField] Sprite destroyedSprite;

    [SerializeField] string backScene = "MainMenu 2";

    Label descriptionText;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.pickingMode = PickingMode.Ignore; // the level nodes are world objects behind the panel
        descriptionText = root.Q<Label>("description");
        root.Q<Button>("back").clicked += Back;
    }

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
