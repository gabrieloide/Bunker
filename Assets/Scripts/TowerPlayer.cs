using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerPlayer : MonoBehaviour
{
    public static TowerPlayer instance;
    public float life;
    Enemy enemy;
    [SerializeField] GameObject Projectile;
    [SerializeField] float bulletSpeed;
    [SerializeField] float bulletTimeLife;
    private void Start()
    {
        if (!instance)
        {
            instance = this;
        }
        enemy = FindObjectOfType<Enemy>();
    }
    private void Update()
    {

    }
    void LaunchBullet()
    {
        GameObject b = Instantiate(Projectile, transform.position, transform.rotation);
        Rigidbody2D rb2d = b.GetComponent<Rigidbody2D>();
        rb2d.velocity = (Vector2.right) * bulletSpeed;
        Destroy(b, bulletTimeLife);
    }

}
