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

    public float timeToDestroy = 30f;

    void Start()
    {
        InvokeRepeating("UpdateTarget", 0f, 0.5f);
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
        fireRateCountDown -= Time.deltaTime;
        
        if (target == null)
            return;

        if(fireRateCountDown <= 0f)
        {
            fireRateCountDown = 1f / towersData.fireRate;
            Shoot();
        }
    }

    void Shoot()
    {

        GameObject b = Instantiate(bulletPrefab, transform.position, transform.rotation);
        b.transform.right = -1*(transform.position - target.position);
        TowerBullet bullet = b.GetComponent<TowerBullet>();
        bullet.GetData(target, damage, towersData.lifeBullet);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }

}
