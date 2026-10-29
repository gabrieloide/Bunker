using System;
using UnityEngine;
using UnityEngine.UIElements;

// Settings panel shared by the main menu and the pause menu (UI/Options.uxml). Brightness is drawn by BrightnessOverlay.
public static class OptionSettings
{
    const string BrightnessKey = "brightness";

    // 1 = untouched picture, lower values darken the whole screen
    public static float Brightness
    {
        get => PlayerPrefs.GetFloat(BrightnessKey, 1f);
        set => PlayerPrefs.SetFloat(BrightnessKey, Mathf.Clamp(value, 0.2f, 1f));
    }

    public static void Bind(VisualElement options, Action onBack)
    {
        var slider = options.Q<Slider>("brightness");
        slider.lowValue = 0.2f;
        slider.highValue = 1f;
        slider.SetValueWithoutNotify(Brightness);
        slider.RegisterValueChangedCallback(e => Brightness = e.newValue);
        options.Q<Button>("back").clicked += onBack;
    }
}
