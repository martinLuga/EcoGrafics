namespace ecografics
//
//  ShaderCommons.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

// ----------------------------------------------------------------------------------------------------
// SHADER-Strings
// Die Common-Deklarationen
// ----------------------------------------------------------------------------------------------------  
module ShaderCommons =

    let commonsLuna =
        "
            #define MaxLights 16

            struct Light
            {
                float3 Strength;
                float FalloffStart; // point/spot light only
                float3 Direction;   // directional/spot light only
                float FalloffEnd;   // point/spot light only
                float3 Position;    // point light only
                float SpotPower;    // spot light only
            };

            struct Material
            {
                float4 DiffuseAlbedo;
                float3 FresnelR0;
                float Shininess;
            };

            struct MaterialData
            {
                float4   DiffuseAlbedo;
                float3   FresnelR0;
                float    Roughness;
                float4x4 MatTransform;
                uint     DiffuseMapIndex;
                uint     MatPad0;
                uint     MatPad1;
                uint     MatPad2;
            };

            TextureCube gCubeMap : register(t0);

            // An array of textures, which is only supported in shader model 5.1+.  Unlike Texture2DArray, the textures
            // in this array can be different sizes and formats, making it more flexible than texture arrays.
            Texture2D gDiffuseMap[4] : register(t1);

            // Put in space1, so the texture array does not overlap with these resources.
            // The texture array will occupy registers t0, t1, ..., t3 in space0.
            StructuredBuffer<MaterialData> gMaterialData : register(t0, space1);


            SamplerState gsamPointWrap        : register(s0);
            SamplerState gsamPointClamp       : register(s1);
            SamplerState gsamLinearWrap       : register(s2);
            SamplerState gsamLinearClamp      : register(s3);
            SamplerState gsamAnisotropicWrap  : register(s4);
            SamplerState gsamAnisotropicClamp : register(s5);

            // Constant data that varies per frame.
            cbuffer cbPerObject : register(b0)
            {
                float4x4 gWorld;
                float4x4 gTexTransform;
                uint gMaterialIndex;
                uint gObjPad0;
                uint gObjPad1;
                uint gObjPad2;
            };

            // Constant data that varies per material.
            cbuffer cbPass : register(b1)
            {
                float4x4 gView;
                float4x4 gInvView;
                float4x4 gProj;
                float4x4 gInvProj;
                float4x4 gViewProj;
                float4x4 gInvViewProj;
                float3 gEyePosW;
                float cbPerObjectPad1;
                float2 gRenderTargetSize;
                float2 gInvRenderTargetSize;
                float gNearZ;
                float gFarZ;
                float gTotalTime;
                float gDeltaTime;
                float4 gAmbientLight;
                Light gLights[MaxLights];
            };
         "

