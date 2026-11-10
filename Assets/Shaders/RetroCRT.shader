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
        // Low life: the bunker's monitor fails. Pure black and white, no tint (see RetroCRTFilter)
        _Danger ("Danger (0 healthy, 1 dying)", Range(0, 1)) = 0
        _Pulse ("Heartbeat Pulse", Range(0, 1)) = 0
        _Glitch ("Hit Glitch", Range(0, 1)) = 0
        _Defeat ("Defeat (monitor dying)", Range(0, 1)) = 0
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
            float _Danger;
            float _Pulse;
            float _Glitch;
            float _Defeat;

            // The game's two colours, so the effect looks like the art (it goes through the same contrast/scanlines/vignette)
            static const float3 PaletteDark = float3(34.0, 35.0, 35.0) / 255.0;
            static const float3 PaletteLight = float3(240.0, 246.0, 240.0) / 255.0;

            static const float Bayer4[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            // Pattern-free hash (Dave Hoskins); the sin() one draws diagonal stripes with large inputs
            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

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

                // A hit at low life tears some bands sideways for a moment (nothing moves otherwise: steady motion made people dizzy)
                float2 sampleUV = curvedUV;
                if (_Glitch > 0.0)
                {
                    float band = floor(curvedUV.y * 90.0);
                    float frame = floor(_Time.y * 24.0);
                    float tear = step(0.6, Hash(float2(band * 1.7, frame + 3.0))) * 0.012 * _Glitch;
                    sampleUV.x += (Hash(float2(band, frame + 7.0)) - 0.5) * 2.0 * tear;
                }

                // Chromatic aberration (subtle RGB edge separation)
                float2 caOffset = (curvedUV - 0.5) * _ChromaticAberration;
                float r = tex2D(_MainTex, sampleUV - caOffset).r;
                float g = tex2D(_MainTex, sampleUV).g;
                float b = tex2D(_MainTex, sampleUV + caOffset).b;
                float3 col = float3(r, g, b);

                // Phosphor Bloom / Soft Glow around bright pixels
                if (_BloomGlow > 0.0)
                {
                    float2 step = _MainTex_TexelSize.xy * 1.5;
                    float3 blur = (
                        tex2D(_MainTex, sampleUV + float2(-step.x, 0.0)).rgb +
                        tex2D(_MainTex, sampleUV + float2( step.x, 0.0)).rgb +
                        tex2D(_MainTex, sampleUV + float2(0.0, -step.y)).rgb +
                        tex2D(_MainTex, sampleUV + float2(0.0,  step.y)).rgb
                    ) * 0.25;
                    col += blur * _BloomGlow;
                }

                // Low life / defeat, in art-pixel blocks and palette colours, before the CRT look so it matches the art
                if (_Danger > 0.0 || _Glitch > 0.0 || _Defeat > 0.0)
                {
                    float block = max(1.0, floor(_ScreenParams.y / 270.0));
                    float2 cell = floor(i.vertex.xy / block);

                    // Static: rare specks, two art pixels wide, that swap the two palette colours
                    float speck = Hash(floor(cell / 2.0) + fmod(floor(_Time.y * 20.0), 97.0) * 31.0);
                    if (speck < _Danger * 0.0006 + _Glitch * 0.01)
                        col = PaletteDark + PaletteLight - saturate(col);

                    // 1-bit dithered vignette: at low life it hugs the corners and barely breathes with the heart;
                    // on defeat it sweeps in from all edges, leaving the middle (the result panel) clean
                    float2 edgeCoord = curvedUV * (1.0 - curvedUV);
                    float edgeLinear = 1.0 - sqrt(saturate(edgeCoord.x * edgeCoord.y * 15.0));
                    float edgeLowLife = edgeLinear * edgeLinear * edgeLinear;
                    float edgeDefeat = saturate((edgeLinear - 0.35) / 0.65);
                    // The small offset keeps the sparsest dither level (a regular mesh of lone dots) away from the play area
                    float lowLife = edgeLowLife * _Danger * (0.85 + 0.15 * _Pulse) * 0.5 - 0.04;
                    float value = lerp(lowLife, edgeDefeat * 2.0, _Defeat);
                    int bayerIndex = (int)(fmod(cell.x, 4.0) + fmod(cell.y, 4.0) * 4.0);
                    if (value > (Bayer4[bayerIndex] + 0.5) / 16.0)
                        col = PaletteDark;
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
