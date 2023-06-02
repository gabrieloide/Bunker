using UnityEngine;

public class NormalTurret : TurretCard
{
    [SerializeField] bool rotate;
    [SerializeField] GameObject headRotation;

    public override void TurretShoot()
    {
        Vector3 relativePos = (target.position - transform.position).normalized;

        float dot = Vector2.Dot(transform.right, relativePos);
        BulletParticle.SetActive(true);
        TowerBullet bullet = ObjectPooling.instance.TurretShoot().GetComponent<TowerBullet>();
        bullet.transform.position = headRotation.transform.position;
        
        if (rotate)
        {
            RotateObjectTo.Rotation(headRotation, target, headRotation.transform);
            Vector3 scale = dot > 0 ? new Vector3(1, 1, 1) : new Vector3(1, -1, 1);
            headRotation.transform.localScale = scale;
        }

        RotateObjectTo.Rotation(bullet.gameObject, target, headRotation.transform);

        bullet.GetData(target.position, towersData.damage, towersData.bulletPen);
        shoot.Post(gameObject);
    }
}
