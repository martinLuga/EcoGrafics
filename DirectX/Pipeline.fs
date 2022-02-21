namespace DirectX
//
//  ShaderSupport.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System
open System.Collections.Generic 

open SharpDX.DXGI
open SharpDX.Direct3D12
open SharpDX 

open D3DUtilities
open Assets

// ----------------------------------------------------------------------------------------------------
// Hilfs-klassen für die Pipeline  
// ----------------------------------------------------------------------------------------------------
module Pipeline = 
    // ----------------------------------------------------------------------------------------------------
    // PipelinStateObject Descriptions
    // ----------------------------------------------------------------------------------------------------
    let emptyPsoDesc ()  = 
        let psoDesc = 
            new GraphicsPipelineStateDescription( 
                // InputLayout
                // RootSignature  
                // VertexShader  
                // PixelShader =  
                // RasterizerState
                // BlendState 
                DepthStencilState = DepthStencilStateDescription.Default(),
                SampleMask = Int32.MaxValue,
                // PrimitiveTopologyType
                RenderTargetCount = 1,  
                // SampleDescription
                StreamOutput=StreamOutputDescription(),
                DepthStencilFormat = DEPTHSTENCILFORMAT
            )            
        psoDesc.RenderTargetFormats.SetValue(BACKBUFFERFORMAT, 0)
        psoDesc

    let rootSignatureDescEmpty =
        new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout, [||], GetStaticSamplers()) 
        
    let isRootSignatureDescEmpty(rs:RootSignatureDescription) =
        rs.Parameters = null || rs.Parameters.Length = 0

    // ----------------------------------------------------------------------------------------------------
    // PipelineStateObjects Descriptions Classic
    // ----------------------------------------------------------------------------------------------------
    let inputLayoutDescription =
        new InputLayoutDescription(
            [| 
                new InputElement("SV_POSITION",     0, Format.R32G32B32_Float,       0, 0);
                new InputElement("NORMAL",          0, Format.R32G32B32_Float,      12, 0);
                new InputElement("COLOR",           0, Format.R32G32B32A32_Float,   24, 0);    
                new InputElement("TEXCOORD",        0, Format.R32G32_Float,         40, 0);
                new InputElement("BLENDINDICES",    0, Format.R32G32B32A32_UInt,    48, 0); 
                new InputElement("BLENDWEIGHT",     0, Format.R32G32B32A32_Float,   64, 0);   
            |]
        ) 

    let rootSignatureDesc =
        let slotRootParameters =
            [|
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 0))  // t0 : World Textur
                new RootParameter(ShaderVisibility.All,     new RootDescriptor(0, 0), RootParameterType.ConstantBufferView)     // b0 : per Object
                new RootParameter(ShaderVisibility.All,     new RootDescriptor(1, 0), RootParameterType.ConstantBufferView)     // b1 : per Frame
                new RootParameter(ShaderVisibility.All,     new RootDescriptor(2, 0), RootParameterType.ConstantBufferView)     // b2 : per Material
                new RootParameter(ShaderVisibility.All,     new RootDescriptor(3, 0), RootParameterType.ConstantBufferView)     // b3 : per Armature 
            |]
        new RootSignatureDescription(
            RootSignatureFlags.AllowInputAssemblerInputLayout,
            slotRootParameters,
            GetStaticSamplers())  
    
    let createRootSignature(device:Device, signatureDesc:RootSignatureDescription) =
        device.CreateRootSignature(new DataPointer (signatureDesc.Serialize().BufferPointer, int (signatureDesc.Serialize().BufferSize)))

    // ----------------------------------------------------------------------------------------------------
    // PipelineStateObjects Descriptions PBR
    // ----------------------------------------------------------------------------------------------------
    let inputLayoutDescriptionPBR =
        new InputLayoutDescription(
            [| new InputElement("NORMAL",   0, Format.R32G32B32_Float, 0, 0)
               new InputElement("POSITION", 0, Format.R32G32B32_Float, 12, 0)
               new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0) |]
        )

    let rootSignatureDescPBR =
        let slotRootParameters =
            [| new RootParameter(ShaderVisibility.Vertex,   new RootDescriptor(0, 0), RootParameterType.ConstantBufferView)     // b0 : ModelViewProjectionConstantBuffer
               new RootParameter(ShaderVisibility.All,      new RootDescriptor(1, 0), RootParameterType.ConstantBufferView)     // b1 : Frame, Light
               new RootParameter(ShaderVisibility.All,      new RootDescriptor(2, 0), RootParameterType.ConstantBufferView)     // b2 : Object, Material

               new RootParameter(ShaderVisibility.Pixel,    new DescriptorRange(DescriptorRangeType.ShaderResourceView, 5, 0))  // t0 : textures
               new RootParameter(ShaderVisibility.Pixel,    new DescriptorRange(DescriptorRangeType.ShaderResourceView, 3, 8))  // t8 : textures 
               new RootParameter(ShaderVisibility.Pixel,    new DescriptorRange(DescriptorRangeType.Sampler, 5, 0))             // s0 : samplers 
               new RootParameter(ShaderVisibility.Pixel,    new DescriptorRange(DescriptorRangeType.Sampler, 3, 8))             // s8 : samplers 
            |]

        new RootSignatureDescription(
            RootSignatureFlags.AllowInputAssemblerInputLayout,
            slotRootParameters
        )

    // ----------------------------------------------------------------------------------------------------
    // PipelineStateObjects Descriptions - Eigene Entwicklung
    // Der bisherige Vertex-Shader
    // Danach der PBR Shader
    // ----------------------------------------------------------------------------------------------------
    let inputLayoutDescriptionNT =
        new InputLayoutDescription(
            [| 
                new InputElement("SV_POSITION",     0, Format.R32G32B32_Float,       0, 0);
                new InputElement("NORMAL",          0, Format.R32G32B32_Float,      12, 0);
                new InputElement("COLOR",           0, Format.R32G32B32A32_Float,   24, 0);    
                new InputElement("TEXCOORD",        0, Format.R32G32_Float,         40, 0);
                new InputElement("BLENDINDICES",    0, Format.R32G32B32A32_UInt,    48, 0); 
                new InputElement("BLENDWEIGHT",     0, Format.R32G32B32A32_Float,   64, 0);   
            |]
        ) 
    let rootSignatureDescNT =
        let slotRootParameters =
            [|
                new RootParameter(ShaderVisibility.All,     new RootDescriptor(0, 0), RootParameterType.ConstantBufferView)     // b0 : per Object      0
                new RootParameter(ShaderVisibility.All,     new RootDescriptor(1, 0), RootParameterType.ConstantBufferView)     // b1 : per Frame       1
                new RootParameter(ShaderVisibility.All,     new RootDescriptor(2, 0), RootParameterType.ConstantBufferView)     // b2 : per Material    2

                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 0))  // t0 : textures        3
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 1))  // t1 : textures        4 
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 2))  // t2 : textures        5
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 3))  // t3 : textures        6
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 4))  // t4 : textures        7
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 8))  // t8 : textures        8
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 9))  // t9 : textures        9
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1,10))  // t10: textures       10

                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.Sampler, 1, 0))             // s0 : samplers       11
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.Sampler, 1, 1))             // s1 : samplers       12
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.Sampler, 1, 2))             // s2 : samplers       13
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.Sampler, 1, 3))             // s3 : samplers       14
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.Sampler, 1, 4))             // s4 : samplers       15
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.Sampler, 1, 8))             // s8 : samplers       16
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.Sampler, 1, 9))             // s9 : samplers       17
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.Sampler, 1,10))             // s10: samplers       18
            |]
        new RootSignatureDescription(
            RootSignatureFlags.AllowInputAssemblerInputLayout,
            slotRootParameters
        )  