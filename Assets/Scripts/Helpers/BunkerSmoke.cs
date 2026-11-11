using UnityEngine;

// Looping smoke over a bunker (the player's or the enemy base) while its life is low or once it is destroyed.
// The particle is Resources/BunkerSmoke, a copy of ExplotionParticle tuned to loop slowly upwards.
public class BunkerSmoke
{
    const string PrefabPath = "BunkerSmoke";
    static GameObject prefab;

    readonly Transform owner;
    readonly Vector3 offset;
    readonly float threshold;
    GameObject smoke;

    public BunkerSmoke(Transform owner, Vector3 offset, float threshold = 0.3f)
    {
        this.owner = owner;
        this.offset = offset;
        this.threshold = threshold;
    }

    // Smoke on at or below the threshold, off again if the bunker is healed above it
    public void SetLife(float life, float maxLife)
    {
        bool low = maxLife > 0f && life / maxLife <= threshold;
        if (low && smoke == null) Start();
        else if (!low && smoke != null) { Object.Destroy(smoke); smoke = null; }
    }

    public void SetDestroyed()
    {
        if (smoke == null) Start();
    }

    void Start()
    {
        if (owner == null) return;
        if (prefab == null) prefab = Resources.Load<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"BunkerSmoke: missing Resources/{PrefabPath} prefab.");
            return;
        }
        smoke = Object.Instantiate(prefab, owner.position + offset, Quaternion.identity, owner);
        OneShotEffect.EnsureFrontSorting(smoke);
    }
}
