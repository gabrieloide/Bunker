using System;
using UnityEngine;
using UnityEngine.UIElements;

// Settings panel shared by the main menu and the pause menu (UI/Options.uxml).
public static class OptionSettings
{
    public static void Bind(VisualElement options, Action onBack)
    {
        var back = options.Q<Button>("back");
        if (back != null) back.clicked += onBack;
    }
}
