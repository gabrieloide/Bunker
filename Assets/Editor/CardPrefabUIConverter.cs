using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// One-shot migration: converts world-space hand card prefabs (SpriteRenderer + physics) into Canvas UI prefabs.
public static class CardPrefabUIConverter
{
    const string CardsFolder = "Assets/Prefabs/Card/CardsInHand";
    static readonly Vector2 CardSize = new Vector2(36f, 52f);

    [MenuItem("Tools/Bunker/Convert Card Prefabs To UI")]
    public static void ConvertAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { CardsFolder });
        int converted = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (Convert(root))
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
                    if (saved)
                    {
                        converted++;
                        Debug.Log($"[CardPrefabUIConverter] Converted {path}");
                    }
                    else
                    {
                        Debug.LogWarning($"[CardPrefabUIConverter] Could not save {path} (missing script?)");
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[CardPrefabUIConverter] Done: {converted}/{guids.Length} prefabs converted.");
    }

    static bool Convert(GameObject root)
    {
        if (root.GetComponent<RectTransform>() != null && root.GetComponent<SpriteRenderer>() == null)
            return false;

        Sprite sprite = null;
        var sr = root.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sprite = sr.sprite;
            Object.DestroyImmediate(sr, true);
        }

        var col = root.GetComponent<Collider2D>();
        if (col != null) Object.DestroyImmediate(col, true);
        var rb = root.GetComponent<Rigidbody2D>();
        if (rb != null) Object.DestroyImmediate(rb, true);

        var rt = root.GetComponent<RectTransform>();
        if (rt == null) rt = root.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = CardSize;
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;

        if (root.GetComponent<CanvasRenderer>() == null) root.AddComponent<CanvasRenderer>();

        var image = root.GetComponent<Image>();
        if (image == null) image = root.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;

        if (root.GetComponent<CanvasGroup>() == null) root.AddComponent<CanvasGroup>();

        return true;
    }
}
