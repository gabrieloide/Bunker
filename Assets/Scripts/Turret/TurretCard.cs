using System.Collections;
using UnityEngine;

public abstract class TurretCard : TurretStats, IDamageable
{
    private const string ENEMY_TAG = "Enemy";
    public float Life;

    [Space]
    [SerializeField] public float fireRateCountDown = 0f;
    [SerializeField] float fireRate = 0;

    [Range(3, 20)][SerializeField] protected float range = 3f;
    [SerializeField] protected GameObject BulletParticle;
    [SerializeField] GameObject TurretLifeSlider;
    [SerializeField] float TurretLifeSliderOffset;
    [SerializeField] Transform BuffSpritePosition;
    [HideInInspector] public GameObject _BuffSpritePosition;
    protected Transform target;
    [HideInInspector] public bool HaveBuff;
    LayerMask NormalCardLM() => LayerMask.GetMask("Path");

    protected void Start()
    {
        StartCoroutine(UpdateTargets());
        Instantiate(TurretLifeSlider, transform.position + new Vector3(default
                                                                , TurretLifeSliderOffset,
                                                                  default),
                                                                  Quaternion.identity, transform);

    }
    IEnumerator UpdateTargets()
    {
        while (true)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(ENEMY_TAG);
            float shortestDistance = Mathf.Infinity;
            GameObject nearestEnemy = null;
            foreach (GameObject enemy in enemies)
            {
                float distanceToEnemy = Vector2.Distance(transform.position, enemy.transform.position);
                if (distanceToEnemy < shortestDistance)
                {
                    shortestDistance = distanceToEnemy;
                    nearestEnemy = enemy;
                }
            }
            if (nearestEnemy != null && shortestDistance <= range)
            {
                target = nearestEnemy.transform;
            }
            else
            {
                target = null;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
    protected void Update()
    {
        if (Life < 0)
            Destroy(gameObject);

        fireRateCountDown -= Time.deltaTime;
        if (target == null)
            return;

        if (fireRateCountDown <= 0f)
        {
            fireRateCountDown = 1f / fireRate;
            TurretShoot();
        }

    }
    public abstract void TurretShoot();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
    public void Damage(float damage, float bulletPen, GameObject deactivateBullet)
    {
        Life -= damage;
        deactivateBullet.SetActive(false);
    }
    public void ShowBuffSprite(GameObject BuffSprite)
    {
        _BuffSpritePosition = Instantiate(BuffSprite, BuffSpritePosition.position, Quaternion.identity, BuffSpritePosition);
    }
}
