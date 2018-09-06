Shader "Unlit/RealPointCloud"
{
	Properties
	{
		_MainTex ("texture", 2D) = "white"{}
		_Size ("particle size", Float) = 0.01
	}
		SubShader
		{
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
			ZWrite Off Blend SrcAlpha OneMinusSrcAlpha

			Pass
			{
				Tags{ "LightMode" = "ForwardBase" }
				CGPROGRAM
				#pragma vertex vert
				#pragma geometry geom
				#pragma fragment frag

				#include "RealSense.hlsl"
				ENDCG
			}
		}
}