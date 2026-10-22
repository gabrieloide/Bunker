Shader "Bunker/CRTChannelSwitch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress ("Progress (0..1)", Range(0, 1)) = 0.0
        _Phase ("Phase (0=Close/Out, 1=Open/In)", Float) = 0.0
        _BeamColor ("Beam Phosphor Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _StaticColor ("Static Noise Color", Color) = (0.9, 0.95, 1.0, 1.0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay+500"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _BeamColor;
            fixed4 _StaticColor;
            float _Progress;
            float _Phase;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float2 p = uv - 0.5;
                float distX = abs(p.x);
                float distY = abs(p.y);
                float t = saturate(_Progress);

                // --- PHASE 0: TRANSITION OUT (CCTV Static Glitch & CRT Snap Collapse) ---
                if (_Phase < 0.5)
                {
                    // 1. Initial CCTV Channel Desync & Static Snow Burst (t: 0.0 -> 0.32)
                    if (t < 0.32)
                    {
                        float pGlitch = t / 0.32;

                        // Rolling CCTV V-hold bar
                        float vHoldBar = smoothstep(0.08, 0.0, abs(frac(uv.y * 1.5 - _Time.y * 8.0) - 0.5));

                        // Random scanline noise lines
                        float lineRnd = Hash21(float2(floor(uv.y * 140.0), floor(_Time.y * 55.0)));
                        float isNoiseLine = step(0.80 - pGlitch * 0.45, lineRnd);

                        // Pixelated static grain
                        float grain = Hash21(float2(floor(uv.x * 280.0), floor(uv.y * 180.0) + _Time.y * 60.0));

                        if (isNoiseLine > 0.5)
                        {
                            float alpha = (grain * 0.75 + 0.25) * (0.3 + pGlitch * 0.7);
                            return fixed4(_StaticColor.rgb, alpha);
                        }

                        // Black flicker during sync loss
                        float blackFlicker = vHoldBar * 0.4 + step(0.65, Hash21(float2(floor(_Time.y * 40.0), 12.3))) * pGlitch * 0.35;
                        return fixed4(0, 0, 0, saturate(blackFlicker));
                    }
                    // 2. Cathode Beam Snap Collapse into Black (t: 0.32 -> 1.0)
                    else
                    {
                        float pClose = (t - 0.32) / 0.68;

                        // Vertical collapse from full height to line (0.0 -> 0.80)
                        float openHeight = (1.0 - smoothstep(0.0, 0.80, pClose)) * 0.5;

                        // Horizontal collapse from wide line to center dot (0.75 -> 0.95)
                        float openWidth = (1.0 - smoothstep(0.75, 0.95, pClose)) * 0.5;

                        // Dot extinguishes completely (0.95 -> 1.0)
                        float dotAlpha = 1.0 - smoothstep(0.95, 1.0, pClose);

                        bool outsideV = distY >= openHeight;
                        bool outsideH = (pClose >= 0.75) && (distX >= openWidth);

                        if (outsideV || outsideH)
                        {
                            // Phosphor border glow around collapsing beam
                            float distFromBorderV = max(0.0, distY - openHeight);
                            float distFromBorderH = (pClose >= 0.75) ? max(0.0, distX - openWidth) : 0.0;
                            float borderGlow = smoothstep(0.015, 0.0, distFromBorderV + distFromBorderH);

                            float3 glowCol = _BeamColor.rgb * borderGlow * 2.2 * dotAlpha;
                            return fixed4(glowCol, 1.0);
                        }
                        else
                        {
                            // Inside collapsing aperture: rapid fade to black
                            float innerFade = smoothstep(0.0, 0.70, pClose);
                            return fixed4(0, 0, 0, innerFade * 0.9);
                        }
                    }
                }
                // --- PHASE 1: TRANSITION IN (CCTV Signal Lock & Smooth Phosphor Dissolve) ---
                else
                {
                    // 1. Electron beam ignition flash in center (t: 0.0 -> 0.18)
                    float beamFlash = 0.0;
                    if (t < 0.18)
                    {
                        float pBeam = t / 0.18;
                        float beamHeight = 0.006 + pBeam * 0.05;
                        float distCenterY = abs(p.y);
                        float beamGlow = smoothstep(beamHeight, 0.0, distCenterY) * (1.0 - pBeam);
                        beamFlash = beamGlow * 2.0;
                    }

                    // 2. Smooth black mask fade-out with ease-out (1.0 -> 0.0)
                    float maskAlpha = pow(1.0 - t, 1.8);

                    // 3. CCTV Static snow clearing out (starts active, smoothly dissolves by t = 0.50)
                    float staticIntensity = max(0.0, 1.0 - t / 0.50);
                    float lineRnd = Hash21(float2(floor(uv.y * 140.0), floor(_Time.y * 50.0)));
                    float isNoiseLine = step(0.70 + t * 0.5, lineRnd);
                    float grain = Hash21(float2(floor(uv.x * 260.0), floor(uv.y * 160.0)) + _Time.y * 45.0);

                    // Combine colors: beam ignition + static noise + phosphor settle
                    float3 col = _BeamColor.rgb * beamFlash;
                    if (isNoiseLine > 0.5 && staticIntensity > 0.01)
                    {
                        col += _StaticColor.rgb * (grain * 0.65 + 0.25) * staticIntensity;
                    }

                    // Subtle phosphor warm-up bloom that peaks at t=0.25 and fades
                    float phosphorBloom = sin(saturate(t * 1.5) * 3.14159) * (1.0 - t) * 0.12;
                    col += _BeamColor.rgb * phosphorBloom;

                    float finalAlpha = saturate(maskAlpha + beamFlash * 0.4);
                    return fixed4(col, finalAlpha);
                }
            }
            ENDCG
        }
    }
}
