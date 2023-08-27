Shader "Custom/Bloomfog/PrepassShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FadeStart ("Fade Start", float) = 0.2
        _FadeEnd ("Fade End", float) = 0.1
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _MainTex_ST;
            float _FadeStart, _FadeEnd;
            float _Offset;

            float _Threshold;
            float _BrightnessMult;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            struct Input
            {
                float2 uv_MainTex;
            };

            float remap(float2 input, float2 minMax, float2 outMinMax)
            {
                float value = max(input.x, input.y);
                return outMinMax.x + (value - minMax.x) * (outMinMax.y - outMinMax.x) / (minMax.y - minMax.x);
            }

            fixed4 getPixelColor(float2 uv)
            {
                float t = _Threshold;
                float m = _BrightnessMult;

                fixed4 col;
                col.rgb = tex2D(_MainTex, uv).rgb;

                fixed maximum = max(col.r, col.g);
                maximum = max(maximum, col.b);

                col.rgb *= (maximum > t) * m;

                float2 distFromCenter = abs((uv - float2(0.5, 0.5)) * 2);
                float fadeoutAmount = remap(distFromCenter, float2(1.0 - _FadeStart, 1.0 - _FadeEnd), float2(0.0, 1.0));
                col.rgb = lerp(col.rgb, float3(0, 0, 0), clamp(fadeoutAmount, 0, 1));

                return col;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                //Apply blur in prepass to save on a blit
                float2 res = _MainTex_TexelSize.xy;
                float i = _Offset;

                fixed4 col;
                col.rgb = getPixelColor(input.uv).rgb;
                col.rgb += getPixelColor(input.uv + float2( i, i ) * res).rgb;
                col.rgb += getPixelColor(input.uv + float2( i, -i ) * res).rgb;
                col.rgb += getPixelColor(input.uv + float2( -i, i ) * res).rgb;
                col.rgb += getPixelColor(input.uv + float2( -i, -i ) * res).rgb;
                col.rgb /= 5.0f;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}