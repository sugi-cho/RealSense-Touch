Shader "Hidden/ScreenDraw"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_DrawTex ("draw", 2D) = "black" {}
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
			
			sampler2D _MainTex, _DrawTex;

			half4 frag (v2f i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.uv);
				half4 d = saturate(tex2D(_DrawTex, i.uv).x);
				d.rgb = frac(half3(sin(_Time.y), cos(_Time.y), tan(_Time.y)));
				col.rgb = lerp(col.rgb, d.rgb, d.a*0.5);
				return col;
			}
			ENDCG
		}
	}
}
