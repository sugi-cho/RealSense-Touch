Shader "Unlit/BlobVisualizer"
{
    Properties
    {
		_Prop("render props(1/tex.wh, cam.wh)", Vector) = (512,512,1,1.6)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
		Blend One One
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			#pragma target 4.5

            #include "UnityCG.cginc"

			struct labelData
			{
				float size;
				float2 pos;
			};
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

			half4 _Prop;
			StructuredBuffer<labelData> _LabelBuffer;

            v2f vert (appdata v, uint iid:SV_InstanceID)
            {
				//tekito-
				labelData ld = _LabelBuffer[iid];
				float2 pos = ld.pos * _Prop.xy * _Prop.wz * 2.0 - _Prop.wz;
				float size = ld.size * _Prop.z * _Prop.y*2;
				v.vertex.xy *= size;
				v.vertex.xy += pos;

				float4 wPos = v.vertex;

                v2f o;
                o.vertex = UnityWorldToClipPos(wPos);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				half d = dot(i.uv-0.5, i.uv-0.5) * 4;
				d *= d < 1 ? 1 : 0;
                return d;
            }
            ENDCG
        }
    }
}
