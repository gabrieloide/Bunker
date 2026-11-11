using UnityEngine;

// White flag of the enemy: its radius is enemy territory (no player flag inside it) until an ally walks into it and knocks it down.
public class EnemyFlag : MonoBehaviour
{
    [Min(0.5f)] [SerializeField] float radius = 3f;
    [SerializeField] Sfx knockedDownSound = Sfx.EnemyBaseHit;

    DottedRing ring;

    public Vector2 Center => transform.position;

    public float Radius
    {
        get => radius;
        set
        {
            radius = Mathf.Max(0.5f, value);
            if (ring != null) ring.SetRadius(radius);
        }
    }

    void Start() => ring = EnemyTerritory.CreateRing(transform, radius);

    void OnEnable() => EnemyTerritory.Register(this);
    void OnDisable() => EnemyTerritory.Unregister(this);

    void Update()
    {
        if (ring != null) EnemyTerritory.UpdateRing(ring);

        float reach = radius * radius;
        foreach (var ally in AllyView.Active)
        {
            if (ally == null || ((Vector2)ally.transform.position - Center).sqrMagnitude > reach) continue;
            SoundBank.Play(knockedDownSound);
            Destroy(gameObject);
            return;
        }
    }
}
