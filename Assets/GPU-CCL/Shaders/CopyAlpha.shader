Shader "Hidden/CopyAlpha"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            sampler2D _MainTex;
			half4 _MainTex_TexelSize;

            half frag (v2f i) : SV_Target
            {
				float duv = _MainTex_TexelSize.xy;
                half4 col = tex2D(_MainTex, i.uv);

				half4 c0 = tex2D(_MainTex, i.uv + float2(-duv.x, 0));
				half4 c1 = tex2D(_MainTex, i.uv + float2( duv.x, 0));
				half4 c2 = tex2D(_MainTex, i.uv + float2(0, -duv.x));
				half4 c3 = tex2D(_MainTex, i.uv + float2( 0, duv.x));

				half dx = length(c0.xyz - c1.xyz) * c0.a * c1.a;
				half dy = length(c2.xyz - c3.xyz) * c2.a * c3.a;

				return col.a * (dx < 0.1) * (dy < 0.1);
            }
            ENDCG
        }
    }
}
