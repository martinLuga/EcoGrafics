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
open SharpDX.Mathematics.Interop
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

    let defaultInputElementsDescriptionNew = 
        new InputLayoutDescription(
            [| 
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0);
                new InputElement("NORMAL",   0, Format.R32G32B32_Float, 12, 0);
                new InputElement("COLOR",    0, Format.R32G32B32A32_Float, 24, 0);    
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 40, 0)
            |]
        )

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

    // ----------------------------------------------------------------------------------------------------
    // Root signature descriptions
    // ----------------------------------------------------------------------------------------------------

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

