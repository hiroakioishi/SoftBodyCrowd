Shader "Hidden/SoftBodyCrowd/BoidsTrailKernel"
{
	Properties
	{
	}
	CGINCLUDE
	#include "UnityCG.cginc"

	struct BoidData
	{
		float3 velocity;
		float3 position;
	};

	struct FragmentOutput
	{
		float4 normal   : SV_Target0;
		float4 binormal : SV_Target1;
	};

	StructuredBuffer<BoidData> _BoidDataBuffer;

	sampler2D _PositionBuffer;
	float4 _PositionBuffer_TexelSize;

	sampler2D _NormalBuffer;
	float4 _NormalBuffer_TexelSize;

	sampler2D _BinormalBuffer;
	float4 _BinormalBuffer_TexelSize;

	int _TrailNum;

	// Pass 0:
	float4 frag_updatePosition(v2f_img i) : SV_Target
	{
		float3 headPos = _BoidDataBuffer[int(i.uv.y * _TrailNum)].position.xyz;
		float4 newPos = (i.uv.x < _PositionBuffer_TexelSize.x) ? float4(headPos.xyz, 1.0) : tex2D(_PositionBuffer, float2(i.uv.x - _PositionBuffer_TexelSize.x, i.uv.y));
		return newPos;
	}

	// Pass 1: 
	FragmentOutput frag_reconstruct(v2f_img i)
	{
		FragmentOutput o = (FragmentOutput)0;

		if (i.uv.x < _PositionBuffer_TexelSize.x)
		{
			float3 p0 = tex2D(_PositionBuffer, float2(i.uv.x, i.uv.y)).xyz;
			float3 p1 = tex2D(_PositionBuffer, float2(i.uv.x + _PositionBuffer_TexelSize.x, i.uv.y)).xyz;

			float3 up = float3(0, 0, 1);

			float3 dir = normalize(p0 - p1);
			float3 right = normalize(cross(dir, up));
			float3 norm = cross(right, dir);

			o.normal   = float4(norm,  1.0);
			o.binormal = float4(right, 1.0);
		}
		else
		{
			o.normal   = tex2D(_NormalBuffer,   float2(i.uv.x - _NormalBuffer_TexelSize.x,   i.uv.y));
			o.binormal = tex2D(_BinormalBuffer, float2(i.uv.x - _BinormalBuffer_TexelSize.x, i.uv.y));
		}
		
		return o;
	}
	ENDCG

	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		// Pass 0: Update Position
		Pass
		{
			CGPROGRAM
			#pragma vertex   vert_img
			#pragma fragment frag_updatePosition
			ENDCG
		}

		// Pass 1: Reconstruct Vectors
		Pass
		{
			CGPROGRAM
			#pragma vertex   vert_img
			#pragma fragment frag_reconstruct		
			ENDCG
		}
	}
}
