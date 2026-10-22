Shader "Bunker/CRTTurnOn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress ("Turn-On Progress", Range(0, 1)) = 0.0
        _BeamColor ("Beam Phosphor Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Curvature ("CRT Curvature", Range(0, 0.15)) = 0.03
        _FlashIntensity ("Flash Intensity", Range(0, 2)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
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
            float _Progress;
            float _Curvature;
            float _FlashIntensity;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float2 CurveUV(float2 uv, float curv)
            {
                if (curv <= 0.001) return uv;
                float2 centered = uv * 2.0 - 1.0;
                float2 offset = centered.yx / (curv * 20.0 + 8.0);
                centered += centered * offset * offset;
                return centered * 0.5 + 0.5;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = saturate(_Progress);

                // Fully turned on: 100% transparent
                if (t >= 0.999)
                {
                    return fixed4(0, 0, 0, 0);
                }

                float2 uv = CurveUV(i.texcoord, _Curvature);

                // If outside curved CRT glass, render solid bezel black
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return fixed4(0.01, 0.01, 0.01, 1.0);
                }

                float2 p = uv - 0.5;

                // --- PHASE 1: Horizontal Electron Beam Ignition (0.0 -> 0.25) ---
                if (t < 0.25)
                {
                    float p1 = smoothstep(0.02, 0.25, t);
                    float halfWidth = p1 * 0.5;

                    float distY = abs(p.y);
                    float distX = abs(p.x);

                    // Very thin horizontal laser / beam line
                    float beamThickness = 0.003;
                    float glowSpread = 0.015;

                    if (distX <= halfWidth && distY < glowSpread * 2.0)
                    {
                        // Beam intensity falls off towards ends and vertically
                        float horizFade = 1.0 - smoothstep(halfWidth * 0.7, halfWidth, distX);
                        float vertGlow = exp(-distY * 180.0);
                        float core = smoothstep(beamThickness, 0.0, distY) * 2.0;

                        float intensity = (core + vertGlow) * horizFade * _FlashIntensity;
                        float3 beamCol = _BeamColor.rgb * intensity;

                        return fixed4(beamCol, 1.0);
                    }

                    // Otherwise solid black
                    return fixed4(0, 0, 0, 1.0);
                }

                // --- PHASE 2: Vertical Slit Expansion (0.25 -> 0.85) ---
                if (t < 0.85)
                {
                    float p2 = smoothstep(0.25, 0.85, t);
                    // Non-linear explosive snap opening typical of CRT deflection coils
                    float openHeight = (pow(p2, 1.8)) * 0.5;

                    float distY = abs(p.y);

                    // INSIDE THE OPENING SLIT:
                    if (distY < openHeight)
                    {
                        // Distance to the moving border edge
                        float distToBorder = openHeight - distY;

                        // Hot cathode glow right near the moving edge
                        float edgeGlow = exp(-distToBorder * 90.0) * (1.0 - p2 * 0.7);

                        // Initial white phosphor flash that fades as it opens
                        float flash = (1.0 - p2) * 0.45 * _FlashIntensity;

                        float alpha = saturate(edgeGlow + flash);
                        float3 col = _BeamColor.rgb * (edgeGlow * 1.5 + flash);

                        return fixed4(col, alpha);
                    }
                    // OUTSIDE THE SLIT: Pure Black Mask
                    else
                    {
                        // Hot beam on the outside boundary
                        float distFromBorder = distY - openHeight;
                        float borderLine = smoothstep(0.01, 0.0, distFromBorder);

                        float3 col = _BeamColor.rgb * borderLine * 2.0;
                        return fixed4(col, 1.0);
                    }
                }

                // --- PHASE 3: Power Settle & Micro-Flicker (0.85 -> 1.0) ---
                float p3 = (t - 0.85) / 0.15; // 0..1

                // 2 micro-flickers (voltage settling)
                float flicker = sin(p3 * 3.14159 * 4.0);
                float flashAlpha = max(0.0, flicker) * (1.0 - p3) * 0.25 * _FlashIntensity;

                return fixed4(_BeamColor.rgb, flashAlpha);
            }
            ENDCG
        }
    }
}
