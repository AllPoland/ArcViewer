Shader "Custom/PlatformShader"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _MainTex ("Main Texture", 2D) = "white" {}
        _FogStartOffset ("Fog Start Offset", float) = 0
        _FogScale ("Fog Scale", float) = 1
        _AmbientStrength ("Ambient Light", float) = 1
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                BLOOM_FOG_COORDS(2, 3)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _BaseColor;
            float _FogStartOffset, _FogScale;
            float _AmbientStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = normalize(v.normal);

                BLOOM_FOG_INITIALIZE(o, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _BaseColor * tex2D(_MainTex, i.uv);

                fixed4 skyCol = unity_AmbientSky * clamp(i.normal.y, 0, 1);
                fixed4 equatorCol = unity_AmbientEquator * (1 - abs(i.normal.y));
                fixed4 groundCol = unity_AmbientGround * abs(clamp(i.normal.y, -1, 0));
                col += (skyCol + equatorCol + groundCol) * _AmbientStrength;

                // float3 worldSpaceViewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);

                BLOOM_FOG_APPLY(i, col, _FogStartOffset, _FogScale);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
