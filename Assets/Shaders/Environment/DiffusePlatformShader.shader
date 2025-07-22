Shader "Custom/DiffusePlatformShader"
{
    Properties
    {
        [HDR]_LaserColor ("Laser Color", Color) = (0,0,0,0)
        _ColorMult ("Laser Color Multiplier", float) = 1
        _MainTex ("Main Texture", 2D) = "white" {}
        _TextureDistance ("Max Texture Distance", float) = 100.0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalDistance ("Max Normal Map Distance", float) = 100.0
        _SpecularMap ("Specular Map", 2D) = "white" {}
        _FogStartOffset ("Fog Start Offset", float) = 0
        _FogScale ("Fog Scale", float) = 1
        _FogHeightOffset ("Fog Height Offset", float) = 0
        _FogHeightScale ("Fog Height Scale", float) = 1
        _AmbientStrength ("Ambient Light", float) = 1
        _ReflectionStrength ("Reflection Strength", float) = 1
        _LightingStrength ("Lighting Brightness", float) = 1
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

            static const int MAX_LASERS = 15;
            static const int MAX_OCCLUSIONS = 3;

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

            //Laser data to calculate diffuse lighting
            uniform float4 _LaserColors[MAX_LASERS];
            uniform float4 _LaserOrigins[MAX_LASERS];
            uniform float4 _LaserDirections[MAX_LASERS];
            uniform float4 _LaserFalloffInfos[MAX_LASERS]; // halfLength; brightnessCap; falloff; falloffSteepness;

            //Data for fake ambient occlusion
            uniform float4 _OcclusionColors[MAX_OCCLUSIONS];
            uniform float4 _OcclusionOrigins[MAX_OCCLUSIONS];
            uniform float4 _OcclusionDirections[MAX_OCCLUSIONS];
            uniform float4 _OcclusionFalloffInfos[MAX_OCCLUSIONS]; // halfLength; brightnessCap; falloff; falloffSteepness;

            uniform float4 _BloomfogTex_TexelSize;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _NormalMap;
            float _TextureDistance, _NormalDistance;

            sampler2D _SpecularMap;
            float _SpecularMap_ST;

            fixed4 _LaserColor;
            float _ColorMult;
            float _FogStartOffset, _FogScale;
            float _FogHeightOffset, _FogHeightScale;
            float _AmbientStrength;
            float _ReflectionStrength;
            float _LightingStrength;

            float3 Project(float3 a, float3 b)
            {
                return b * dot(a, b) / dot(b, b);
            }

            fixed4 GetDiffuseLight(float3 v, float4 color, float4 origin, float4 direction, float4 falloffInfo)
            {
                if (color.a < 0.0001) return fixed4(0,0,0,0);

                //Project the pixel position onto the laser line
                float3 relativePos = v - origin.xyz;
                float3 projectedPos = Project(relativePos, direction.xyz);
                float projectedMagnitude = length(projectedPos);

                //Clamp the projected position onto the laser's length
                float projectedDistance = min(projectedMagnitude, falloffInfo.x);
                float3 lightPos = (projectedPos / projectedMagnitude) * projectedDistance;

                //Find the direction and distance from the pixel position to the point on the laser
                float3 relativeLightPos = lightPos - relativePos;
                float lightDistance = max(length(relativeLightPos), 0.001);
                float3 relativeLightDir = relativeLightPos / lightDistance;

                //Calculate brightness using only the distance
                float falloffFactor = lightDistance / falloffInfo.z;
                float lightAmount = 1.0 / (falloffInfo.y + (lightDistance / falloffInfo.w) + (falloffFactor * falloffFactor));
    
                return fixed4(color.rgb * lightAmount * color.a, 0);
            }

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
                fixed4 textureColor = tex2D(_MainTex, i.uv);

                //Alpha clipping
                clip(textureColor.a - 0.5);

                BLOOM_FOG_INITIALIZE_FRAG(i);

                float3 cameraOffset = _WorldSpaceCameraPos - i.worldPos;
                float cameraDistance = length(cameraOffset);

                float2 bloomfogRes = _BloomfogTex_TexelSize.xy;

                //Read the normal map and convert to worldspace
                float3 tangentNormal = UnpackNormal(tex2D(_NormalMap, i.uv));
                tangentNormal = lerp(tangentNormal, float3(0, 0, 1), clamp(cameraDistance / _NormalDistance, 0.001, 1));

                float3x3 TBN = float3x3(normalize(i.tangent), normalize(i.binormal), normalize(i.normal));
                float3 worldNormal = mul(tangentNormal, TBN);

                //Calculate diffuse lighting
                fixed4 col = fixed4(0,0,0,0);
                for(int li = 0; li < MAX_LASERS; li++)
                {
                    col += GetDiffuseLight(i.worldPos, _LaserColors[li], _LaserOrigins[li], _LaserDirections[li], _LaserFalloffInfos[li]);
                }

                //Calculate ambient occlusion by multiplying by the inverse of occlusion marker strength
                for(int oi = 0; oi < MAX_OCCLUSIONS; oi++)
                {
                    col *= 1.0 - saturate(GetDiffuseLight(i.worldPos, _OcclusionColors[oi], _OcclusionOrigins[oi], _OcclusionDirections[oi], _OcclusionFalloffInfos[oi]));
                }

                col *= _LightingStrength;

                //Use the viewspace normal to create fake environment reflections
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal);
                float3 cameraDir = cameraOffset / cameraDistance;

                //Convert coordinates to a pixel grid (to avoid aspect ratio issues)
                float2 originalFogCoord = (i.fogCoord * 2) - 1;
                originalFogCoord.y *= bloomfogRes;

                //Push the fog sample pos in the direction of the normal
                //Distance is based on the angle of reflection
                float fresnel = dot(cameraDir, worldNormal);
                float reflectionDist = fresnel * bloomfogRes.x * 0.5;
                float2 screenReflectPos = originalFogCoord + (viewNormal.xy * reflectionDist);

                //Convert back to UV coordinates to sample the bloomfog
                screenReflectPos.y /= bloomfogRes;
                float2 screenReflectUV = (screenReflectPos + 1) * 0.5;

                //Scale reflections with a fresnel effect for more convincing specularity
                float reflectionMult = _ReflectionStrength * tex2D(_SpecularMap, i.uv) * (1.0 - fresnel);

                //Add specular reflections to color
                col += BLOOM_FOG_SAMPLE(screenReflectUV) * reflectionMult;

                //Apply ambient lighting based on the up/down facing of the normal
                fixed4 skyCol = unity_AmbientSky * clamp(worldNormal.y, 0, 1);
                fixed4 equatorCol = unity_AmbientEquator * (1 - abs(worldNormal.y));
                fixed4 groundCol = unity_AmbientGround * abs(clamp(worldNormal.y, -1, 0));
                col += (skyCol + equatorCol + groundCol) * _AmbientStrength;

                //Apply albedo texture
                col = lerp(col * textureColor, col, clamp(cameraDistance / _TextureDistance, 0.001, 1));

                //Add the laser glow
                col += fixed4(_LaserColor.rgb, 0) * _LaserColor.a * _ColorMult;

                //Apply bloomfog
                BLOOM_HEIGHT_FOG_APPLY(i, col, _FogStartOffset, _FogScale, _FogHeightOffset, _FogHeightScale);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
