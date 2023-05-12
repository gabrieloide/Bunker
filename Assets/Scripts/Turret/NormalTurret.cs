using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalTurret : TurretCard
{
    public override void TurretShoot()
    {
        Instantiate(BulletParticle, nozzle.position, transform.rotation);
        //SFX PARA CADA VEZ QUE LA TORRETA DISPARA

        TowerBullet bullet = ObjectPooling.instance.Shoot().GetComponent<TowerBullet>();
        bullet.transform.position = nozzle.position;

        Vector3 relativePos = target.position - nozzle.position;
        float angle = Mathf.Atan2(relativePos.y, relativePos.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        bullet.GetData(target, towersData.bulletPen);
    }
}
