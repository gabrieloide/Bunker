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

    // End of match: the camera travels to a point on its own (real time, pixel steps) and ignores the keys
    const float PixelSize = 1f / 16f;
    bool focusing;
    Vector3 focusFrom, focusTo;
    float focusStart, focusDuration;

    public void Focus(Vector3 target, float duration)
    {
        focusing = true;
        focusFrom = basePosition;
        focusTo = new Vector3(Mathf.Clamp(target.x, minX, maxX), Mathf.Clamp(target.y, minY, maxY), basePosition.z);
        focusStart = Time.unscaledTime;
        focusDuration = Mathf.Max(0.01f, duration);
    }

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
        if (focusing)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.unscaledTime - focusStart) / focusDuration));
            Vector3 p = Vector3.Lerp(focusFrom, focusTo, t);
            basePosition = new Vector3(Mathf.Round(p.x / PixelSize) * PixelSize, Mathf.Round(p.y / PixelSize) * PixelSize, p.z);
            return;
        }
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
