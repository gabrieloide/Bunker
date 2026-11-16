using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10000)]
public class PersistentCameraGuard : MonoBehaviour
{
    public static PersistentCameraGuard Instance { get; private set; }

    public Camera FallbackCamera => fallbackCamera;
    public bool IsFallbackActive => fallbackCamera != null && fallbackCamera.enabled;

    Camera fallbackCamera;
    AudioListener audioListener;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            var go = new GameObject("[PersistentCamera]");
            go.AddComponent<PersistentCameraGuard>();
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupComponents();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            Evaluate();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void SetupComponents()
    {
        fallbackCamera = gameObject.AddComponent<Camera>();
        fallbackCamera.orthographic = true;
        fallbackCamera.clearFlags = CameraClearFlags.SolidColor;
        fallbackCamera.backgroundColor = Color.black;
        fallbackCamera.cullingMask = 0;
        fallbackCamera.depth = -100f;

        var urpData = gameObject.GetComponent<UniversalAdditionalCameraData>();
        if (urpData == null)
            urpData = gameObject.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderType = CameraRenderType.Base;
        urpData.renderPostProcessing = false;

        audioListener = gameObject.AddComponent<AudioListener>();
    }

    void LateUpdate()
    {
        Evaluate();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Evaluate();
    }

    void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        Evaluate();
    }

    void Evaluate()
    {
        if (fallbackCamera == null) return;

        bool shouldEnable = ShouldEnableFallback(CountOtherScreenCameras(fallbackCamera));
        if (fallbackCamera.enabled != shouldEnable)
            fallbackCamera.enabled = shouldEnable;
        if (audioListener != null && audioListener.enabled != shouldEnable)
            audioListener.enabled = shouldEnable;
    }

    public static bool ShouldEnableFallback(int otherScreenCameraCount) => otherScreenCameraCount <= 0;

    public static int CountOtherScreenCameras(Camera self)
    {
        var cameras = Camera.allCameras;
        int count = 0;
        for (int i = 0; i < cameras.Length; i++)
        {
            var cam = cameras[i];
            if (cam == null || cam == self) continue;
            if (cam.targetTexture != null) continue;
            if (!cam.isActiveAndEnabled) continue;
            count++;
        }
        return count;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        if (Instance == this)
            Instance = null;
    }
}
