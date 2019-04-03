Shader "Hidden/LabelVisualizer"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_LabelTex ("labeled texture", 2D) = "black"{}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex,_LabelTex;

			float3 rand3(float2 seed) {
				float t = sin(seed.x + seed.y * 1e3);
				return float3(frac(t*1e4), frac(t*1e6), frac(t*1e5));
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv).r;
				col *= col;
				half label = tex2D(_LabelTex, i.uv);
				half3 overlay = rand3(label * 0.001);
				col.rgb = lerp(col.rgb, overlay, 0 < label);
				return col;
			}
			ENDCG
		}
	}
}
