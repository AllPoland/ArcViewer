Shader "Custom/Bloomfog/BrightnessThresholdShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            float _Threshold;
            float _BrightnessMult;

            v2f vert (appdata v)
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

            fixed4 frag (v2f input) : SV_Target
            {
                float t = _Threshold;
                float m = _BrightnessMult;

                fixed4 col;
                col.rgb = tex2D(_MainTex, input.uv).rgb;

                fixed maximum = max(col.r, col.g);
                maximum = max(maximum, col.b);

                col.rgb *= (maximum > t) * m;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}