using UnityEngine;

public static class RotateObjectTo
{
    public static void Rotation(GameObject _object, Transform target, Transform nozzle)
    {
        Vector3 relativePos = target.position - nozzle.position;
        float angle = Mathf.Atan2(relativePos.y, relativePos.x) * Mathf.Rad2Deg;
        _object.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}