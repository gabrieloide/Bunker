using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CameraShake))]
public class CameraMovement : MonoBehaviour
{
    public static CameraMovement instance;
    public float minX, maxX;
    public float minY, maxY;
    public float speed;

    Vector3 basePosition;

    void Start()
    {
        if (!instance)
        {
            instance = this;
        }
        basePosition = transform.position;
    }

    void Update()
    {
        float X = Input.GetAxis("Horizontal");
        float Y = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(X * speed * Time.deltaTime, Y * speed * Time.deltaTime, 0f);
        basePosition += move;
        basePosition.x = Mathf.Clamp(basePosition.x, minX, maxX);
        basePosition.y = Mathf.Clamp(basePosition.y, minY, maxY);
    }

    void LateUpdate()
    {
        Vector3 shake = CameraShake.Instance != null ? CameraShake.Instance.Offset : Vector3.zero;
        transform.position = new Vector3(basePosition.x + shake.x, basePosition.y + shake.y, basePosition.z);
    }
}
