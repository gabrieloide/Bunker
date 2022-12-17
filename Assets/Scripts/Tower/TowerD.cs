using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerD : MonoBehaviour
{
    public TowersData towersData;

    public Transform target;
    private string enemyTag = "Enemy";
    public int damage;
    public float range = 3f;
    public float fireRateCountDown = 0f;

    public GameObject bulletPrefab;

    [SerializeField]
    private float timeToDestroy = 30f;

    void Start()
    {
        damage = towersData.damage;
        InvokeRepeating("UpdateTarget", 0f, 0.5f);
        Destroy(gameObject, timeToDestroy);
    }

    void UpdateTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;
        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector2.Distance(transform.position, enemy.transform.position);
            if(distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        if(nearestEnemy != null && shortestDistance <= range)
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
        if (target == null)
            return;
        
        if(fireRateCountDown <= 0f)
        {
            fireRateCountDown = 1f / towersData.fireRate;
            Shoot();
        }

        fireRateCountDown -= Time.deltaTime;
    }

    void Shoot()
    {
        GameObject b = Instantiate(bulletPrefab, transform.position, transform.rotation);
        TowerBullet bullet = b.GetComponent<TowerBullet>();
        bullet.GetData(target, towersData.damage, towersData.lifeBullet);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }

}
