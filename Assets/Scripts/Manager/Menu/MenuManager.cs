using UnityEngine;
using UnityEngine.UIElements;

// Main menu (UI/MainMenu.uxml)
[RequireComponent(typeof(UIDocument))]
public class MenuManager : MonoBehaviour
{
    [SerializeField] string SceneName = "LevelSelector";
    [SerializeField] string creditsScene = "Credits";

    VisualElement main, options;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        main = root.Q("main");
        options = root.Q("options");

        root.Q<Button>("start").clicked += PlayButton;
        root.Q<Button>("settings").clicked += () => ShowOptions(true);
        root.Q<Button>("credits").clicked += () => CRTTransition.LoadScene(creditsScene);
        root.Q<Button>("quit").clicked += Quit;
        OptionSettings.Bind(options, () => ShowOptions(false));
    }

    public void PlayButton() => CRTTransition.LoadScene(SceneName);

    void ShowOptions(bool show)
    {
        main.EnableInClassList("hidden", show);
        options.EnableInClassList("hidden", !show);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
