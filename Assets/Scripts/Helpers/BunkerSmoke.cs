using UnityEngine;

// Smoke over a bunker (the player's or the enemy base): thin while its life is low, thick once it is destroyed.
// The emitter is Resources/BunkerSmoke (PixelSmoke with the Group 23Explotion puff frames).
public class BunkerSmoke
{
    const string PrefabPath = "BunkerSmoke";
    static PixelSmoke prefab;

    readonly Transform owner;
    readonly float threshold;
    readonly SpriteRenderer bodyRenderer;
    PixelSmoke smoke;

    public BunkerSmoke(Transform owner, float threshold = 0.3f, SpriteRenderer bodyRenderer = null)
    {
        this.owner = owner;
        this.threshold = threshold;
        this.bodyRenderer = bodyRenderer;
    }

    public static Sprite FallbackDestroyedBunkerSprite
    {
        get
        {
            if (prefab == null) prefab = Resources.Load<PixelSmoke>(PrefabPath);
            return prefab != null ? prefab.FallbackDestroyedBunkerSprite : null;
        }
    }

    // Smoke on at or below the threshold, off again if the bunker is healed above it
    public void SetLife(float life, float maxLife)
    {
        if (smoke != null && smoke.Destroyed) return;
        bool low = maxLife > 0f && life / maxLife <= threshold;
        if (low && smoke == null) Start();
        else if (!low && smoke != null) { Object.Destroy(smoke.gameObject); smoke = null; }
    }

    public void SetDestroyed()
    {
        if (smoke == null) Start();
        if (smoke != null) smoke.Destroyed = true;
    }

    void Start()
    {
        if (owner == null) return;
        if (prefab == null) prefab = Resources.Load<PixelSmoke>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"BunkerSmoke: missing Resources/{PrefabPath} prefab.");
            return;
        }
        smoke = Object.Instantiate(prefab, owner.position, Quaternion.identity, owner);
        if (bodyRenderer != null) smoke.SetBodyRenderer(bodyRenderer);
    }
}
