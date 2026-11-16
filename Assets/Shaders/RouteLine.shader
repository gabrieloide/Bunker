// Pixel-art chevrons along a strip: uv.x = distance walked, uv.y = distance from the centre line (both in world units).
// They scroll towards the end of the route, so the player reads both the route and its direction.
// _Dashed = 1 draws a thin still dashed line instead: the road without a direction, for while both sides walk it.
Shader "Bunker/RouteLine"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _PixelsPerUnit ("Pixels Per Unit", Float) = 16
        _Spacing ("Chevron Spacing (pixels)", Float) = 8
        _Length ("Chevron Length (pixels)", Float) = 2
        _Speed ("Scroll Speed (pixels/sec)", Float) = 8
        _Dashed ("Dashed (0 = chevrons)", Float) = 0
        _DashSpacing ("Dash Spacing (pixels)", Float) = 6
        _DashLength ("Dash Length (pixels)", Float) = 3
        _DashHalfWidth ("Dash Half Width (pixels)", Float) = 1
        _SweepDistance ("Sweep Distance", Float) = 1000000
        _SweepByKey ("Sweep by UV1.x key (0 = uv.x)", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _PixelsPerUnit;
            float _Spacing;
            float _Length;
            float _Speed;
            float _Dashed;
            float _DashSpacing;
            float _DashLength;
            float _DashHalfWidth;
            float _SweepDistance;
            float _SweepByKey;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float2 uv1 : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float key : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.key = v.uv1.x;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Evaluate at art-pixel centres so it reads as pixel art
                float along = floor(i.uv.x * _PixelsPerUnit) + 0.5;
                float across = floor(abs(i.uv.y) * _PixelsPerUnit) + 0.5;
                float sweepKey = _SweepByKey > 0.5 ? i.key : i.uv.x;
                bool dashed = (sweepKey <= _SweepDistance) ? (_Dashed > 0.5) : (_Dashed <= 0.5);
                if (dashed)
                {
                    clip(_DashHalfWidth - across);
                    clip(_DashLength / _DashSpacing - frac(along / _DashSpacing));
                    return fixed4(_Color.rgb, _Color.a);
                }
                // Edges trail behind the centre: a ">" whose tip points the way soldiers walk
                float s = frac((along + across - _Time.y * _Speed) / _Spacing);
                clip(_Length / _Spacing - s);
                return fixed4(_Color.rgb, _Color.a);
            }
            ENDCG
        }
    }
}
