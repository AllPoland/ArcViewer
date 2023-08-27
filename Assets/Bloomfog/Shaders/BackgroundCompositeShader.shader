Shader "Custom/Bloomfog/BackgroundCompositeShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _BrightnessMult;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //Average the color of all pixels on the texture
                //I would do this with loops but they're broken for some fucking reason
                //So we're just gonna deal with 64 separate lines of code
                fixed4 col = tex2D(_MainTex, float2(0.0625, 0.0625));
                col += tex2D(_MainTex, float2(0.1875, 0.0625));
                col += tex2D(_MainTex, float2(0.3125, 0.0625));
                col += tex2D(_MainTex, float2(0.4375, 0.0625));
                col += tex2D(_MainTex, float2(0.5625, 0.0625));
                col += tex2D(_MainTex, float2(0.6875, 0.0625));
                col += tex2D(_MainTex, float2(0.8125, 0.0625));
                col += tex2D(_MainTex, float2(0.9375, 0.0625));

                col += tex2D(_MainTex, float2(0.0625, 0.1875));
                col += tex2D(_MainTex, float2(0.1875, 0.1875));
                col += tex2D(_MainTex, float2(0.3125, 0.1875));
                col += tex2D(_MainTex, float2(0.4375, 0.1875));
                col += tex2D(_MainTex, float2(0.5625, 0.1875));
                col += tex2D(_MainTex, float2(0.6875, 0.1875));
                col += tex2D(_MainTex, float2(0.8125, 0.1875));
                col += tex2D(_MainTex, float2(0.9375, 0.1875));

                col += tex2D(_MainTex, float2(0.0625, 0.3125));
                col += tex2D(_MainTex, float2(0.1875, 0.3125));
                col += tex2D(_MainTex, float2(0.3125, 0.3125));
                col += tex2D(_MainTex, float2(0.4375, 0.3125));
                col += tex2D(_MainTex, float2(0.5625, 0.3125));
                col += tex2D(_MainTex, float2(0.6875, 0.3125));
                col += tex2D(_MainTex, float2(0.8125, 0.3125));
                col += tex2D(_MainTex, float2(0.9375, 0.3125));

                col += tex2D(_MainTex, float2(0.0625, 0.4375));
                col += tex2D(_MainTex, float2(0.1875, 0.4375));
                col += tex2D(_MainTex, float2(0.3125, 0.4375));
                col += tex2D(_MainTex, float2(0.4375, 0.4375));
                col += tex2D(_MainTex, float2(0.5625, 0.4375));
                col += tex2D(_MainTex, float2(0.6875, 0.4375));
                col += tex2D(_MainTex, float2(0.8125, 0.4375));
                col += tex2D(_MainTex, float2(0.9375, 0.4375));

                col += tex2D(_MainTex, float2(0.0625, 0.5625));
                col += tex2D(_MainTex, float2(0.1875, 0.5625));
                col += tex2D(_MainTex, float2(0.3125, 0.5625));
                col += tex2D(_MainTex, float2(0.4375, 0.5625));
                col += tex2D(_MainTex, float2(0.5625, 0.5625));
                col += tex2D(_MainTex, float2(0.6875, 0.5625));
                col += tex2D(_MainTex, float2(0.8125, 0.5625));
                col += tex2D(_MainTex, float2(0.9375, 0.5625));

                col += tex2D(_MainTex, float2(0.0625, 0.6875));
                col += tex2D(_MainTex, float2(0.1875, 0.6875));
                col += tex2D(_MainTex, float2(0.3125, 0.6875));
                col += tex2D(_MainTex, float2(0.4375, 0.6875));
                col += tex2D(_MainTex, float2(0.5625, 0.6875));
                col += tex2D(_MainTex, float2(0.6875, 0.6875));
                col += tex2D(_MainTex, float2(0.8125, 0.6875));
                col += tex2D(_MainTex, float2(0.9375, 0.6875));

                col += tex2D(_MainTex, float2(0.0625, 0.8125));
                col += tex2D(_MainTex, float2(0.1875, 0.8125));
                col += tex2D(_MainTex, float2(0.3125, 0.8125));
                col += tex2D(_MainTex, float2(0.4375, 0.8125));
                col += tex2D(_MainTex, float2(0.5625, 0.8125));
                col += tex2D(_MainTex, float2(0.6875, 0.8125));
                col += tex2D(_MainTex, float2(0.8125, 0.8125));
                col += tex2D(_MainTex, float2(0.9375, 0.8125));

                col += tex2D(_MainTex, float2(0.0625, 0.9375));
                col += tex2D(_MainTex, float2(0.1875, 0.9375));
                col += tex2D(_MainTex, float2(0.3125, 0.9375));
                col += tex2D(_MainTex, float2(0.4375, 0.9375));
                col += tex2D(_MainTex, float2(0.5625, 0.9375));
                col += tex2D(_MainTex, float2(0.6875, 0.9375));
                col += tex2D(_MainTex, float2(0.8125, 0.9375));
                col += tex2D(_MainTex, float2(0.9375, 0.9375));

                col /= 64;

                return col * _BrightnessMult;
            }
            ENDCG
        }
    }
}
