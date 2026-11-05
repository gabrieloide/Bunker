// Pixel-art chevrons along a strip: uv.x = distance walked, uv.y = distance from the centre line (both in world units).
// They scroll towards the end of the route, so the player reads both the route and its direction.
Shader "Bunker/RouteLine"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _PixelsPerUnit ("Pixels Per Unit", Float) = 16
        _Spacing ("Chevron Spacing (pixels)", Float) = 8
        _Length ("Chevron Length (pixels)", Float) = 2
        _Speed ("Scroll Speed (pixels/sec)", Float) = 8
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

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Evaluate at art-pixel centres so it reads as pixel art
                float along = floor(i.uv.x * _PixelsPerUnit) + 0.5;
                float across = floor(abs(i.uv.y) * _PixelsPerUnit) + 0.5;
                // Edges trail behind the centre: a ">" whose tip points the way soldiers walk
                float s = frac((along + across - _Time.y * _Speed) / _Spacing);
                clip(_Length / _Spacing - s);
                return fixed4(_Color.rgb, _Color.a);
            }
            ENDCG
        }
    }
}
