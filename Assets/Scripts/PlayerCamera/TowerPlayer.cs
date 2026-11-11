using UnityEngine;

public class TowerPlayer : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] Sfx HitSound = Sfx.BunkerHit;
    [Space]
    public static TowerPlayer instance;
    [Range(0, 100)] public float life;
    public float maxLife = 100f;
    [SerializeField] GameObject hitParticle;
    public float DealTime;
    [SerializeField] LayerMask EnemyLayer;

    public Unity.Entities.Entity Entity { get; set; }

    [Tooltip("Smoke shows at or below this fraction of life, and once destroyed")]
    [Range(0f, 1f)] [SerializeField] float smokeBelow = 0.3f;
    [SerializeField] Vector3 smokeOffset = new Vector3(0f, 1f, 0f);
    BunkerSmoke smoke;

    private void Awake()
    {
        if (!instance)
        {
            instance = this;
            if (maxLife <= 0f) maxLife = 100f;
            life = maxLife;
            smoke = new BunkerSmoke(transform, smokeOffset, smokeBelow);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void TakeHit(float enemyDamage, float remainingLife = -1f)
    {
        if (remainingLife >= 0f)
            life = remainingLife;
        else
            life = Mathf.Max(0f, life - enemyDamage);
        smoke?.SetLife(life, maxLife);

        if (enemyDamage > 0f)
        {
            SoundBank.Play(HitSound);
            RetroCRTFilter.Hit();
            if (hitParticle != null)
                OneShotEffect.EnsureFrontSorting(Instantiate(hitParticle, transform.position + new Vector3(2, 0), Quaternion.identity, transform));
        }
    }

    public void OnHealed(float newLife)
    {
        life = Mathf.Clamp(newLife, 0f, maxLife > 0f ? maxLife : 100f);
        smoke?.SetLife(life, maxLife);
    }
}
