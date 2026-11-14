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
    const float TouchPanThresholdPx = 10f;
    bool focusing;
    Vector3 focusFrom, focusTo;
    float focusStart, focusDuration;

    bool touchPanCandidate;
    bool touchPanning;
    Vector2 touchPanStartScreen;
    Vector2 touchPanLastScreen;

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
        Vector2 axis = BunkerInput.MoveCamera;
        Vector3 move = new Vector3(axis.x * speed * Time.deltaTime, axis.y * speed * Time.deltaTime, 0f);
        basePosition += move;

        UpdateTouchPan();

        basePosition.x = Mathf.Clamp(basePosition.x, minX, maxX);
        basePosition.y = Mathf.Clamp(basePosition.y, minY, maxY);
    }

    void UpdateTouchPan()
    {
        bool blocked = PauseMenu.GameIsPaused || (GameManager.instance != null && GameManager.instance.onDrag);
        if (blocked || !PointerInput.IsTouch || !PointerInput.IsPressed)
        {
            touchPanCandidate = false;
            touchPanning = false;
            return;
        }

        if (!PointerInput.TryGetScreenPosition(out Vector2 screenPos))
            return;

        if (PointerInput.WasPressedThisFrame && !PointerInput.IsPointerOverUI())
        {
            touchPanCandidate = true;
            touchPanning = false;
            touchPanStartScreen = screenPos;
            touchPanLastScreen = screenPos;
            return;
        }

        if (!touchPanCandidate) return;

        if (!touchPanning)
        {
            if (Vector2.Distance(screenPos, touchPanStartScreen) < TouchPanThresholdPx)
                return;
            touchPanning = true;
            touchPanLastScreen = screenPos;
            return;
        }

        Vector2 delta = screenPos - touchPanLastScreen;
        touchPanLastScreen = screenPos;

        var cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float worldPerPixel = (cam.orthographicSize * 2f) / Mathf.Max(1, Screen.height);
            basePosition -= new Vector3(delta.x * worldPerPixel, delta.y * worldPerPixel, 0f);
        }
    }

    void LateUpdate()
    {
        Vector3 shake = CameraShake.Instance != null ? CameraShake.Instance.Offset : Vector3.zero;
        transform.position = new Vector3(basePosition.x + shake.x, basePosition.y + shake.y, basePosition.z);
    }
}
