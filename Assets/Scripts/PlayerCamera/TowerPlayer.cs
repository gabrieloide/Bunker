using UnityEngine;

public class TowerPlayer : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] AK.Wwise.Event HitSound;
    [Space]
    public static TowerPlayer instance;
    public Loot[] loot;
    [Range(0, 100)] public float life;
    [SerializeField] GameObject hitParticle;
    public float DealTime;
    [SerializeField] LayerMask EnemyLayer;
    [SerializeField] bool canChangeChance;
    [SerializeField] int currentIndex = 9;

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

        UpdateCardChance();
    }

    public void OnHealed(float newLife)
    {
        life = Mathf.Clamp(newLife, 0f, 100f);
        UpdateCardChance();
    }

    void UpdateCardChance()
    {
        int lifeTier = Mathf.FloorToInt(life / 10f);
        if (lifeTier == currentIndex && canChangeChance)
        {
            float extraChance = IncreaseChance();
            foreach (var item in loot)
            {
                item.dropChance += Mathf.FloorToInt(extraChance);
            }
            currentIndex--;
            canChangeChance = false;
        }
        else if (lifeTier != currentIndex)
        {
            canChangeChance = true;
        }
    }

    float IncreaseChance()
    {
        float currentLifeTier = Mathf.Floor(life / 10f) * 10f;
        return (100f - currentLifeTier) / 10f;
    }
}
