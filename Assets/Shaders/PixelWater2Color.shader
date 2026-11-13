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
        _WaveCoverage ("Wave Coverage", Range(0.05, 0.6)) = 0.38
        _SwirlCellSize ("Whirlpool Cell Size (px)", Float) = 52
        _SwirlCoverage ("Whirlpool Coverage", Range(0.0, 0.8)) = 0.35
        _SwirlRadius ("Whirlpool Radius (px)", Range(4.0, 12.0)) = 7.5
        _SwirlFPS ("Whirlpool Step FPS", Float) = 7
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

                // 2) Occasional small pixel whirlpools (remolinos)
                float sSize = max(_SwirlCellSize, 24.0);
                float sRow = floor(px.y / sSize);
                float sStagger = (fmod(abs(sRow), 2.0) >= 1.0) ? floor(sSize * 0.5) : 0.0;
                float2 sPx = float2(px.x + sStagger, px.y);
                float2 sCell = floor(sPx / sSize);
                float2 sLocal = sPx - sCell * sSize;

                float sSpawn = hash21(sCell + 211.3);
                if (sSpawn < _SwirlCoverage)
                {
                    float margin = _SwirlRadius + 2.0;
                    float span = max(sSize - margin * 2.0, 1.0);
                    float cx = floor(hash21(sCell + 43.9) * span + margin);
                    float cy = floor(hash21(sCell + 89.7) * span + margin);

                    float2 d = sLocal - float2(cx, cy);
                    float r = length(d);

                    if (r >= 1.1 && r <= _SwirlRadius)
                    {
                        float swirlDir = (hash21(sCell + 157.1) > 0.5) ? 1.0 : -1.0;
                        float swirlPhase = floor(_Time.y * _SwirlFPS) * 0.14 * swirlDir + hash21(sCell + 19.3);
                        float ang = atan2(d.y, d.x) / 6.2831853;

                        // Two crisp spiral arms tightening toward the center
                        float spiral = frac(ang * 2.0 + r * 0.23 - swirlPhase);
                        float armWidth = (r < _SwirlRadius * 0.65) ? 0.28 : 0.18;
                        if (spiral < armWidth)
                            isWave = true;
                    }
                }

                c.rgb = isWave ? _WaveColor.rgb : _WaterDarkColor.rgb;
                return c;
            }
            ENDCG
        }
    }
}
