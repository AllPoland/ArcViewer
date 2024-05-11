Shader "Custom/LaserGlowShader"
{
    Properties
    {
        [HDR]_LaserColor ("Color", Color) = (1,1,1,1)
        _ColorMult ("Color Multiplier", float) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType"="Transparent"
        }
        
        LOD 100
        Blend SrcAlpha One
        ZWrite Off

        pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "BloomFog.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _LaserColor;
            float _ColorMult;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _LaserColor * _ColorMult;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}