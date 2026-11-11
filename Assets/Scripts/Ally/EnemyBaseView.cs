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
    [SerializeField] Vector3 smokeOffset = new Vector3(0f, 1f, 0f);
    BunkerSmoke smoke;

    DottedRing ring;

    void Start()
    {
        if (lifeBar != null) lifeBar.SetValue(StartLife, StartLife);
        smoke = new BunkerSmoke(transform, smokeOffset, smokeBelow);
        if (Radius > 0f)
        {
            EnemyTerritory.SetBase(transform.position, Radius);
            ring = EnemyTerritory.CreateRing(transform, Radius);
        }
    }

    void Update()
    {
        if (ring != null) EnemyTerritory.UpdateRing(ring);
    }

    void OnDestroy() => EnemyTerritory.ClearBase();

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
        SoundBank.Play(Sfx.EnemyBaseDestroyed);
        smoke?.SetDestroyed();
        if (destroyedParticle != null)
            OneShotEffect.EnsureFrontSorting(Instantiate(destroyedParticle, transform.position, Quaternion.identity));
    }
}
