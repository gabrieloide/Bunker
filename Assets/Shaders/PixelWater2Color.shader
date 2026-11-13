Shader "Bunker/PixelWater2Color"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WaterDarkColor ("Water Dark Color", Color) = (0.07, 0.07, 0.07, 1)
        _WaveColor ("Wave Highlight Color", Color) = (0.88, 0.88, 0.88, 1)
        _DarkThreshold ("Dark Pixel Threshold", Range(0.05, 0.6)) = 0.25
        _PixelsPerUnit ("Pixels Per Unit", Float) = 16
        _WaveFPS ("Wave Step FPS", Float) = 4
        _WaveDensity ("Wave Threshold", Range(0.75, 0.99)) = 0.91
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _WaterDarkColor;
            fixed4 _WaveColor;
            float _DarkThreshold;
            float _PixelsPerUnit;
            float _WaveFPS;
            float _WaveDensity;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex).xy;
                return OUT;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                if (c.a <= 0.01)
                    return c;

                float lum = dot(c.rgb, float3(0.299, 0.587, 0.114));
                if (lum <= _DarkThreshold)
                {
                    float2 px = floor(IN.worldPos * _PixelsPerUnit);
                    float stepTime = floor(_Time.y * _WaveFPS);

                    // Horizontal 3-pixel wave dashes drifting across quantized pixel rows
                    float rowSeed = hash21(float2(0.0, px.y));
                    float dir = (fmod(px.y, 2.0) < 1.0) ? 1.0 : -1.0;
                    float shiftedX = px.x + stepTime * dir + floor(rowSeed * 16.0);
                    float dashCell = floor(shiftedX / 4.0);
                    float dashLocal = fmod(abs(shiftedX), 4.0);

                    float wave = hash21(float2(dashCell, px.y));
                    float shimmer = hash21(float2(dashCell + floor(stepTime * 0.5), px.y * 3.0));

                    bool isGlint = (wave > _WaveDensity) && (shimmer > 0.45) && (dashLocal < 2.0);
                    c.rgb = isGlint ? _WaveColor.rgb : _WaterDarkColor.rgb;
                }

                return c;
            }
            ENDCG
        }
    }
}
