using UnityEngine;
using UnityEngine.SceneManagement;

// Persistent Global CRT controller:
// Keeps the authentic bunker RetroCRT filter active across all scenes via DontDestroyOnLoad.
[DefaultExecutionOrder(-1000)]
public class GlobalRetroCRT : MonoBehaviour
{
    public static GlobalRetroCRT Instance { get; private set; }

    [Header("Screen Curvature & Bezel")]
    [Range(0f, 0.25f)] public float curvature = 0.05f;
    [Range(0.001f, 0.05f)] public float bezelSmoothness = 0.015f;

    [Header("Scanlines")]
    [Range(0f, 1f)] public float scanlineIntensity = 0.32f;
    public float scanlineCount = 270f;

    [Header("Phosphor & Aesthetics")]
    [Range(0f, 0.5f)] public float phosphorGlow = 0.12f;
    [Range(0f, 0.4f)] public float shadowMaskIntensity = 0.10f;
    [Range(0f, 0.006f)] public float chromaticAberration = 0.0015f;
    [Range(0.6f, 1.4f)] public float brightness = 1.05f;
    [Range(0.6f, 1.4f)] public float contrast = 1.05f;

    [Header("Vignette")]
    [Range(0f, 1f)] public float vignetteIntensity = 0.35f;
    [Range(0.1f, 1f)] public float vignetteRoundness = 0.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ApplyFilterToActiveCameras();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ApplyFilterToActiveCameras();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyFilterToActiveCameras();
    }

    public void ApplyFilterToActiveCameras()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            if (cam == null) continue;

            // Skip helper cameras that only render to small mini textures
            if (cam.targetTexture != null && cam.targetTexture.width < 256) continue;

            ApplyToCamera(cam);
        }
    }

    public void ApplyToCamera(Camera cam)
    {
        if (cam == null) return;

        var filter = cam.GetComponent<RetroCRTFilter>();
        if (filter == null)
        {
            filter = cam.gameObject.AddComponent<RetroCRTFilter>();
        }

        filter.curvature = curvature;
        filter.bezelSmoothness = bezelSmoothness;
        filter.scanlineIntensity = scanlineIntensity;
        filter.scanlineCount = scanlineCount;
        filter.phosphorGlow = phosphorGlow;
        filter.shadowMaskIntensity = shadowMaskIntensity;
        filter.chromaticAberration = chromaticAberration;
        filter.brightness = brightness;
        filter.contrast = contrast;
        filter.vignetteIntensity = vignetteIntensity;
        filter.vignetteRoundness = vignetteRoundness;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyFilterToActiveCameras();
        }
    }
#endif
}
