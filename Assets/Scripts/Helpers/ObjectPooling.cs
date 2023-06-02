using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooling : MonoBehaviour
{
    public static ObjectPooling instance;
    List<GameObject> bulletsPooling = new List<GameObject>();
    List<GameObject> enemyBulletsPooling = new List<GameObject>();
    List<GameObject> textDamagePooling = new List<GameObject>();

    
    [SerializeField] GameObject turretBullet;
    [SerializeField] GameObject EnemyBullet;
    [SerializeField] GameObject textDamage;
    [SerializeField] int amountToPool;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            GameObject newBullet = Instantiate(turretBullet, transform.position, transform.rotation, transform);
            newBullet.SetActive(false);
            bulletsPooling.Add(newBullet);

            GameObject bulletEnemy = Instantiate(EnemyBullet, transform.position, Quaternion.identity, transform);
            bulletEnemy.SetActive(false);
            enemyBulletsPooling.Add(bulletEnemy);

            GameObject newTextDamage = Instantiate(textDamage, transform.position, Quaternion.identity, transform);
            newTextDamage.SetActive(false);
            textDamagePooling.Add(newTextDamage);
        }
    }
    void AddLasersToPool(int amount, GameObject _bullet, List<GameObject> pool)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject bullet = Instantiate(_bullet);
            bullet.SetActive(false);
            pool.Add(bullet);
            bullet.transform.parent = transform;
        }
    }
    public GameObject TextDamage()
    {
        return Shoot(textDamagePooling, textDamage);
    }
    public GameObject EnemyShoot()
    {
        return Shoot(enemyBulletsPooling, EnemyBullet);
    }
    public GameObject TurretShoot()
    {
        return Shoot(bulletsPooling, turretBullet);
    }
    public GameObject Shoot(List<GameObject> bulletsPooling, GameObject _bullet)
    {
        for (int i = 0; i < bulletsPooling.Count; i++)
        {
            if (!bulletsPooling[i].activeSelf)
            {
                bulletsPooling[i].SetActive(true);
                return bulletsPooling[i];
            }
        }
        AddLasersToPool(1, _bullet, bulletsPooling);
        bulletsPooling[bulletsPooling.Count - 1].SetActive(true);
        return bulletsPooling[bulletsPooling.Count - 1];
    }
}
