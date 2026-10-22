using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public static class SetupWorldLevelSelector
{
    [MenuItem("Bunker/Setup World Level Selector")]
    public static void Execute()
    {
        string scenePath = "Assets/Scenes/LevelSelector.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/ScriptableObjects/Levels/LevelCatalog.asset");
        if (catalog == null)
        {
            Debug.LogError("No se encontro LevelCatalog en Assets/ScriptableObjects/Levels/LevelCatalog.asset");
            return;
        }

        // Load Sprites
        var sB1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/32x32/b1island.png");
        var sB2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/32x32/b2island.png");
        var sB3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/32x32/b3island.png");
        var sB4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/32x32/b4island.png");
        var sB5 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/32x32/b5island.png");
        var sDestroyed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/32x32/destroyed.png");
        var sName = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/32x32/name.png");
        var sStairsLeft = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/32x32/stairsleft.png");
        var sStairsRight = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/32x32/stairsright.png");

        var fontPrompt = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/WINDOWS_COMMAND_PROMPT SDF.asset");
        var spaceMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/PixelArtSpace.mat");

        Sprite[] turretSprites = { sB1, sB2, sB3, sB4, sB5 };

        // 1. Setup Camera with Physics2DRaycaster
        var camObj = GameObject.Find("Main Camera");
        if (camObj != null)
        {
            var cam = camObj.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.015f, 0.015f, 0.04f, 1f);

            if (camObj.GetComponent<Physics2DRaycaster>() == null)
            {
                camObj.AddComponent<Physics2DRaycaster>();
            }
        }

        // 2. Space Background Quad
        var existingBg = GameObject.Find("SpaceBackground");
        if (existingBg != null) Object.DestroyImmediate(existingBg);

        if (spaceMat != null)
        {
            var spaceQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            spaceQuad.name = "SpaceBackground";
            spaceQuad.transform.position = new Vector3(0f, 0f, 5f);
            spaceQuad.transform.localScale = new Vector3(24f, 14f, 1f);
            var mr = spaceQuad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = spaceMat;
            var col = spaceQuad.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        // 3. Clean and recreate WorldLevelNodes root
        var existingNodes = GameObject.Find("WorldLevelNodes");
        if (existingNodes != null) Object.DestroyImmediate(existingNodes);

        var nodesRoot = new GameObject("WorldLevelNodes");
        nodesRoot.transform.position = Vector3.zero;

        // Island positions in world (zigzag 1 -> 5)
        Vector3[] nodePositions = {
            new Vector3(-5.6f, -1.5f, 0f),
            new Vector3(-2.8f,  1.1f, 0f),
            new Vector3( 0.0f, -1.5f, 0f),
            new Vector3( 2.8f,  1.1f, 0f),
            new Vector3( 5.6f, -1.5f, 0f)
        };

        // Connecting stairs between islands
        CreateStairs(nodesRoot.transform, "Stairs_1_2", new Vector3(-4.2f, -0.2f, 0f), sStairsRight, new Vector3(1.5f, 1.5f, 1f));
        CreateStairs(nodesRoot.transform, "Stairs_2_3", new Vector3(-1.4f, -0.2f, 0f), sStairsLeft,  new Vector3(1.5f, 1.5f, 1f));
        CreateStairs(nodesRoot.transform, "Stairs_3_4", new Vector3( 1.4f, -0.2f, 0f), sStairsRight, new Vector3(1.5f, 1.5f, 1f));
        CreateStairs(nodesRoot.transform, "Stairs_4_5", new Vector3( 4.2f, -0.2f, 0f), sStairsLeft,  new Vector3(1.5f, 1.5f, 1f));

        List<WorldLevelNode> worldNodesList = new List<WorldLevelNode>();

        for (int i = 0; i < 5; i++)
        {
            var nodeObj = new GameObject($"LevelNode_{i + 1}");
            nodeObj.transform.SetParent(nodesRoot.transform);
            nodeObj.transform.position = nodePositions[i];

            // Collider for interaction (hover & click)
            var boxCol = nodeObj.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(2.2f, 2.2f);

            // Turret Visual
            var visualObj = new GameObject("TurretVisual");
            visualObj.transform.SetParent(nodeObj.transform);
            visualObj.transform.localPosition = Vector3.zero;
            visualObj.transform.localScale = new Vector3(1.7f, 1.7f, 1f);

            var turretSr = visualObj.AddComponent<SpriteRenderer>();
            turretSr.sprite = turretSprites[i];
            turretSr.sortingOrder = 10;

            // Name Box (Banner Root)
            var nameBoxObj = new GameObject("NameBox");
            nameBoxObj.transform.SetParent(nodeObj.transform);
            nameBoxObj.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            nameBoxObj.transform.localScale = Vector3.one;

            // Frame child
            var frameObj = new GameObject("Frame");
            frameObj.transform.SetParent(nameBoxObj.transform);
            frameObj.transform.localPosition = Vector3.zero;
            frameObj.transform.localScale = new Vector3(3.2f, 1.0f, 1f);

            var nameSr = frameObj.AddComponent<SpriteRenderer>();
            nameSr.sprite = sName;
            nameSr.sortingOrder = 15;

            // Name Text (TextMeshPro inside NameBox, unscaled for sharp font)
            var textObj = new GameObject("NameText");
            textObj.transform.SetParent(nameBoxObj.transform);
            textObj.transform.localPosition = new Vector3(0f, 0.05f, -0.05f);
            textObj.transform.localScale = Vector3.one;

            var tmp = textObj.AddComponent<TextMeshPro>();
            if (fontPrompt != null) tmp.font = fontPrompt;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 1.8f;
            tmp.fontSizeMax = 2.8f;
            tmp.fontSize = 2.5f;
            tmp.color = Color.white;
            tmp.sortingOrder = 20;

            string lvlTitle = (catalog.levels != null && i < catalog.levels.Count && catalog.levels[i] != null)
                ? catalog.levels[i].displayName
                : $"NIVEL {i + 1}";
            tmp.text = lvlTitle.ToUpper();

            var rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(3.0f, 0.8f);

            // Node Component
            var nodeComp = nodeObj.AddComponent<WorldLevelNode>();

            var so = new SerializedObject(nodeComp);
            so.FindProperty("levelIndex").intValue = i;
            if (i < catalog.levels.Count)
                so.FindProperty("levelDefinition").objectReferenceValue = catalog.levels[i];
            so.FindProperty("turretTransform").objectReferenceValue = visualObj.transform;
            so.FindProperty("turretRenderer").objectReferenceValue = turretSr;
            so.FindProperty("normalSprite").objectReferenceValue = turretSprites[i];
            so.FindProperty("destroyedSprite").objectReferenceValue = sDestroyed;
            so.FindProperty("nameBox").objectReferenceValue = nameBoxObj;
            so.FindProperty("nameText").objectReferenceValue = tmp;
            so.FindProperty("nameBoxRenderer").objectReferenceValue = nameSr;
            so.FindProperty("hoverScaleMultiplier").floatValue = 1.25f;
            so.ApplyModifiedProperties();

            worldNodesList.Add(nodeComp);
        }

        // 4. Update LevelSelectorMenu on Canvas
        var menu = Object.FindAnyObjectByType<LevelSelectorMenu>();
        if (menu != null)
        {
            var menuSo = new SerializedObject(menu);
            menuSo.FindProperty("catalog").objectReferenceValue = catalog;
            menuSo.FindProperty("destroyedSprite").objectReferenceValue = sDestroyed;

            var spriteProp = menuSo.FindProperty("customTurretSprites");
            spriteProp.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                spriteProp.GetArrayElementAtIndex(i).objectReferenceValue = turretSprites[i];
            }

            var nodesProp = menuSo.FindProperty("worldNodes");
            nodesProp.arraySize = worldNodesList.Count;
            for (int i = 0; i < worldNodesList.Count; i++)
            {
                nodesProp.GetArrayElementAtIndex(i).objectReferenceValue = worldNodesList[i];
            }

            menuSo.ApplyModifiedProperties();
        }

        // 5. Adjust Canvas Elements (Title, Back button, Description)
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj != null)
        {
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camObj.GetComponent<Camera>();
            canvas.planeDistance = 1f;

            // Hide plain black canvas background completely
            var canvasBg = GameObject.Find("Canvas/Background");
            if (canvasBg != null) canvasBg.SetActive(false);

            // Hide old button grid
            var oldLevels = GameObject.Find("Canvas/Levels");
            if (oldLevels != null) oldLevels.SetActive(false);

            // Title at top
            var titleObj = GameObject.Find("Canvas/Title");
            if (titleObj != null)
            {
                var rt = titleObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -25f);
                rt.sizeDelta = new Vector2(800f, 60f);

                var titleText = titleObj.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    titleText.text = "SELECCIONA UN SECTOR";
                    titleText.fontSize = 32f;
                }
            }

            // Description at bottom
            var descObj = GameObject.Find("Canvas/Description");
            if (descObj != null)
            {
                var rt = descObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 35f);
                rt.sizeDelta = new Vector2(700f, 40f);

                var descText = descObj.GetComponent<TextMeshProUGUI>();
                if (descText != null)
                {
                    descText.fontSize = 20f;
                    descText.color = new Color(0.85f, 0.9f, 1f, 0.9f);
                }
            }

            // Back button at bottom left
            var backObj = GameObject.Find("Canvas/Back");
            if (backObj != null)
            {
                var rt = backObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(25f, 25f);
                rt.sizeDelta = new Vector2(140f, 40f);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("World Level Selector actualizado con éxito.");
    }

    static void CreateStairs(Transform parent, string name, Vector3 pos, Sprite sprite, Vector3 scale)
    {
        if (sprite == null) return;
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 3;
    }
}
