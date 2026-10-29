using UnityEngine;

// Visual feedback shown when a card is dragged over an invalid position or cannot be played
public class PlacementCrossIndicator : MonoBehaviour
{
    static PlacementCrossIndicator instance;

    [SerializeField] Sprite crossSprite;
    SpriteRenderer spriteRenderer;

    public static void Show(Vector3 position)
    {
        EnsureInstance();
        if (instance == null) return;
        instance.Place(position);
    }

    public static void Hide()
    {
        if (instance != null)
            instance.SetVisible(false);
    }

    static void EnsureInstance()
    {
        if (instance != null) return;
        instance = Object.FindAnyObjectByType<PlacementCrossIndicator>(FindObjectsInactive.Include);
        if (instance != null) return;

        GameObject prefab = Resources.Load<GameObject>("PlacementCrossIndicator");
        if (prefab != null)
        {
            GameObject go = Instantiate(prefab);
            instance = go.GetComponent<PlacementCrossIndicator>();
            DontDestroyOnLoad(go);
            return;
        }

        GameObject dynGo = new GameObject("PlacementCrossIndicator");
        instance = dynGo.AddComponent<PlacementCrossIndicator>();
        DontDestroyOnLoad(dynGo);
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitRenderer();
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void InitRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (crossSprite == null)
        {
            // Fallback: try loading from Resources
            crossSprite = Resources.Load<Sprite>("cross");
            if (crossSprite == null)
            {
                var sprites = Resources.LoadAll<Sprite>("cross");
                if (sprites != null && sprites.Length > 0)
                    crossSprite = sprites[0];
            }
        }

        if (spriteRenderer.sprite == null && crossSprite != null)
            spriteRenderer.sprite = crossSprite;

        spriteRenderer.sortingLayerName = "InHand";
        spriteRenderer.sortingOrder = 25;
        transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
    }

    public void Place(Vector3 position)
    {
        InitRenderer();
        position.z = 0f;
        transform.position = position;
        SetVisible(true);
    }

    public void SetVisible(bool visible)
    {
        if (visible && !gameObject.activeSelf)
            gameObject.SetActive(true);
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;
        else if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
    }
}
