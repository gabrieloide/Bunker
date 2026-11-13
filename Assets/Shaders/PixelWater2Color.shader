Shader "Bunker/PixelWater2Color"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WaterDarkColor ("Water Dark Color", Color) = (0.06, 0.06, 0.06, 1)
        _WaveColor ("Wave Highlight Color", Color) = (0.92, 0.92, 0.92, 1)
        _PixelsPerUnit ("Pixels Per Unit", Float) = 16
        _WaveFPS ("Wave Step FPS", Float) = 3
        _CellWidth ("Wave Cell Width (px)", Float) = 22
        _CellHeight ("Wave Cell Height (px)", Float) = 14
        _WaveCoverage ("Wave Coverage", Range(0.05, 0.6)) = 0.34
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

                // Quantize to world-space pixel grid so ripples stay crisp and pixel-locked
                float2 px = floor(IN.worldPos * _PixelsPerUnit);
                float stepTime = floor(_Time.y * _WaveFPS);

                // Gentle unified current drift (1px every few steps)
                float driftX = floor(stepTime * 0.5);
                float2 cellSize = float2(max(_CellWidth, 12.0), max(_CellHeight, 10.0));

                // Stagger odd rows horizontally by half a cell to avoid grid alignment
                float rowIdx = floor(px.y / cellSize.y);
                float stagger = (fmod(abs(rowIdx), 2.0) >= 1.0) ? floor(cellSize.x * 0.5) : 0.0;

                float2 shiftedPx = float2(px.x - driftX + stagger, px.y);
                float2 cellId = floor(shiftedPx / cellSize);
                float2 localPx = shiftedPx - cellId * cellSize;

                // Per-cell random offset & slow lifecycle phase
                float rSpawn = hash21(cellId + 13.7);
                float rOffX = floor(hash21(cellId + 31.1) * (cellSize.x - 10.0)) + 2.0;
                float rOffY = floor(hash21(cellId + 71.9) * (cellSize.y - 4.0)) + 1.0;
                float rPhase = hash21(cellId + 97.3) * 16.0;

                // Slow 16-step wave lifecycle: calm -> small -> crest -> small -> calm
                float cycle = fmod(stepTime + floor(rPhase), 16.0);

                bool isWave = false;
                if (rSpawn < _WaveCoverage && cycle < 10.0)
                {
                    float dx = localPx.x - rOffX;
                    float dy = localPx.y - rOffY;

                    // Lifecycle width: 3px -> 5px -> 7px -> 5px -> 3px
                    float halfWidth = (cycle < 2.0 || cycle >= 7.0) ? 1.0 :
                                      (cycle < 3.0 || cycle >= 6.0) ? 2.0 : 3.0;

                    // Subtle 1-bit pixel wavelet shape: gentle arc
                    // Crest center rises 1px during peak frames
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

                c.rgb = isWave ? _WaveColor.rgb : _WaterDarkColor.rgb;
                return c;
            }
            ENDCG
        }
    }
}
