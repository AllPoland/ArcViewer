Shader "Custom/Bloomfog/BlurShader"
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

            float _Offset;

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
                float2 res = _MainTex_TexelSize.xy;
                float i = _Offset;

                fixed4 col;
                col.rgb = tex2D(_MainTex, input.uv).rgb;
                col.rgb += tex2D( _MainTex, input.uv + float2( i, i ) * res ).rgb;
                col.rgb += tex2D( _MainTex, input.uv + float2( i, -i ) * res ).rgb;
                col.rgb += tex2D( _MainTex, input.uv + float2( -i, i ) * res ).rgb;
                col.rgb += tex2D( _MainTex, input.uv + float2( -i, -i ) * res ).rgb;
                col.rgb /= 5.0f;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}