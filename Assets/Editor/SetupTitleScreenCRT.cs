using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupTitleScreenCRT
{
    [MenuItem("Bunker/Setup Title Screen CRT Turn-On")]
    public static void Execute()
    {
        string scenePath = "Assets/Scenes/TitleScreen.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            Debug.LogError("Canvas no encontrado en TitleScreen.unity");
            return;
        }

        var crtMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/CRTTurnOn.mat");
        if (crtMat == null)
        {
            Debug.LogError("No se encontro CRTTurnOn.mat en Assets/Shaders/CRTTurnOn.mat");
            return;
        }

        // Clean existing overlay if present
        var existingOverlay = GameObject.Find("Canvas/CRTTurnOnOverlay");
        if (existingOverlay != null)
        {
            Object.DestroyImmediate(existingOverlay);
        }

        // Create overlay Image covering the whole canvas
        var overlayObj = new GameObject("CRTTurnOnOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        overlayObj.transform.SetAsLastSibling();

        var rt = overlayObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        var img = overlayObj.AddComponent<Image>();
        img.color = Color.white;
        img.material = crtMat;
        img.raycastTarget = true;

        var effect = overlayObj.AddComponent<CRTTurnOnEffect>();

        // Wire to TitleScreen manager
        var managerObj = GameObject.Find("Manager");
        if (managerObj != null)
        {
            var titleScreen = managerObj.GetComponent<TitleScreen>();
            if (titleScreen != null)
            {
                var so = new SerializedObject(titleScreen);
                var prop = so.FindProperty("turnOnEffect");
                if (prop != null)
                {
                    prop.objectReferenceValue = effect;
                    so.ApplyModifiedProperties();
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Efecto de encendido de TV CRT configurado con éxito en TitleScreen.unity.");
    }
}
