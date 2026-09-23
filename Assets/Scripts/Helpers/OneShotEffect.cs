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
        var controller = GetComponent<Animator>().runtimeAnimatorController;
        Duration = controller != null && controller.animationClips.Length > 0 ? controller.animationClips[0].length : 0f;
        Destroy(gameObject, Duration);
    }

    public void SetTint(Color tint) => GetComponent<SpriteRenderer>().color = tint;

    // Draws just above the given renderer so the effect is never hidden behind what it decorates
    public void DrawAbove(Renderer target)
    {
        if (target == null) return;
        var sr = GetComponent<SpriteRenderer>();
        sr.sortingLayerID = target.sortingLayerID;
        sr.sortingOrder = target.sortingOrder + 1;
    }
}
