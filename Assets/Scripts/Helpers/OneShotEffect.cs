using UnityEngine;

// Sprite animation that plays its clip once and destroys itself. Duration is read from the
// controller's clip so callers can sequence on it (e.g. spawn the tower when the flip ends).
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class OneShotEffect : MonoBehaviour
{
    public float Duration { get; private set; }

    // Awake runs inside Instantiate, so Duration is valid as soon as the caller gets the instance back
    void Awake()
    {
        EnsureFrontSorting(gameObject, 100);

        var controller = GetComponent<Animator>().runtimeAnimatorController;
        Duration = controller != null && controller.animationClips.Length > 0 ? controller.animationClips[0].length : 0f;
        Destroy(gameObject, Duration);
    }

    public void SetTint(Color tint) => GetComponent<SpriteRenderer>().color = tint;

    // Draws just above the given renderer so the effect is never hidden behind what it decorates
    public void DrawAbove(Renderer target)
    {
        if (target == null) return;
        int entityLayerId = SortingLayer.NameToID("Entity");
        int targetOrder = Mathf.Max(100, target.sortingOrder + 10);
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            r.sortingLayerID = entityLayerId;
            r.sortingOrder = targetOrder;
        }
    }

    public static void EnsureFrontSorting(GameObject instance, int minOrder = 100)
    {
        if (instance == null) return;
        var pos = instance.transform.position;
        instance.transform.position = new Vector3(pos.x, pos.y, 0f);

        int entityLayerId = SortingLayer.NameToID("Entity");
        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
        {
            r.sortingLayerID = entityLayerId;
            r.sortingOrder = Mathf.Max(r.sortingOrder, minOrder);
        }
    }
}
