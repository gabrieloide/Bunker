using UnityEngine;
using System.Collections;

public class TowerBullet : Bullet
{
    [SerializeField] GameObject damageText;
    [SerializeField] float offsetDamageTextY = -4f;
    [SerializeField] float damageTextTime;
    public TowerBullet()
    {
        hitName = "Enemy";
    }
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            DamageTextMovement();
        }
        base.OnTriggerEnter2D(collision);

    }
    void DamageTextMovement()
    {
        GameObject dt = ObjectPooling.instance.TextDamage();
        dt.transform.position = transform.position;
        dt.SetActive(true);
        LeanTween.move(dt, transform.position + new Vector3(default, offsetDamageTextY, 0), damageTextTime).setEaseOutQuad();
    }
}
