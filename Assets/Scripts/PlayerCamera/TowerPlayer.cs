using UnityEngine;

public class TowerPlayer : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] AK.Wwise.Event HitSound;
    [Space]
    public static TowerPlayer instance;
    [Range(0, 100)] public float life;
    [SerializeField] GameObject hitParticle;
    public float DealTime;
    [SerializeField] LayerMask EnemyLayer;

    public Unity.Entities.Entity Entity { get; set; }

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void TakeHit(float enemyDamage, float remainingLife = -1f)
    {
        if (remainingLife >= 0f)
            life = remainingLife;
        else
            life = Mathf.Max(0f, life - enemyDamage);

        if (enemyDamage > 0f)
        {
            HitSound.Post(gameObject);
            if (hitParticle != null)
                Instantiate(hitParticle, transform.position + new Vector3(2, 0), Quaternion.identity, transform);
        }
    }

    public void OnHealed(float newLife)
    {
        life = Mathf.Clamp(newLife, 0f, 100f);
    }
}
