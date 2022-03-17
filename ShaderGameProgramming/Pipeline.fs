namespace ShaderGameProgramming
//
//  Pipeline.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX.DXGI
open SharpDX.Direct3D12

// ----------------------------------------------------------------------------------------------------
// dx12gameprogramming Pipeline  
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
                new InputElement("POSITION",    0, Format.R32G32B32_Float,  0, 0);
                new InputElement("NORMAL",      0, Format.R32G32B32_Float,  12, 0);
                new InputElement("TEXCOORD",    0, Format.R32G32_Float,     24, 0);
            |]
        ) 

    let rootSignatureDesc =
        let slotRootParameters =
            [|
                new RootParameter(ShaderVisibility.All, new RootDescriptor(0, 0), RootParameterType.ConstantBufferView);
                new RootParameter(ShaderVisibility.All, new RootDescriptor(1, 0), RootParameterType.ConstantBufferView);
                new RootParameter(ShaderVisibility.All, new RootDescriptor(0, 1), RootParameterType.ShaderResourceView);
                new RootParameter(ShaderVisibility.All, new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 0));
                new RootParameter(ShaderVisibility.All, new DescriptorRange(DescriptorRangeType.ShaderResourceView, 5, 1));
            |]

        new RootSignatureDescription(
            RootSignatureFlags.AllowInputAssemblerInputLayout,
            slotRootParameters,
            GetStaticSamplers()) 