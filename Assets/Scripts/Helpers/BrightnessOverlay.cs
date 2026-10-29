using UnityEngine;

// Darkens the whole screen (game and UI Toolkit panels) by the brightness option. Lives across scenes, no setup.
public class BrightnessOverlay : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        var go = new GameObject("[BrightnessOverlay]");
        DontDestroyOnLoad(go);
        go.AddComponent<BrightnessOverlay>();
    }

    void OnGUI()
    {
        float alpha = 1f - OptionSettings.Brightness;
        if (alpha <= 0.001f || Event.current.type != EventType.Repaint) return;

        GUI.depth = 10;
        var previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
