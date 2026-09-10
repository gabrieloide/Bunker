using UnityEngine;

public class TowerBullet : Bullet
{
    [SerializeField] float offsetDamageTextY = -4f;
    [SerializeField] float damageTextTime;

    public float DamageTextOffsetY => offsetDamageTextY;
    public float DamageTextTime => damageTextTime;
}
