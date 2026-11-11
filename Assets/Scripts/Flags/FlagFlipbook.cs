using UnityEngine;

// Loops a short sprite sequence on a SpriteRenderer (the waving flag, like FlagStand.anim on the player's flag).
[RequireComponent(typeof(SpriteRenderer))]
public class FlagFlipbook : MonoBehaviour
{
    [SerializeField] Sprite[] frames;
    [Min(1f)] [SerializeField] float framesPerSecond = 8f;

    SpriteRenderer body;
    float elapsed;

    void Awake()
    {
        body = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0) body.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length < 2) { enabled = false; return; }
        elapsed += Time.deltaTime;
        body.sprite = frames[(int)(elapsed * framesPerSecond) % frames.Length];
    }
}
