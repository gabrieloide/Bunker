using UnityEngine;

// Plays a short sprite sequence once on a SpriteRenderer and stays on the last frame (the planted flag animation).
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
        int frame = Mathf.Min((int)(elapsed * framesPerSecond), frames.Length - 1);
        body.sprite = frames[frame];
        if (frame == frames.Length - 1) enabled = false;
    }
}
