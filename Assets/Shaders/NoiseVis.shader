Shader "Unlit/NoiseVis"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseVisPow("NoiseVisPow", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _NoiseVisPow;

            float DecodeRGBAToFloat(float4 enc) 
            {
                uint ex = (enc.x * 255);
                uint ey = (enc.y * 255);
                uint ez = (enc.z * 255);
                uint ew = (enc.w * 255);
                uint v = (ex << 24) + (ey << 16) + (ez << 8) + ew;
                return v / (256.0f * 256.0f * 256.0f * 256.0f);
            }

            float DecodeFloatRGBATest( float4 enc )
            {
                float4 kDecodeDot = float4(1.0, 1/255.0, 1/65025.0, 1/160581375.0);
                return dot( enc.gbar, kDecodeDot );
            }

            v2f vert (appdata v)
            {
                v2f o;
                float4 height = tex2Dlod(_MainTex, float4(v.uv.xy,0,0));
                //v.vertex.y = DecodeFloatRGBATest(height) * 100;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 col = tex2D(_MainTex, i.uv);
                float noiseVal = DecodeFloatRGBATest(col.argb);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return float4(noiseVal, noiseVal, noiseVal, 1);
            }
            ENDCG
        }
    }
}
