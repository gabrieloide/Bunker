using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class RetroCRTFilter : MonoBehaviour
{
    [Header("Shader")]
    [SerializeField] Shader crtShader;

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

    // The bunker's monitor fails as the bunker does: dithered edges beating like a heart, static, twitching lines,
    // and a tear on every hit. Black and white only, no tint.
    [Header("Low life")]
    [Tooltip("Share of life below which the monitor starts failing")]
    [Range(0f, 1f)] public float dangerStart = 0.35f;
    [Tooltip("Share of life at which the effect is at its worst")]
    [Range(0f, 1f)] public float dangerFull = 0.1f;
    [Tooltip("Heartbeat (beats per minute) when the effect starts and at its worst")]
    public float calmBpm = 70f, panicBpm = 130f;
    [Tooltip("Seconds a hit tears the picture")]
    public float glitchTime = 0.15f;
    [Tooltip("Units per second the effect fades after healing or winning")]
    public float recoverySpeed = 0.5f;
    [Tooltip("Seconds the dithering takes to close in when the bunker falls")]
    public float defeatSweepTime = 1.5f;

    static float lastHitTime = float.NegativeInfinity;
    float danger, beatPhase, pulse, defeat;

    // The bunker was hit: tears the picture if the monitor is already failing
    public static void Hit() => lastHitTime = Time.unscaledTime;

    Material material;

    Material Mat
    {
        get
        {
            if (material == null)
            {
                if (crtShader == null)
                    crtShader = Shader.Find("Hidden/RetroCRT");

                if (crtShader != null)
                {
                    material = new Material(crtShader);
                    material.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            return material;
        }
    }

    void OnValidate()
    {
        if (crtShader == null)
            crtShader = Shader.Find("Hidden/RetroCRT");
    }

    void Awake()
    {
        if (crtShader == null)
            crtShader = Shader.Find("Hidden/RetroCRT");
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        var bunker = TowerPlayer.instance;
        float lifeShare = bunker != null && bunker.maxLife > 0f ? bunker.life / bunker.maxLife : 1f;
        bool over = SimEventDispatcher.IsGameOver;

        // Lost: the monitor dies, the dithering closes in and the heart stops. Won: the effect fades away
        if (over && lifeShare <= 0f)
            defeat = Mathf.MoveTowards(defeat, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, defeatSweepTime));
        float target = over ? 0f : Mathf.InverseLerp(dangerStart, dangerFull, lifeShare);
        // Worse at once, better slowly
        danger = target > danger ? target : Mathf.MoveTowards(danger, target, recoverySpeed * Time.unscaledDeltaTime);

        if (danger <= 0f || over)
        {
            beatPhase = 0f;
            pulse = 0f;
            return;
        }
        float previous = beatPhase;
        beatPhase += Time.unscaledDeltaTime * Mathf.Lerp(calmBpm, panicBpm, danger) / 60f;
        if (Mathf.Floor(beatPhase) > Mathf.Floor(previous))
            SoundBank.Play(Sfx.LowLifeBeat);
        // Lub-dub: a strong beat and a softer one right after
        float p = beatPhase - Mathf.Floor(beatPhase);
        pulse = Mathf.Max(Mathf.Exp(-p * 10f), p > 0.18f ? 0.6f * Mathf.Exp(-(p - 0.18f) * 10f) : 0f);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (Mat == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        Mat.SetFloat("_Curvature", curvature);
        Mat.SetFloat("_BezelSmoothness", bezelSmoothness);
        Mat.SetFloat("_ScanlineIntensity", scanlineIntensity);
        Mat.SetFloat("_ScanlineCount", scanlineCount);
        Mat.SetFloat("_BloomGlow", phosphorGlow);
        Mat.SetFloat("_MaskIntensity", shadowMaskIntensity);
        Mat.SetFloat("_ChromaticAberration", chromaticAberration);
        Mat.SetFloat("_Brightness", brightness);
        Mat.SetFloat("_Contrast", contrast);
        Mat.SetFloat("_VignetteIntensity", vignetteIntensity);
        Mat.SetFloat("_VignetteRoundness", vignetteRoundness);
        float sinceHit = Time.unscaledTime - lastHitTime;
        Mat.SetFloat("_Danger", danger);
        Mat.SetFloat("_Pulse", pulse);
        Mat.SetFloat("_Glitch", danger > 0f && sinceHit < glitchTime ? 1f - sinceHit / glitchTime : 0f);
        Mat.SetFloat("_Defeat", defeat);

        RenderTexture inputTex = source;
        RenderTexture composited = null;
        var cam = GetComponent<Camera>();
        if (cam != null && cam.targetTexture == null && RetroCRTUIBridge.Instance != null)
        {
            if (RetroCRTUIBridge.Instance.TryComposite(source, out composited))
                inputTex = composited;
        }

        Graphics.Blit(inputTex, destination, Mat);

        if (composited != null)
            RenderTexture.ReleaseTemporary(composited);
    }

    void OnDisable()
    {
        if (material != null)
        {
            DestroyImmediate(material);
            material = null;
        }
    }
}
