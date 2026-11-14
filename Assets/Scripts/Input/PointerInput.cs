using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Unified pointer state over Unity's New Input System (mouse, touch, and pen).
/// </summary>
public static class PointerInput
{
    public static bool HasPointer => Pointer.current != null;
    public static bool IsTouch => Pointer.current is Touchscreen;

    public static bool IsPressed => Pointer.current != null && Pointer.current.press.isPressed;
    public static bool WasPressedThisFrame => Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
    public static bool WasReleasedThisFrame => Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;

    public static Vector2 Delta => Pointer.current != null ? Pointer.current.delta.ReadValue() : Vector2.zero;

    public static bool TryGetScreenPosition(out Vector2 screenPos)
    {
        var pointer = Pointer.current;
        if (pointer == null)
        {
            screenPos = default;
            return false;
        }

        Vector2 pos = pointer.position.ReadValue();
        if (!float.IsFinite(pos.x) || !float.IsFinite(pos.y))
        {
            screenPos = default;
            return false;
        }

        screenPos = pos;
        return true;
    }

    public static Vector2 ScreenPositionOrZero()
    {
        return TryGetScreenPosition(out Vector2 pos) ? pos : Vector2.zero;
    }

    public static bool TryGetWorldPosition(Camera cam, out Vector3 worldPos)
    {
        if (cam == null || !TryGetScreenPosition(out Vector2 screenPos))
        {
            worldPos = Vector3.zero;
            return false;
        }

        worldPos = cam.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;
        return true;
    }

    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (Pointer.current is Touchscreen ts)
        {
            int touchId = ts.primaryTouch.touchId.ReadValue();
            return touchId != 0 && EventSystem.current.IsPointerOverGameObject(touchId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
}
