Shader "Custom/Bloomfog/BloomfogOutputShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Blend One One

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
                float4 screenSpace : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _MainTex_ST;
            sampler2D _CameraDepthTexture;

            float _BrightnessMult;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenSpace = ComputeScreenPos(o.vertex);
                return o;
            }

            struct Input
            {
                float2 uv_MainTex;
            };

            fixed4 frag (v2f input) : SV_Target
            {
                float2 screenSpaceUV = input.screenSpace.xy / input.screenSpace.w;
                float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenSpaceUV));

                fixed4 col;
                col.rgb = tex2D(_MainTex, input.uv).rgb * _BrightnessMult;
                col.rgb *= depth;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}