Shader "Custom/PlatformShader"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _MainTex ("Main Texture", 2D) = "white" {}
        _TextureDistance ("Max Texture Distance", float) = 100.0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalDistance ("Max Normal Map Distance", float) = 100.0
        _FogStartOffset ("Fog Start Offset", float) = 0
        _FogScale ("Fog Scale", float) = 1
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1
        _AmbientStrength ("Ambient Light", float) = 1
        _ReflectionStrength ("Reflection Strength", float) = 1
        _AmbientReflectionStrength ("Ambient Reflection", float) = 0.01
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
                BLOOM_FOG_COORDS(4, 5, 6);
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NormalMap;
            float _TextureDistance, _NormalDistance;

            fixed4 _BaseColor;
            float _FogStartOffset, _FogScale;
            float _FogHeightOffset, _FogHeightScale;
            float _AmbientStrength;
            float _ReflectionStrength, _AmbientReflectionStrength, _NormalWeight;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.normal = mul((float3x3)unity_ObjectToWorld, v.normal);
                o.tangent = mul((float3x3)unity_ObjectToWorld, v.tangent);

                float3 binormal = cross(v.normal, v.tangent.xyz);
                o.binormal = mul((float3x3)unity_ObjectToWorld, binormal);

                BLOOM_FOG_INITIALIZE_VERT(o, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                BLOOM_FOG_INITIALIZE_FRAG(i);

                fixed4 col = _BaseColor;

                float cameraDistance = distance(i.worldPos, _WorldSpaceCameraPos);

                //Read the normal map and convert to worldspace
                float3 tangentNormal = UnpackNormal(tex2D(_NormalMap, i.uv));
                tangentNormal = lerp(tangentNormal, float3(0, 0, 1), clamp(cameraDistance / _NormalDistance, 0.001, 1));

                float3x3 TBN = float3x3(normalize(i.tangent), normalize(i.binormal), normalize(i.normal));
                float3 worldNormal = mul(tangentNormal, TBN);

                //Use the viewspace normal to create fake environment reflections
                float2 originalFogCoord = (i.fogCoord * 2) - 1;
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal);

                //Push the fog sample pos in the direction of the normal
                //Multiply by distance from screen origin to effectively mirror the screen
                float2 screenReflectPos = originalFogCoord + (viewNormal.xy * length(originalFogCoord));
                screenReflectPos = (screenReflectPos + 1) / 2;

                //Scale reflections with how much the face points toward the camera
                //because they'd be reflecting backwards where there's no fog
                float reflectionMult = lerp(_ReflectionStrength, _AmbientReflectionStrength, abs(viewNormal.z));
                col += BLOOM_FOG_SAMPLE(screenReflectPos) * reflectionMult;

                //Apply albedo texture
                col = lerp(col * tex2D(_MainTex, i.uv), col, clamp(cameraDistance / _TextureDistance, 0.001, 1));

                //Apply ambient lighting based on the up/down facing of the normal
                fixed4 skyCol = unity_AmbientSky * clamp(worldNormal.y, 0, 1);
                fixed4 equatorCol = unity_AmbientEquator * (1 - abs(worldNormal.y));
                fixed4 groundCol = unity_AmbientGround * abs(clamp(worldNormal.y, -1, 0));
                col += (skyCol + equatorCol + groundCol) * _AmbientStrength;

                BLOOM_HEIGHT_FOG_APPLY(i, col, _FogStartOffset, _FogScale, _FogHeightOffset, _FogHeightScale);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
