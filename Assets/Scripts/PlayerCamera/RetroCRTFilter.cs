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

        Graphics.Blit(source, destination, Mat);
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
