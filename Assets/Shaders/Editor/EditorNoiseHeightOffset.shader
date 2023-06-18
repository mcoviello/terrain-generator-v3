Shader "Unlit/EditorNoiseHeightOffset"
{
    Properties
    {
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _MaxHeight ("Max Height", float) = 100
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

            sampler2D _NoiseTex;
            float _MaxHeight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float noiseVal = tex2D(_NoiseTex, i.uv);
                i.vertex.y = noiseVal * _MaxHeight;
                return float4(noiseVal, noiseVal, noiseVal, 1);
            }
            ENDCG
        }
    }
}
