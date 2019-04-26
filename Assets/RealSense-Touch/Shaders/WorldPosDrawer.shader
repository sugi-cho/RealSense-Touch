Shader "Unlit/WorldPosDrawer"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100 Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
			#pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float3 wPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (float4 vertex : POSITION)
            {
				vertex.y *= -1;

                v2f o;
				o.wPos = mul(unity_ObjectToWorld, vertex).xyz;
                o.vertex = UnityObjectToClipPos(vertex);
                return o;
            }

			[maxvertexcount(3)]
			void geom(triangle  v2f i[3], inout TriangleStream<v2f> triStream) {
				v2f o;
				half3 w0 = i[0].wPos;
				half3 w1 = i[1].wPos;
				half3 w2 = i[2].wPos;

				half l = dot(w0 - w1, w0 - w1) + dot(w1 - w2, w1 - w2) + dot(w2 - w0, w2 - w0);
				if (l < .03) {
					triStream.Append(i[0]);
					triStream.Append(i[1]);
					triStream.Append(i[2]);
				}
			}

            half4 frag (v2f i) : SV_Target
            {
				return half4(i.wPos,1);
            }
            ENDCG
        }
    }
}
