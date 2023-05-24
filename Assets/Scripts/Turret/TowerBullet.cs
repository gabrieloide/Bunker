using UnityEngine;

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
        base.OnTriggerEnter2D(collision);

        DamageTextMovement();
    }
    void DamageTextMovement()
    {
        GameObject dt = Instantiate(damageText, transform.position, Quaternion.identity);
        LeanTween.move(dt, transform.position + new Vector3(default, offsetDamageTextY, 0), damageTextTime).setEaseOutQuad();
        Destroy(dt, damageTextTime);
    }
}
