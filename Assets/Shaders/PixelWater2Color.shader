Shader "Bunker/PixelWater2Color"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WaterDarkColor ("Water Dark Color", Color) = (0.06, 0.06, 0.06, 1)
        _WaveColor ("Wave Highlight Color", Color) = (0.92, 0.92, 0.92, 1)
        _PixelsPerUnit ("Pixels Per Unit", Float) = 16
        _WaveFPS ("Wave Step FPS", Float) = 4
        _CellWidth ("Wave Cell Width (px)", Float) = 22
        _CellHeight ("Wave Cell Height (px)", Float) = 14
        _WaveCoverage ("Wave Coverage", Range(0.05, 0.6)) = 0.36
        _SwirlCellSize ("Ripple Cell Size (px)", Float) = 88
        _SwirlCoverage ("Ripple Spawn Chance", Range(0.0, 0.8)) = 0.30
        _SwirlRadius ("Ripple Max Radius (px)", Range(4.0, 14.0)) = 8.5
        _SwirlFPS ("Ripple Step FPS", Float) = 6
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float2 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _WaterDarkColor;
            fixed4 _WaveColor;
            float _PixelsPerUnit;
            float _WaveFPS;
            float _CellWidth;
            float _CellHeight;
            float _WaveCoverage;
            float _SwirlCellSize;
            float _SwirlCoverage;
            float _SwirlRadius;
            float _SwirlFPS;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex).xy;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                if (c.a <= 0.01)
                    return c;

                float2 px = floor(IN.worldPos * _PixelsPerUnit);
                float stepTime = floor(_Time.y * _WaveFPS);

                // 1) Sparse drifting 1-bit wavelets
                float driftX = floor(stepTime * 0.5);
                float2 cellSize = float2(max(_CellWidth, 12.0), max(_CellHeight, 10.0));
                float rowIdx = floor(px.y / cellSize.y);
                float stagger = (fmod(abs(rowIdx), 2.0) >= 1.0) ? floor(cellSize.x * 0.5) : 0.0;

                float2 shiftedPx = float2(px.x - driftX + stagger, px.y);
                float2 cellId = floor(shiftedPx / cellSize);
                float2 localPx = shiftedPx - cellId * cellSize;

                float rSpawn = hash21(cellId + 13.7);
                float rOffX = floor(hash21(cellId + 31.1) * (cellSize.x - 10.0)) + 2.0;
                float rOffY = floor(hash21(cellId + 71.9) * (cellSize.y - 4.0)) + 1.0;
                float rPhase = hash21(cellId + 97.3) * 16.0;
                float cycle = fmod(stepTime + floor(rPhase), 16.0);

                bool isWave = false;
                if (rSpawn < _WaveCoverage && cycle < 10.0)
                {
                    float dx = localPx.x - rOffX;
                    float dy = localPx.y - rOffY;
                    float halfWidth = (cycle < 2.0 || cycle >= 7.0) ? 1.0 :
                                      (cycle < 3.0 || cycle >= 6.0) ? 2.0 : 3.0;

                    if (cycle >= 3.0 && cycle <= 5.0)
                    {
                        if (abs(dy - 1.0) < 0.25 && abs(dx) <= 1.1) isWave = true;
                        if (abs(dy) < 0.25 && abs(dx) >= 1.9 && abs(dx) <= 3.1) isWave = true;
                    }
                    else
                    {
                        if (abs(dy) < 0.25 && abs(dx) <= halfWidth + 0.1) isWave = true;
                    }
                }

                // 2) Rare, temporary expanding swirling drop-ripples
                float sSize = max(_SwirlCellSize, 32.0);
                float sRow = floor(px.y / sSize);
                float sStagger = (fmod(abs(sRow), 2.0) >= 1.0) ? floor(sSize * 0.5) : 0.0;
                float2 sPx = float2(px.x + sStagger, px.y);
                float2 sCell = floor(sPx / sSize);
                float2 sLocal = sPx - sCell * sSize;

                float swirlStep = floor(_Time.y * _SwirlFPS);
                float cycleLen = 54.0;
                float activeLen = 15.0;
                float phaseOffset = floor(hash21(sCell + 19.3) * cycleLen);
                float totalStep = swirlStep + phaseOffset;
                float epoch = floor(totalStep / cycleLen);
                float ageStep = totalStep - epoch * cycleLen;

                // Re-roll position & existence each epoch so ripples appear in different spots
                float sSpawn = hash21(sCell + epoch * 17.1 + 211.3);
                if (sSpawn < _SwirlCoverage && ageStep < activeLen)
                {
                    float t = ageStep / (activeLen - 1.0); // 0..1 lifecycle
                    float margin = _SwirlRadius + 2.0;
                    float span = max(sSize - margin * 2.0, 1.0);
                    float cx = floor(hash21(sCell + epoch * 7.3 + 43.9) * span + margin);
                    float cy = floor(hash21(sCell + epoch * 11.7 + 89.7) * span + margin);

                    float2 d = sLocal - float2(cx, cy);
                    float r = length(d);

                    float outerR = lerp(1.5, _SwirlRadius, t);
                    float innerR = lerp(0.5, _SwirlRadius * 0.55, saturate((t - 0.18) / 0.82));

                    float swirlDir = (hash21(sCell + epoch * 3.1 + 157.1) > 0.5) ? 1.0 : -1.0;
                    float ang = atan2(d.y, d.x) / 6.2831853;
                    float rot = t * 0.75 * swirlDir;

                    // Swirling broken concentric arcs (drop-ripple + eddy rotation)
                    float arcMask = frac(ang * 2.0 + r * 0.08 * swirlDir - rot);
                    float arcFill = lerp(0.75, 0.32, t); // breaks into smaller arcs as it dissipates

                    bool onOuter = (abs(r - outerR) < 0.68) && (arcMask < arcFill);
                    bool onInner = (t > 0.22 && t < 0.85) && (abs(r - innerR) < 0.62) && (frac(arcMask + 0.5) < 0.62);

                    if (onOuter || onInner)
                        isWave = true;
                }

                c.rgb = isWave ? _WaveColor.rgb : _WaterDarkColor.rgb;
                return c;
            }
            ENDCG
        }
    }
}
