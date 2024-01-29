Shader "Custom/LaserShader"
{
    Properties
    {
        [HDR]_BaseColor ("Color", Color) = (1,1,1,1)
        _FogStartOffset ("Fog Start Offset", float) = 0
        _FogScale ("Fog Scale", float) = 1
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1
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
                float3 normal : NORMAL;
                float3 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                BLOOM_FOG_COORDS(0, 1, 2);
                float4 vertex : SV_POSITION;
            };

            fixed4 _BaseColor;
            float _FogStartOffset, _FogScale;
            float _FogHeightOffset, _FogHeightScale;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                BLOOM_FOG_INITIALIZE_VERT(o, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                BLOOM_FOG_INITIALIZE_FRAG(i);

                fixed4 col = _BaseColor;
                BLOOM_HEIGHT_FOG_APPLY(i, col, _FogStartOffset, _FogScale, _FogHeightOffset, _FogHeightScale);

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}