Shader "Hidden/SoftBodyCrowd/BoidsRender"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		Cull Back

		CGPROGRAM
		#pragma surface surf Standard vertex:vert addshadow
		#pragma instancing_options procedural:setup

		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
			float3 color;
		};


		sampler2D _MainTex;

		sampler2D _PositionBuffer;
		float4    _PositionBuffer_TexelSize;
		sampler2D _NormalBuffer;
		float4    _NormalBuffer_TexelSize;
		sampler2D _TangentBuffer;
		float4    _TangentBuffer_TexelSize;
		sampler2D _BinormalBuffer;
		float4    _BinormalBuffer_TexelSize;

		int _TrailHistory;

		float _PathScale;
		float _PathOffset;
		float _Thickness;

		half   _Glossiness;
		half   _Metallic;  
		fixed4 _Color;     

		void vert(inout appdata_full v)
		{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

			// convert axis
			v.vertex.xyz = v.vertex.xzy * float3(1,1,1);

			float xStep = _PositionBuffer_TexelSize.x;
			float yStep = _PositionBuffer_TexelSize.y;
			float xOffset = 0.5 * xStep;
			float yOffset = 0.5 * yStep;

			float2 coord;
			coord.x = _PathScale * v.vertex.z + _PathOffset + xOffset;
			coord.x = 1.0 - coord.x;
			coord.y = unity_InstanceID * yStep + yOffset;

			float3 C = tex2Dlod(_PositionBuffer, float4(coord, 0, 0)).xyz;
			float3 N = tex2Dlod(_NormalBuffer,   float4(coord, 0, 0)).xyz;
			float3 B = tex2Dlod(_BinormalBuffer, float4(coord, 0, 0)).xyz;

			float thickness = _Thickness;

			float3 spoke = float3(v.vertex.x, 0.0, v.vertex.y);

			float3 P = float3(
				spoke.x * B.x * thickness + spoke.z * N.x * thickness,
				spoke.x * B.y * thickness + spoke.z * N.y * thickness,
				spoke.x * B.z * thickness + spoke.z * N.z * thickness
				);
			float3 position = C + P;

			v.vertex = mul(unity_ObjectToWorld, float4(position.xyz, 1.0));
			v.color = fixed4(N.rgb, 1.0);
			//v.normal *= float3(-1, -1, -1);
			v.normal = -normalize(mul(unity_ObjectToWorld, normalize(v.normal + N)));
#endif
		}

		void setup()
		{
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
		}
		ENDCG
	}
	FallBack "Diffuse"
}