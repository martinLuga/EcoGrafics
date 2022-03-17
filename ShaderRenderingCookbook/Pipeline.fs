namespace ShaderRenderingCookbook
//
//  Pipeline.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX.DXGI
open SharpDX.Direct3D12

// ----------------------------------------------------------------------------------------------------
// ShaderRenderingCookbook Pipeline  
// ----------------------------------------------------------------------------------------------------
module Pipeline = 

    let GetStaticSamplers() =   
        [|
            // PointWrap
            new StaticSamplerDescription(
                shaderVisibility=ShaderVisibility.All,
                shaderRegister=0,
                registerSpace=0,
                Filter=Filter.MinMagMipPoint,
                AddressUVW = TextureAddressMode.Wrap
            );        
            // PointClamp
            new StaticSamplerDescription(
                shaderVisibility=ShaderVisibility.All,
                shaderRegister=1,
                registerSpace=0, 
                Filter = Filter.MinMagMipPoint,
                AddressUVW = TextureAddressMode.Clamp
            ); 
            // LinearWrap
            new StaticSamplerDescription(
                shaderVisibility=ShaderVisibility.All,
                shaderRegister=2,
                registerSpace=0, 
                Filter = Filter.MinMagMipLinear,
                AddressUVW = TextureAddressMode.Wrap
            ); 
            // LinearClamp
            new StaticSamplerDescription(
                shaderVisibility=ShaderVisibility.All,
                shaderRegister=3,
                registerSpace=0, 
                Filter = Filter.MinMagMipLinear,
                AddressUVW = TextureAddressMode.Clamp
            ); 
            // AnisotropicWrap
            new StaticSamplerDescription(
                shaderVisibility=ShaderVisibility.All,
                shaderRegister=4,
                registerSpace=0, 
                Filter = Filter.Anisotropic,
                AddressUVW = TextureAddressMode.Wrap,
                MipLODBias = 0.0f,
                MaxAnisotropy = 8
            ); 
            // AnisotropicClamp
            new StaticSamplerDescription(
                shaderVisibility=ShaderVisibility.All,
                shaderRegister=5,
                registerSpace=0, 
                Filter = Filter.Anisotropic,
                AddressUVW = TextureAddressMode.Clamp,
                MipLODBias = 0.0f,
                MaxAnisotropy = 8
            )
        |]

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
                new RootParameter(ShaderVisibility.All,     new RootDescriptor(0, 0), RootParameterType.ConstantBufferView)     // b0 : per Object
                new RootParameter(ShaderVisibility.All,     new RootDescriptor(1, 0), RootParameterType.ConstantBufferView)     // b1 : per Frame
                new RootParameter(ShaderVisibility.All,     new RootDescriptor(2, 0), RootParameterType.ConstantBufferView)     // b2 : per Material
                new RootParameter(ShaderVisibility.All,     new RootDescriptor(3, 0), RootParameterType.ConstantBufferView)     // b3 : per Armature 
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 0))  // t0 : Cube Textur
                new RootParameter(ShaderVisibility.Pixel,   new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 1))  // t1 : Textur
            |]
        new RootSignatureDescription(
            RootSignatureFlags.AllowInputAssemblerInputLayout,
            slotRootParameters,
            GetStaticSamplers()) 