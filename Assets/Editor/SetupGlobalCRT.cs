using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupGlobalCRT
{
    [MenuItem("Bunker/Setup Global CRT Filter")]
    public static void Execute()
    {
        // 1. Setup in MainMenu 2.unity
        SetupScene("Assets/Scenes/MainMenu 2.unity");

        // 2. Also ensure it's in TitleScreen.unity if starting from the title
        SetupScene("Assets/Scenes/TitleScreen.unity");

        Debug.Log("GlobalRetroCRT configurado exitosamente en MainMenu 2 y TitleScreen.");
    }

    static void SetupScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Remove old camera filter pack if present on camera
        var camObj = GameObject.Find("Main Camera");
        if (camObj != null)
        {
            var oldFilter = camObj.GetComponent("CameraFilterPack_TV_ARCADE_2");
            if (oldFilter != null)
            {
                Object.DestroyImmediate(oldFilter);
            }

            var filter = camObj.GetComponent<RetroCRTFilter>();
            if (filter == null)
            {
                filter = camObj.AddComponent<RetroCRTFilter>();
            }

            // Set exact values from SceneGame
            filter.curvature = 0.05f;
            filter.bezelSmoothness = 0.015f;
            filter.scanlineIntensity = 0.32f;
            filter.scanlineCount = 270f;
            filter.phosphorGlow = 0.12f;
            filter.shadowMaskIntensity = 0.10f;
            filter.chromaticAberration = 0.0015f;
            filter.brightness = 1.05f;
            filter.contrast = 1.05f;
            filter.vignetteIntensity = 0.35f;
            filter.vignetteRoundness = 0.5f;
        }

        // Setup persistent GlobalRetroCRT GameObject
        var globalObj = GameObject.Find("GlobalRetroCRT");
        if (globalObj == null)
        {
            globalObj = new GameObject("GlobalRetroCRT");
        }

        var globalCrt = globalObj.GetComponent<GlobalRetroCRT>();
        if (globalCrt == null)
        {
            globalCrt = globalObj.AddComponent<GlobalRetroCRT>();
        }

        // Configure exact values from SceneGame
        globalCrt.curvature = 0.05f;
        globalCrt.bezelSmoothness = 0.015f;
        globalCrt.scanlineIntensity = 0.32f;
        globalCrt.scanlineCount = 270f;
        globalCrt.phosphorGlow = 0.12f;
        globalCrt.shadowMaskIntensity = 0.10f;
        globalCrt.chromaticAberration = 0.0015f;
        globalCrt.brightness = 1.05f;
        globalCrt.contrast = 1.05f;
        globalCrt.vignetteIntensity = 0.35f;
        globalCrt.vignetteRoundness = 0.5f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
