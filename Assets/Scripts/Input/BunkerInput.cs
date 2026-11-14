using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central New Input System actions for gameplay, camera, pause, and cutscene skip.
/// Works across all scenes without requiring per-scene PlayerInput wiring.
/// </summary>
public static class BunkerInput
{
    static InputActionMap gameplayMap;
    static InputAction moveCameraAction;
    static InputAction pauseAction;
    static InputAction skipAction;
    static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitOnLoad()
    {
        EnsureInitialized();
    }

    static void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        gameplayMap = new InputActionMap("Gameplay");

        moveCameraAction = gameplayMap.AddAction("MoveCamera", InputActionType.Value, expectedControlLayout: "Vector2");
        moveCameraAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveCameraAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveCameraAction.AddBinding("<Gamepad>/leftStick");

        pauseAction = gameplayMap.AddAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/p");
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");

        skipAction = gameplayMap.AddAction("Skip", InputActionType.Button);
        skipAction.AddBinding("<Pointer>/press");
        skipAction.AddBinding("<Keyboard>/space");
        skipAction.AddBinding("<Keyboard>/enter");
        skipAction.AddBinding("<Gamepad>/buttonSouth");

        gameplayMap.Enable();
    }

    public static Vector2 MoveCamera
    {
        get
        {
            EnsureInitialized();
            return moveCameraAction.ReadValue<Vector2>();
        }
    }

    public static bool PauseWasPressedThisFrame
    {
        get
        {
            EnsureInitialized();
            return pauseAction.WasPressedThisFrame();
        }
    }

    public static bool SkipWasPressedThisFrame
    {
        get
        {
            EnsureInitialized();
            return skipAction.WasPressedThisFrame();
        }
    }

    public static bool AnyInputWasPressedThisFrame =>
        (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
        PointerInput.WasPressedThisFrame ||
        (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
}
