using Unity.Entities;
using UnityEngine;

// Presentation of the enemy base the allies attack. Its life lives in the simulation; destroying it wins.
public class EnemyBaseView : MonoBehaviour
{
    [Tooltip("Used when no LevelDefinition sets the base life")]
    [Min(1f)] [SerializeField] float defaultLife = 500f;
    [SerializeField] WorldLifeBar lifeBar;
    [SerializeField] GameObject hitParticle;
    [SerializeField] GameObject destroyedParticle;

    [Tooltip("Used when no LevelDefinition sets the base territory")]
    [Min(0f)] [SerializeField] float defaultRadius = 7f;

    public Entity Entity { get; set; }
    public float StartLife => LevelSetup.Current != null ? LevelSetup.Current.enemyBaseLife : defaultLife;
    float Radius => LevelSetup.Current != null ? LevelSetup.Current.enemyBaseRadius : defaultRadius;

    [Tooltip("Smoke shows at or below this fraction of life, and once destroyed")]
    [Range(0f, 1f)] [SerializeField] float smokeBelow = 0.3f;

    [Header("Destroyed Visual")]
    [SerializeField] SpriteRenderer bodyRenderer;
    [SerializeField] Sprite destroyedSprite;

    BunkerSmoke smoke;
    DottedRing ring;
    bool destroyed;
    bool destroyedVisualApplied;

    public bool IsDestroyed => destroyed;
    public SpriteRenderer BodyRenderer => ResolveBodyRenderer();

    void Start()
    {
        var body = ResolveBodyRenderer();
        if (lifeBar != null) lifeBar.SetValue(StartLife, StartLife);
        smoke = new BunkerSmoke(transform, smokeBelow, body);
        if (Radius > 0f)
        {
            EnemyTerritory.RegisterBase(this, transform.position, Radius);
            ring = EnemyTerritory.CreateRing(transform, Radius);
        }
    }

    void Update()
    {
        if (ring != null)
        {
            EnemyTerritory.UpdateRing(ring);
            ring.transform.Rotate(0f, 0f, -10f * Time.deltaTime);
        }
    }

    void OnDestroy() => EnemyTerritory.UnregisterBase(this);

    public void OnHit(float remainingLife)
    {
        if (lifeBar != null) lifeBar.SetValue(remainingLife, StartLife);
        smoke?.SetLife(remainingLife, StartLife);
        SoundBank.Play(Sfx.EnemyBaseHit);
        if (hitParticle != null)
            OneShotEffect.EnsureFrontSorting(Instantiate(hitParticle, transform.position, Quaternion.identity));
    }

    public void OnDestroyed()
    {
        if (destroyed) return;
        destroyed = true;
        SoundBank.Play(Sfx.EnemyBaseDestroyed);
        smoke?.SetDestroyed();
        if (destroyedParticle != null)
            OneShotEffect.EnsureFrontSorting(Instantiate(destroyedParticle, transform.position, Quaternion.identity));

        var sequence = FindAnyObjectByType<MatchEndSequence>();
        if (sequence == null || !sequence.gameObject.activeInHierarchy)
            ApplyDestroyedVisual();
    }

    /// <summary>Replaces the intact enemy base sprite with ruined art, keeping its world width.</summary>
    public bool ApplyDestroyedVisual()
    {
        if (destroyedVisualApplied) return true;
        var renderer = ResolveBodyRenderer();
        var ruins = destroyedSprite != null ? destroyedSprite : BunkerSmoke.FallbackDestroyedBunkerSprite;
        if (renderer == null || ruins == null) return false;

        float oldWidth = renderer.bounds.size.x;
        renderer.sprite = ruins;
        renderer.color = Color.white;

        float lossyX = Mathf.Abs(renderer.transform.lossyScale.x);
        float newWidth = ruins.bounds.size.x * (lossyX > 0.0001f ? lossyX : 1f);
        if (oldWidth > 0f && newWidth > 0f)
            renderer.transform.localScale *= oldWidth / newWidth;

        destroyedVisualApplied = true;
        return true;
    }

    SpriteRenderer ResolveBodyRenderer()
    {
        if (bodyRenderer != null && bodyRenderer.sprite != null)
            return bodyRenderer;

        SpriteRenderer best = PickLargestSprite(GetComponentsInChildren<SpriteRenderer>());
        if (best != null)
        {
            bodyRenderer = best;
            return bodyRenderer;
        }

        float bestDistSq = 4f;
        foreach (var candidate in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (candidate == null || candidate.sprite == null || !candidate.enabled) continue;
            if (IsAuxRenderer(candidate)) continue;
            float distSq = ((Vector2)candidate.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distSq > bestDistSq) continue;
            if (candidate.gameObject.name.Contains("EnemyBase") || distSq < bestDistSq)
            {
                best = candidate;
                bestDistSq = candidate.gameObject.name.Contains("EnemyBase") ? 0f : distSq;
            }
        }

        if (best != null) bodyRenderer = best;
        return bodyRenderer;
    }

    static SpriteRenderer PickLargestSprite(SpriteRenderer[] candidates)
    {
        SpriteRenderer best = null;
        float bestArea = 0f;
        foreach (var r in candidates)
        {
            if (r == null || r.sprite == null || !r.enabled || IsAuxRenderer(r)) continue;
            float area = r.bounds.size.x * r.bounds.size.y;
            if (area > bestArea) { bestArea = area; best = r; }
        }
        return best;
    }

    static bool IsAuxRenderer(SpriteRenderer r) =>
        r.GetComponentInParent<PixelSmoke>() != null
        || r.GetComponentInParent<WorldLifeBar>() != null
        || r.GetComponentInParent<Enemy>() != null
        || r.GetComponentInParent<AllyView>() != null
        || r.GetComponentInParent<TurretStats>() != null;
}
