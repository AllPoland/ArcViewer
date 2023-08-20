#ifndef BLOOM_FOG_CG_INCLUDED
#define BLOOM_FOG_CG_INCLUDED

uniform sampler2D _BloomfogTex;
uniform float _CustomFogOffset;
uniform float _CustomFogAttenuation;
uniform float _CustomFogHeightFogStartY;
uniform float _CustomFogHeightFogHeight;

inline float2 GetFogCoord(float4 clipPos) {
  float4 screenPos = ComputeScreenPos(clipPos);
  return screenPos.xy / screenPos.w;
}

inline float GetHeightFogIntensity(float3 worldPos, float fogHeightOffset, float fogHeightScale) {
  float heightFogIntensity = _CustomFogHeightFogHeight + _CustomFogHeightFogStartY;
  heightFogIntensity = ((worldPos.y * fogHeightScale) + fogHeightOffset) + -heightFogIntensity;
  heightFogIntensity = heightFogIntensity / _CustomFogHeightFogHeight;
  heightFogIntensity = clamp(heightFogIntensity, 0, 1);
  return ((-heightFogIntensity * 2) + 3) * (heightFogIntensity * heightFogIntensity);
}

inline float GetFogIntensity(float3 distance, float fogStartOffset, float fogScale) {
  float fogIntensity = max(dot(distance, distance) + -fogStartOffset, 0);
  fogIntensity = max((fogIntensity * fogScale) + -_CustomFogOffset, 0);
  fogIntensity = 1 / ((fogIntensity * _CustomFogAttenuation) + 1);
  return -fogIntensity;
}

#define BLOOM_FOG_COORDS(fogCoordIndex, worldPosIndex) \
  float2 fogCoord : TEXCOORD##fogCoordIndex; \
  float3 worldPos : TEXCOORD##worldPosIndex;

#define BLOOM_FOG_SURFACE_INPUT \
  float2 fogCoord; \
  float3 worldPos;

#define BLOOM_FOG_INITIALIZE(outputStruct, inputVertex) \
  outputStruct.fogCoord = GetFogCoord(UnityObjectToClipPos(inputVertex)); \
  outputStruct.worldPos = mul(unity_ObjectToWorld, inputVertex)

#define BLOOM_FOG_SAMPLE(fogData) \
  tex2D(_BloomfogTex, fogData.fogCoord)

#define BLOOM_FOG_APPLY(fogData, col, fogStartOffset, fogScale) \
  float3 fogDistance = fogData.worldPos + -_WorldSpaceCameraPos; \
  float4 fogCol = -float4(col.rgb, 1) + BLOOM_FOG_SAMPLE(fogData); \
  fogCol.a = -col.a; \
  col = col + ((GetFogIntensity(fogDistance, fogStartOffset, fogScale) + 1) * fogCol)

#define BLOOM_HEIGHT_FOG_APPLY(fogData, col, fogStartOffset, fogScale, fogHeightOffset, fogHeightScale) \
  float3 fogDistance = fogData.worldPos + -_WorldSpaceCameraPos; \
  float4 fogCol = -float4(col.rgb, 1) + BLOOM_FOG_SAMPLE(fogData); \
  fogCol.a = -col.a; \
  col = col + (((GetHeightFogIntensity(fogData.worldPos, fogHeightOffset, fogHeightScale) * GetFogIntensity(fogDistance, fogStartOffset, fogScale)) + 1) * fogCol)

#endif // BLOOM_FOG_CG_INCLUDED
