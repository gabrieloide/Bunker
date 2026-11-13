Shader "Hidden/AlphaComposite"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _OverlayTex ("Overlay", 2D) = "black" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _OverlayTex;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv);
                fixed4 overCol = tex2D(_OverlayTex, i.uv);
                // UI Toolkit targetTexture uses premultiplied alpha
                fixed3 rgb = overCol.rgb + baseCol.rgb * (1.0 - overCol.a);
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
}
