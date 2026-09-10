using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    Vector3 shakeOffset;
    float shakeDuration;
    float shakeIntensity;
    float shakeTimer;

    public Vector3 Offset => shakeOffset;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    public static void Shake(float duration, float intensity)
    {
        if (Instance != null)
            Instance.TriggerShake(duration, intensity);
    }

    public static void MicroShake() => Shake(0.08f, 0.08f);
    public static void MediumShake() => Shake(0.18f, 0.22f);
    public static void HeavyShake() => Shake(0.32f, 0.45f);

    public void TriggerShake(float duration, float intensity)
    {
        shakeDuration = duration;
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        shakeTimer = duration;
    }

    void Update()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            float progress = Mathf.Clamp01(shakeTimer / shakeDuration);
            float currentIntensity = shakeIntensity * progress;

            shakeOffset = new Vector3(
                (Mathf.PerlinNoise(Time.time * 40f, 0f) - 0.5f) * 2f * currentIntensity,
                (Mathf.PerlinNoise(0f, Time.time * 40f) - 0.5f) * 2f * currentIntensity,
                0f
            );
        }
        else
        {
            shakeOffset = Vector3.zero;
            shakeIntensity = 0f;
        }
    }
}
