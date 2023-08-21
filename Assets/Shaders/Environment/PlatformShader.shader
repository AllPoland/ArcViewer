Shader "Custom/PlatformShader"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _MainTex ("Main Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _FogStartOffset ("Fog Start Offset", float) = 0
        _FogScale ("Fog Scale", float) = 1
        _AmbientStrength ("Ambient Light", float) = 1
        _ReflectionStrength ("Reflection Strength", float) = 1
        _ReflectAngleStrength ("Reflcetion Angle Strength", float) = 1
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
                float3 normal : NORMAL;
                float3 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 tangent : TEXCOORD2;
                float3 binormal : TEXCOORD3;
                BLOOM_FOG_COORDS(4, 5)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NormalMap;
            fixed4 _BaseColor;
            float _FogStartOffset, _FogScale;
            float _AmbientStrength;
            float _ReflectionStrength, _ReflectAngleStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.normal = mul((float3x3)unity_ObjectToWorld, v.normal);
                o.tangent = mul((float3x3)unity_ObjectToWorld, v.tangent);

                float3 binormal = cross(v.normal, v.tangent.xyz);
                o.binormal = mul((float3x3)unity_ObjectToWorld, binormal);

                BLOOM_FOG_INITIALIZE(o, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _BaseColor * tex2D(_MainTex, i.uv);

                float3 tangentNormal = UnpackNormal(tex2D(_NormalMap, i.uv));

                float3x3 TBN = float3x3(normalize(i.tangent), normalize(i.binormal), normalize(i.normal));

                float3 worldNormal = normalize(mul(tangentNormal, TBN));

                fixed4 skyCol = unity_AmbientSky * clamp(worldNormal.y, 0, 1);
                fixed4 equatorCol = unity_AmbientEquator * (1 - abs(worldNormal.y));
                fixed4 groundCol = unity_AmbientGround * abs(clamp(worldNormal.y, -1, 0));
                col += (skyCol + equatorCol + groundCol) * _AmbientStrength;

                float3 worldSpaceViewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float3 reflectionDir = reflect(worldSpaceViewDir, worldNormal);

                float2 reflectionSamplePos = GetFogCoord(i.vertex + float4(reflectionDir, 1) * _ReflectAngleStrength);

                BLOOM_FOG_APPLY(i, col, _FogStartOffset, _FogScale);
                // col.rgb = worldNormal.yyy;
                // col = BLOOM_FOG_SAMPLE(reflectionSamplePos) * _ReflectionStrength;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
