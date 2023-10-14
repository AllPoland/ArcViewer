Shader "Custom/Bloomfog/BoxBlurShader"
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
            Blend SrcAlpha OneMinusSrcAlpha

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

            uniform float _BlurAlpha;
            uniform float _Offset;

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

                for(int x = -2; x <= 2; x++)
                {
                    for(int y = -2; y <= 2; y++)
                    {
                        col.rgb += tex2D(_MainTex, input.uv + float2(x, y) * res).rgb;
                    }
                }

                col.rgb /= 25;

                col.a = _BlurAlpha;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}