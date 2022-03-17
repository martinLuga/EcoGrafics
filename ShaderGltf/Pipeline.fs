namespace ShaderGltf
//
//  Pipeline.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System

open SharpDX.DXGI
open SharpDX.Direct3D12

open DirectX.Assets

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
                DepthStencilState = DepthStencilStateDescription.Default(),
                SampleMask = Int32.MaxValue,
                RenderTargetCount = 1,  
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
    // PipelineStateObjects Descriptions - Eigene Entwicklung
    // Der bisherige Vertex-Shader
    // Danach der PBR Shader
    // ----------------------------------------------------------------------------------------------------
    let inputLayoutDescription =
        new InputLayoutDescription(
            [| 
                new InputElement("SV_POSITION",     0, Format.R32G32B32_Float,       0, 0);
                new InputElement("NORMAL",          0, Format.R32G32B32_Float,      12, 0);   
                new InputElement("TEXCOORD",        0, Format.R32G32_Float,         24, 0); 
            |]
        ) 
    let rootSignatureDesc =
        let slotRootParameters =
            [|
                new RootParameter(ShaderVisibility.All, new RootDescriptor(0, 0), RootParameterType.ConstantBufferView)     // b0 : per Object      0
                new RootParameter(ShaderVisibility.All, new RootDescriptor(1, 0), RootParameterType.ConstantBufferView)     // b1 : per Frame       1
                new RootParameter(ShaderVisibility.All, new RootDescriptor(2, 0), RootParameterType.ConstantBufferView)     // b2 : per Material    2

                new RootParameter(ShaderVisibility.All, new DescriptorRange(DescriptorRangeType.ShaderResourceView, 5, 0))  // t0 - t04: textures   3
                new RootParameter(ShaderVisibility.All, new DescriptorRange(DescriptorRangeType.ShaderResourceView, 3, 8))  // t8 - t10: textures   4

                new RootParameter(ShaderVisibility.All, new DescriptorRange(DescriptorRangeType.Sampler, 5, 0))             // s0 : samplers        5
                new RootParameter(ShaderVisibility.All, new DescriptorRange(DescriptorRangeType.Sampler, 3, 8))             // s1 : samplers        6
            |]
        new RootSignatureDescription(
            RootSignatureFlags.AllowInputAssemblerInputLayout,
            slotRootParameters
        )  