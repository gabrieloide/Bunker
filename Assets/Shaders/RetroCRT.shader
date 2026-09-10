Shader "Hidden/RetroCRT"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Curvature ("Screen Curvature", Range(0, 0.5)) = 0.08
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.35
        _ScanlineCount ("Scanline Count", Float) = 360.0
        _VignetteIntensity ("Vignette Intensity", Range(0, 2)) = 0.4
        _VignetteRoundness ("Vignette Roundness", Range(0.1, 1)) = 0.5
        _BezelSmoothness ("Bezel Smoothness", Range(0.001, 0.1)) = 0.02
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.01)) = 0.002
        _MaskIntensity ("Phosphor Mask Intensity", Range(0, 1)) = 0.15
        _Brightness ("Brightness", Range(0.5, 2.0)) = 1.05
        _Contrast ("Contrast", Range(0.5, 2.0)) = 1.05
        _BloomGlow ("Phosphor Glow", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _Curvature;
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _VignetteIntensity;
            float _VignetteRoundness;
            float _BezelSmoothness;
            float _ChromaticAberration;
            float _MaskIntensity;
            float _Brightness;
            float _Contrast;
            float _BloomGlow;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // High-quality barrel distortion
            float2 CurveUV(float2 uv, float curvature)
            {
                if (curvature <= 0.0001) return uv;
                float2 centered = uv * 2.0 - 1.0;
                float2 offset = centered.yx / float2(curvature * 15.0 + 8.0, curvature * 15.0 + 8.0);
                centered += centered * offset * offset;
                return centered * 0.5 + 0.5;
            }

            // Clean bezel frame border cutoff
            float BezelMask(float2 uv, float smoothness)
            {
                float2 corner = min(uv, 1.0 - uv);
                float dist = min(corner.x, corner.y);
                return smoothstep(0.0, smoothness, dist);
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 curvedUV = CurveUV(i.uv, _Curvature);

                // Check out of bounds (clean black border outside CRT glass)
                if (curvedUV.x < 0.0 || curvedUV.x > 1.0 || curvedUV.y < 0.0 || curvedUV.y > 1.0)
                {
                    return float4(0.01, 0.01, 0.01, 1.0);
                }

                // Chromatic aberration (subtle RGB edge separation)
                float2 caOffset = (curvedUV - 0.5) * _ChromaticAberration;
                float r = tex2D(_MainTex, curvedUV - caOffset).r;
                float g = tex2D(_MainTex, curvedUV).g;
                float b = tex2D(_MainTex, curvedUV + caOffset).b;
                float3 col = float3(r, g, b);

                // Phosphor Bloom / Soft Glow around bright pixels
                if (_BloomGlow > 0.0)
                {
                    float2 step = _MainTex_TexelSize.xy * 1.5;
                    float3 blur = (
                        tex2D(_MainTex, curvedUV + float2(-step.x, 0.0)).rgb +
                        tex2D(_MainTex, curvedUV + float2( step.x, 0.0)).rgb +
                        tex2D(_MainTex, curvedUV + float2(0.0, -step.y)).rgb +
                        tex2D(_MainTex, curvedUV + float2(0.0,  step.y)).rgb
                    ) * 0.25;
                    col += blur * _BloomGlow;
                }

                // Contrast & Brightness
                col = (col - 0.5) * _Contrast + 0.5;
                col *= _Brightness;

                // Scanlines (Clean sine wave mapped across scanline count)
                if (_ScanlineIntensity > 0.0)
                {
                    float scan = sin(curvedUV.y * _ScanlineCount * 6.2831853);
                    scan = scan * 0.5 + 0.5;
                    scan = pow(scan, 0.85);
                    col *= lerp(1.0, scan, _ScanlineIntensity);
                }

                // Phosphor Shadow Mask (Aperture Grille Triad columns)
                if (_MaskIntensity > 0.0)
                {
                    float maskCol = fmod(floor(i.vertex.x), 3.0);
                    float3 mask = float3(1.0, 1.0, 1.0);
                    if (maskCol == 0.0) mask = float3(1.0, 0.78, 0.78);
                    else if (maskCol == 1.0) mask = float3(0.78, 1.0, 0.78);
                    else mask = float3(0.78, 0.78, 1.0);

                    col = lerp(col, col * mask, _MaskIntensity);
                }

                // Vignette (smooth corner shading)
                if (_VignetteIntensity > 0.0)
                {
                    float2 vigCoord = curvedUV * (1.0 - curvedUV);
                    float vig = vigCoord.x * vigCoord.y * 15.0;
                    vig = saturate(pow(vig, _VignetteRoundness));
                    col *= lerp(1.0 - _VignetteIntensity, 1.0, vig);
                }

                // Bezel cutoff
                col *= BezelMask(curvedUV, _BezelSmoothness);

                return float4(saturate(col), 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
