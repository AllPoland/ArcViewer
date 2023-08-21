#ifndef BLOOM_FOG_CG_INCLUDED
#define BLOOM_FOG_CG_INCLUDED

uniform sampler2D _BloomfogTex;
uniform float _CustomFogOffset;
uniform float _CustomFogAttenuation;
uniform float _CustomFogHeightFogStartY;
uniform float _CustomFogHeightFogHeight;
uniform float2 _FogTextureToScreenRatio;

inline float2 GetFogCoord(float4 screenPos) {
  float2 uv = clamp(screenPos.xy / screenPos.w, 0, 1);
  return float2(
    (uv.x + -0.5) * _FogTextureToScreenRatio.x + 0.5,
    (uv.y + -0.5) * _FogTextureToScreenRatio.y + 0.5
  );
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

#define BLOOM_FOG_COORDS(screenPosIndex, fogCoordIndex, worldPosIndex) \
  float4 screenPos : TEXCOORD##screenPosIndex; \
  float2 fogCoord : TEXCOORD##fogCoordIndex; \
  float3 worldPos : TEXCOORD##worldPosIndex

#define BLOOM_FOG_SURFACE_INPUT \
  float4 screenPos; \
  float2 fogCoord; \
  float3 worldPos;

#define BLOOM_FOG_INITIALIZE_VERT(outputStruct, inputVertex) \
  outputStruct.screenPos = ComputeScreenPos(UnityObjectToClipPos(inputVertex)); \
  outputStruct.worldPos = mul(unity_ObjectToWorld, inputVertex)

#define BLOOM_FOG_INITIALIZE_FRAG(inoutStruct) \
  inoutStruct.fogCoord = GetFogCoord(inoutStruct.screenPos)

#define BLOOM_FOG_SAMPLE(fogCoord) \
  tex2D(_BloomfogTex, fogCoord)

#define BLOOM_FOG_APPLY(fogData, col, fogStartOffset, fogScale) \
  float3 fogDistance = fogData.worldPos + -_WorldSpaceCameraPos; \
  float4 fogCol = -float4(col.rgb, 1) + BLOOM_FOG_SAMPLE(fogData.fogCoord); \
  fogCol.a = -col.a; \
  col = col + ((GetFogIntensity(fogDistance, fogStartOffset, fogScale) + 1) * fogCol)

#define BLOOM_HEIGHT_FOG_APPLY(fogData, col, fogStartOffset, fogScale, fogHeightOffset, fogHeightScale) \
  float3 fogDistance = fogData.worldPos + -_WorldSpaceCameraPos; \
  float4 fogCol = -float4(col.rgb, 1) + BLOOM_FOG_SAMPLE(fogData.fogCoord); \
  fogCol.a = -col.a; \
  col = col + (((GetHeightFogIntensity(fogData.worldPos, fogHeightOffset, fogHeightScale) * GetFogIntensity(fogDistance, fogStartOffset, fogScale)) + 1) * fogCol)

#endif // BLOOM_FOG_CG_INCLUDED
