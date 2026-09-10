using UnityEngine;

public static class RotateObjectTo
{
    public static void Rotation(GameObject _object, Transform target, Transform nozzle)
    {
        Rotation(_object, target.position, nozzle);
    }

    public static void Rotation(GameObject _object, Vector3 targetPosition, Transform nozzle)
    {
        _object.transform.rotation = FromDirection(targetPosition - nozzle.position);
    }

    public static Quaternion FromDirection(Vector3 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
