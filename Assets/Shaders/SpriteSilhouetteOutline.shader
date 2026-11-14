// Draws a dilated solid silhouette of a sprite so a SpriteRenderer placed behind
// the target sprite creates a crisp pixel-art outline around it.
Shader "Bunker/SpriteSilhouetteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width (texels)", Float) = 1.5
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
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _OutlineWidth;

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
                float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;

                float a = tex2D(_MainTex, i.uv).a;
                a = max(a, tex2D(_MainTex, i.uv + float2( offset.x,  0.0)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(-offset.x,  0.0)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2( 0.0,  offset.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2( 0.0, -offset.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2( offset.x,  offset.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(-offset.x,  offset.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2( offset.x, -offset.y)).a);
                a = max(a, tex2D(_MainTex, i.uv + float2(-offset.x, -offset.y)).a);

                float mask = step(0.1, a);
                clip(mask - 0.001);
                return fixed4(i.color.rgb, mask * i.color.a);
            }
            ENDCG
        }
    }
}
