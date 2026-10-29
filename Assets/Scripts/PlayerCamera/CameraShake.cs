using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Shake Configuration")]
    [Tooltip("Global multiplier applied to camera shake intensity")]
    [SerializeField] float intensityMultiplier = 1f;

    [Tooltip("Maximum clamp for shake offset magnitude to avoid disorienting movements")]
    [SerializeField] float maxIntensityCap = 0.12f;

    Vector3 shakeOffset;
    float shakeDuration;
    float shakeIntensity;
    float shakeTimer;

    float seedX;
    float seedY;

    public Vector3 Offset => shakeOffset;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }

        if (intensityMultiplier <= 0f)
            intensityMultiplier = 1f;
        if (maxIntensityCap <= 0f)
            maxIntensityCap = 0.12f;

        seedX = Random.Range(10f, 500f);
        seedY = Random.Range(500f, 1000f);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnDisable()
    {
        shakeOffset = Vector3.zero;
        shakeTimer = 0f;
        shakeIntensity = 0f;
        shakeDuration = 0f;
    }

    public static void Shake(float duration, float intensity)
    {
        if (Instance != null)
            Instance.TriggerShake(duration, intensity);
    }

    // Drastically reduced presets (20%-25% of original intensity and 37%-50% duration)
    // for subtle micro-shakes that do not violently jar the screen.
    public static void MicroShake() => Shake(0.04f, 0.02f);
    public static void MediumShake() => Shake(0.08f, 0.05f);
    public static void HeavyShake() => Shake(0.12f, 0.09f);

    public void TriggerShake(float duration, float intensity)
    {
        if (duration <= 0f || intensity <= 0f)
            return;

        float currentRemaining = shakeTimer > 0f && shakeDuration > 0f
            ? shakeIntensity * Mathf.Clamp01(shakeTimer / shakeDuration)
            : 0f;

        float targetIntensity = Mathf.Clamp(intensity * intensityMultiplier, 0f, maxIntensityCap);

        if (targetIntensity >= currentRemaining)
        {
            shakeIntensity = targetIntensity;
            shakeDuration = duration;
            shakeTimer = duration;
        }
        else
        {
            // The ongoing shake is stronger: do not truncate its intensity or remaining time.
            // If the incoming shake has a longer duration than what remains, extend smoothly.
            if (duration > shakeTimer)
            {
                shakeDuration = duration;
                shakeTimer = duration;
                shakeIntensity = currentRemaining;
            }
        }
    }

    void Update()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0f)
            {
                shakeTimer = 0f;
                shakeDuration = 0f;
                shakeIntensity = 0f;
                shakeOffset = Vector3.zero;
                return;
            }

            float progress = shakeDuration > 0f ? Mathf.Clamp01(shakeTimer / shakeDuration) : 0f;
            float currentIntensity = shakeIntensity * progress;

            shakeOffset = new Vector3(
                (Mathf.PerlinNoise(Time.time * 40f + seedX, seedX) - 0.5f) * 2f * currentIntensity,
                (Mathf.PerlinNoise(seedY, Time.time * 40f + seedY) - 0.5f) * 2f * currentIntensity,
                0f
            );
        }
        else
        {
            shakeOffset = Vector3.zero;
            shakeIntensity = 0f;
            shakeDuration = 0f;
        }
    }
}
