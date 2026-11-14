// Reveals a sprite progressively along an axis in discrete pixel steps for bridge unlock animations.
Shader "Bunker/SpriteFillReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _FillProgress ("Fill Progress", Range(0, 1)) = 1
        _FillAxis ("Fill Axis (X, Y)", Vector) = (1, 0, 0, 0)
        _FillSteps ("Discrete Steps", Float) = 13
        _UVBounds ("UV Bounds (MinU, MinV, SizeU, SizeV)", Vector) = (0, 0, 1, 1)
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

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _FillProgress;
            float4 _FillAxis;
            float _FillSteps;
            float4 _UVBounds;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 normUv = (i.uv - _UVBounds.xy) / max(_UVBounds.zw, float2(0.0001, 0.0001));
                float2 axis = normalize(max(abs(_FillAxis.xy), float2(0.0001, 0.0001))) * sign(_FillAxis.xy);
                float coord = saturate(dot(normUv, axis));

                float progress = _FillProgress;
                if (_FillSteps > 1.0 && progress > 0.0 && progress < 1.0)
                    progress = floor(progress * _FillSteps) / _FillSteps;

                clip(progress - coord - 0.0001);

                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                clip(c.a - 0.001);
                return c;
            }
            ENDCG
        }
    }
}
