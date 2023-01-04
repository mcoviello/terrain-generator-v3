Shader "Unlit/Wireframe"
{
    //Shader from https://blog.logrocket.com/building-wireframe-shader-unity/
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WireframeColor("Wireframe Color", color) = (1,1,1,1)
        _WireframeWidth("Wireframe Width", float) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
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

            struct g2f
            {
                float4 pos: SV_POSITION;
                float3 barycentric: TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _WireframeColor;
            float _WireframeWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }

            [maxvertexcount(3)]
            void geom(triangle v2f IN[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;
                o.pos = IN[0].vertex;
                o.barycentric = float3(1,0,0);
                triStream.Append(o);
                o.pos = IN[1].vertex;
                o.barycentric = float3(0,1,0);
                triStream.Append(o);
                o.pos = IN[2].vertex;
                o.barycentric = float3(0,0,1);
                triStream.Append(o);
            }

            fixed4 frag(g2f i) : SV_Target
            {

                float closest = min(i.barycentric.x, min(i.barycentric.y, i.barycentric.z));
                float alpha = step(closest, _WireframeWidth);
                return fixed4(_WireframeColor.r, _WireframeColor.g, _WireframeColor.b, alpha);
            }
            ENDCG
        }
    }
}
