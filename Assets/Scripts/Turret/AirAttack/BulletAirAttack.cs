using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletAirAttack : MonoBehaviour
{
    [SerializeField] GameObject explotionParticle;
    [SerializeField] protected TowersData towersData;
    public float Damage
    {
        get { return towersData.damage; }
        set { towersData.damage = value; }
    }
    public float BulletPen
    {
        get { return towersData.bulletPen; }
        set { towersData.bulletPen = value; }
    }
    public float bulletPen;

    private void OnDestroy()
    {
        Instantiate(explotionParticle, transform.position, Quaternion.identity);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.Damage(Damage, bulletPen);
        }
    }
}