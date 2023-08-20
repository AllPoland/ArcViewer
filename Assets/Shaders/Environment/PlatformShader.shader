Shader "Custom/PlatformShader"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _FogStartOffset ("Fog Start Offset", float) = 0
        _FogScale ("Fog Scale", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                BLOOM_FOG_COORDS(1, 2)
                float4 vertex : SV_POSITION;
            };

            fixed4 _BaseColor;
            float _FogStartOffset;
            float _FogScale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                BLOOM_FOG_INITIALIZE(o, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _BaseColor;

                BLOOM_FOG_APPLY(i, col, _FogStartOffset, _FogScale);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
