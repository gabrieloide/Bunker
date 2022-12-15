using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerD : MonoBehaviour
{
    public TowersData towersData;

    public Transform target;
    private string enemyTag = "Enemy";

    public float range = 3f;
    private float fireRateCountDown = 0f;

    public GameObject partToRotate;
    public GameObject bulletPrefab;

    void Start()
    {
        InvokeRepeating("UpdateTarget", 0f, 0.5f);
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
        /*
        Vector2 dir = target.position - partToRotate.transform.position;
        Quaternion lookRotation = Quaternion.LookRotation(dir);
        Vector2 rotation = lookRotation.eulerAngles;
        partToRotate.transform.rotation = Quaternion.Euler();
        */
        if(fireRateCountDown <= 0f)
        {
            Shoot();
            fireRateCountDown = 1f / towersData.fireRate;
        }

        fireRateCountDown -= Time.deltaTime;
    }

    void Shoot()
    {
        GameObject b = Instantiate(bulletPrefab, partToRotate.transform.position, partToRotate.transform.rotation);
        TowerBullet bullet = b.GetComponent<TowerBullet>();
        bullet.GetData(target, towersData.damage, towersData.lifeBullet);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }

}
