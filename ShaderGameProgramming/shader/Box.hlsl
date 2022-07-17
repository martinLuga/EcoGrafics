//=============================================================================
// Sky.fx by Frank Luna (C) 2011 All Rights Reserved.
//=============================================================================

// Include common HLSL code.
#include "Common.hlsl"

struct VertexOut
{
	float4 PosH : SV_POSITION;	// Position
	float3 PosL : POSITION;		// Normal     
}; 

VertexOut VSMain(VertexShaderInput vin)
{
	VertexOut vout;

	vout.PosL = vin.Position.xyz;

	vout.PosH = mul(float4(vin.Position.xyz, 1.0f), World);

	// Always center sky about camera.
	vout.PosH.xyz += CameraPosition;

	// vout.PosH = SV_POSITION
	vout.PosH = mul(vout.PosH, ViewProjection);

	return vout;
}

float4 PSMain(VertexOut pin) : SV_Target
{
	return gCubeMap.Sample(gsamLinearWrap, pin.PosL.xyz);
} 