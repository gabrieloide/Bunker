using System;
using UnityEngine;
using UnityEngine.UIElements;

// Settings panel shared by the main menu and the pause menu (UI/Options.uxml).
public static class OptionSettings
{
    const string VolumeKey = "Bunker_Volume";
    const string CrtKey = "Bunker_CRT";

    public static void Bind(VisualElement options, Action onBack)
    {
        if (options == null) return;

        float savedVol = PlayerPrefs.GetFloat(VolumeKey, 1f);
        AudioListener.volume = savedVol;

        var volume = options.Q<Slider>("volume");
        if (volume != null)
        {
            volume.SetValueWithoutNotify(savedVol);
            volume.RegisterValueChangedCallback(evt =>
            {
                AudioListener.volume = evt.newValue;
                PlayerPrefs.SetFloat(VolumeKey, evt.newValue);
                PlayerPrefs.Save();
            });
        }

        bool crtOn = PlayerPrefs.GetInt(CrtKey, 1) == 1;
        if (GlobalRetroCRT.Instance != null)
            GlobalRetroCRT.Instance.SetCrtEnabled(crtOn);

        var crtBtn = options.Q<Button>("crt");
        if (crtBtn != null)
        {
            crtBtn.text = crtOn ? "ON" : "OFF";
            crtBtn.clicked += () =>
            {
                crtOn = !crtOn;
                crtBtn.text = crtOn ? "ON" : "OFF";
                PlayerPrefs.SetInt(CrtKey, crtOn ? 1 : 0);
                PlayerPrefs.Save();
                if (GlobalRetroCRT.Instance != null)
                    GlobalRetroCRT.Instance.SetCrtEnabled(crtOn);
            };
        }

        var fsBtn = options.Q<Button>("fullscreen");
        if (fsBtn != null)
        {
            fsBtn.text = Screen.fullScreen ? "ON" : "OFF";
            fsBtn.clicked += () =>
            {
                bool next = !Screen.fullScreen;
                Screen.fullScreen = next;
                fsBtn.text = next ? "ON" : "OFF";
            };
        }

        var back = options.Q<Button>("back");
        if (back != null) back.clicked += onBack;
    }
}
