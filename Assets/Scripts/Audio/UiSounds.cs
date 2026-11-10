using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Click and hover sounds for every UI Toolkit button in every screen (menus, pause, level selector, results, options),
// without touching each screen: it listens on each panel's root, which outlives the documents rebuilding their trees.
public class UiSounds : MonoBehaviour
{
    const float ScanInterval = 0.5f;

    readonly HashSet<IPanel> hooked = new HashSet<IPanel>();
    float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var host = new GameObject("[UiSounds]");
        DontDestroyOnLoad(host);
        host.AddComponent<UiSounds>();
    }

    // Documents come and go with scenes and screens, so look for new panels every now and then
    void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + ScanInterval;

        hooked.RemoveWhere(panel => panel == null || panel.visualTree == null);
        foreach (var document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
        {
            var panel = document.rootVisualElement?.panel;
            if (panel == null || !hooked.Add(panel)) continue;
            panel.visualTree.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            panel.visualTree.RegisterCallback<PointerEnterEvent>(OnPointerEnter, TrickleDown.TrickleDown);
        }
    }

    static void OnClick(ClickEvent evt)
    {
        var button = ButtonOf(evt.target);
        if (button != null && button.enabledInHierarchy)
            SoundBank.Play(Sfx.UiClick);
    }

    // Enter events reach the root once per element entered: only the button itself counts, not its label
    static void OnPointerEnter(PointerEnterEvent evt)
    {
        if (evt.target is Button button && button.enabledInHierarchy)
            SoundBank.Play(Sfx.UiHover);
    }

    static Button ButtonOf(IEventHandler target)
    {
        for (var element = target as VisualElement; element != null; element = element.parent)
            if (element is Button button) return button;
        return null;
    }
}
