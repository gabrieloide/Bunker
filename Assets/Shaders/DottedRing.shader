// Pixel-art dotted circle drawn on a quad: radius in world units, dots sized in art pixels.
// Distances are measured from the object's origin, so batching must stay off.
Shader "Bunker/DottedRing"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Radius ("Radius (world units)", Float) = 3
        _PixelsPerUnit ("Pixels Per Unit", Float) = 16
        _DotSpacing ("Dot Spacing (pixels)", Float) = 4
        _DotLength ("Dot Length (pixels)", Float) = 2
        _Thickness ("Thickness (pixels)", Float) = 1
        _Speed ("Scroll Speed (pixels/sec)", Float) = 4
<<<<<<< HEAD
=======
        _MergeWithFlags ("Merge With Flags", Float) = 0
>>>>>>> 1b0f21870329b922747c317aa3561b86f80f1c88
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "DisableBatching" = "True" "PreviewType" = "Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Radius;
            float _PixelsPerUnit;
            float _DotSpacing;
            float _DotLength;
            float _Thickness;
            float _Speed;
<<<<<<< HEAD
=======
            float _MergeWithFlags;
            // Set globally by FlagTerritory: xy = centre, z = radius (world units)
            float4 _FlagCircles[32];
            float _FlagCircleCount;
>>>>>>> 1b0f21870329b922747c317aa3561b86f80f1c88

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 local : TEXCOORD0;
<<<<<<< HEAD
=======
                float2 center : TEXCOORD1;
>>>>>>> 1b0f21870329b922747c317aa3561b86f80f1c88
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 center = unity_ObjectToWorld._m03_m13_m23;
                o.local = (world - center).xy;
<<<<<<< HEAD
=======
                o.center = center.xy;
>>>>>>> 1b0f21870329b922747c317aa3561b86f80f1c88
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Evaluate at art-pixel centres so the ring reads as pixel art, not a smooth vector circle
                float2 px = floor(i.local * _PixelsPerUnit) + 0.5;
                float r = _Radius * _PixelsPerUnit;
                float ring = step(abs(length(px) - r), _Thickness * 0.5);

                // Whole number of dots so there is no seam where the angle wraps around
                float circumference = 6.2831853 * r;
                float dots = max(1.0, round(circumference / _DotSpacing));
                float spacing = circumference / dots;
                float s = (atan2(px.y, px.x) / 6.2831853 + 0.5) * dots - _Time.y * _Speed / spacing;
                float dotMask = step(frac(s), _DotLength / spacing);

                float a = ring * dotMask * _Color.a;
<<<<<<< HEAD
=======

                // Flag rings skip what lies inside another flag, so overlapping flags draw a single outline
                if (_MergeWithFlags > 0.5 && a > 0.0)
                {
                    float2 worldPx = i.center * _PixelsPerUnit + px;
                    for (int k = 0; k < 32; k++)
                    {
                        if (k >= (int)_FlagCircleCount) break;
                        float4 c = _FlagCircles[k];
                        bool self = distance(c.xy, i.center) < 0.01 && abs(c.z - _Radius) < 0.01;
                        if (!self && length(worldPx - c.xy * _PixelsPerUnit) < c.z * _PixelsPerUnit - 0.5)
                            a = 0.0;
                    }
                }
>>>>>>> 1b0f21870329b922747c317aa3561b86f80f1c88
                clip(a - 0.001);
                return fixed4(_Color.rgb, a);
            }
            ENDCG
        }
    }
}
