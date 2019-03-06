Shader "Unlit/PointCloudBufferVis"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 5.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float val : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			StructuredBuffer<float4> _PCBuffer;

            v2f vert (appdata v, uint vIdx : SV_VertexID)
            {
				float4 pcData = _PCBuffer[vIdx];
				v.vertex.xyz = pcData.xyz;

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.val = pcData.w;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				return i.val;
            }
            ENDCG
        }
    }
}
