using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerPlayer : MonoBehaviour
{
    public static TowerPlayer instance;
    public float life;
    [SerializeField] GameObject Projectile;
    [SerializeField] float bulletSpeed;
    [SerializeField] LayerMask Enemy;
    [SerializeField] float fireRate;
    private void Start()
    {
        if (!instance)
        {
            instance = this;
        }
        
    }
    private void Update()
    {
        bool hitEnemy = Physics2D.Raycast(transform.position, Vector2.right, 15, Enemy);
        if (hitEnemy)
        {
            Invoke("LaunchBullet", fireRate);
        }
    }
    void LaunchBullet()
    {
        GameObject b = Instantiate(Projectile, transform.position, transform.rotation);
        CancelInvoke("LaunchBullet");
        Rigidbody2D rb2d = b.GetComponent<Rigidbody2D>();
        rb2d.velocity = Vector2.right * bulletSpeed;
        Destroy(b, 0.7f);
    }

}