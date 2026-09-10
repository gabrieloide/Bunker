using UnityEngine;

public class NormalTurret : TurretCard
{
    [SerializeField] bool rotate;
    [SerializeField] GameObject headRotation;

    public override Vector3 MuzzlePosition => headRotation != null ? headRotation.transform.position : transform.position;

    public override void OnFired(Vector3 targetPosition)
    {
        Vector3 relativePos = (targetPosition - transform.position).normalized;
        float dot = Vector2.Dot(transform.right, relativePos);

        if (BulletParticle != null)
            BulletParticle.SetActive(true);

        if (rotate && headRotation != null)
        {
            RotateObjectTo.Rotation(headRotation, targetPosition, headRotation.transform);
            headRotation.transform.localScale = dot > 0 ? new Vector3(1, 1, 1) : new Vector3(1, -1, 1);
        }

        shoot.Post(gameObject);
    }
}
