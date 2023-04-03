using UnityEngine;

public class TowerD : MonoBehaviour
{
    public TowersData towersData;

    [HideInInspector] public Transform target;
    public Transform nozzle;
    private string enemyTag = "Enemy";
    [HideInInspector] public int damage;
    public float range = 3f;
    public float fireRateCountDown = 0f;

    public GameObject bulletPrefab;
    [SerializeField] GameObject BulletParticle;
    public float timeToDestroy = 30f;
    [SerializeField] GameObject TurretLifeSlider;
    [SerializeField] float TurretLifeSliderOffset;

    void Start()
    {
        InvokeRepeating("UpdateTarget", 0f, 0.5f);
        Instantiate(TurretLifeSlider, transform.position + new Vector3(default
                                                                , TurretLifeSliderOffset,
                                                                  default), Quaternion.identity, transform);
        Destroy(gameObject, timeToDestroy);
        damage = towersData.damage;
    }
    void UpdateTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
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
    }
    void Update()
    {
        fireRateCountDown -= Time.deltaTime;

        if (target == null)
            return;

        if (fireRateCountDown <= 0f)
        {
            fireRateCountDown = 1f / towersData.fireRate;
            Shoot();
        }
    }
    void Shoot()
    {
        Instantiate(BulletParticle, nozzle.position, transform.rotation);
        //SFX PARA CADA VEZ QUE LA TORRETA DISPARA

        TowerBullet bullet = ObjectPooling.instance.Shoot().GetComponent<TowerBullet>();
        bullet.transform.position = nozzle.position;

        //Vector3 newBulletDirection = target.position - nozzle.transform.position;
        //float angle = Mathf.Atan(newRot.y / newRot.x);
        //float RotationDegree = Mathf.Acos(Vector2.Dot(nozzle.transform.position, target.transform.position)
        //    / (nozzle.transform.position.magnitude * target.transform.position.magnitude));

        Vector3 newZ = bullet.transform.up = target.position - nozzle.position;
        bullet.transform.rotation = Quaternion.Euler(0, 0, newZ.z);
        bullet.GetData(target, damage, towersData.lifeBullet);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }

}
