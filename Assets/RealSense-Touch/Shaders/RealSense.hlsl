#include "UnityCG.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    uint vIdx : SV_VertexID;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float depth : TEXCOORD1;
};

StructuredBuffer<float3> _VertBuffer;
float _Size;
			
v2f vert(appdata v)
{
    v2f o = (v2f) 0;
    o.vertex.xyz = _VertBuffer[v.vIdx]; 

    return o;
}

[maxvertexcount(4)]
void geom(point v2f input[1], inout TriangleStream<v2f> triStream)
{
    v2f o = input[0];
    o.vertex.y *= -1.0;
    float size = _Size * saturate(length(o.vertex.xyz));
    float3 center = UnityObjectToViewPos(o.vertex.xyz);
    o.depth = abs(center.z);
    
    for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        {
            float3 pos = center + float3(x - 0.5, y - 0.5, 0.0) * size;
            o.vertex = UnityViewToClipPos(pos);
            o.uv = float2(x, y);
            triStream.Append(o);
        }
    triStream.RestartStrip();
}
		
half4 frag(v2f i) : SV_Target
{
    return 1;
}