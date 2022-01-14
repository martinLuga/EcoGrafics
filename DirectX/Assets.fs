namespace DirectX
//
//  BufferDescriptions.fs
//
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

// ----------------------------------------------------------------------------------------------------
// Description
// für die Übergabe an die Shader
// ----------------------------------------------------------------------------------------------------
module Assets =

    let BACKBUFFERFORMAT    = Format.R8G8B8A8_UNorm      
    let DEPTHSTENCILFORMAT  = Format.D24_UNorm_S8_UInt 

    let mutable allRootSignatureDescriptions         = new Dictionary<string, RootSignatureDescription>() 
    let mutable allInputLayoutDescriptions           = new Dictionary<string, InputLayoutDescription>() 
    let mutable allGraphicsPipelineStateDescriptions = new Dictionary<string, GraphicsPipelineStateDescription>() 

    let textureDescription(resource:Resource) =
        new ShaderResourceViewDescription(
            Shader4ComponentMapping = D3DUtil.DefaultShader4ComponentMapping,
            Format = resource.Description.Format,
            Dimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new ShaderResourceViewDescription.Texture2DResource(
                MostDetailedMip = 0,
                MipLevels = -1,
                ResourceMinLODClamp = 0.0f
            )
        )

    // Depth stencil state
    let ff = 
        new DepthStencilOperationDescription(
            Comparison = Comparison.Always,
            PassOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep,
            DepthFailOperation = StencilOperation.Increment
        )
    let bf = 
        new DepthStencilOperationDescription(
            Comparison = Comparison.Always,
            PassOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep,
            DepthFailOperation = StencilOperation.Decrement
        )
    let depthStencilStateDescription = 
        new DepthStencilStateDescription(
            IsDepthEnabled = RawBool(true),  
            DepthComparison = Comparison.Less,
            DepthWriteMask = DepthWriteMask.All,
            IsStencilEnabled = RawBool(false), 
            StencilReadMask  = byte 0xFF, // 0xff (no mask)
            StencilWriteMask = byte 0xFF, // 0xff (no mask)
            FrontFace = ff,
            BackFace = bf
        )

    let depthStencilStateDescriptionDefault =
        new DepthStencilStateDescription(
            IsDepthEnabled = RawBool(true),
            DepthWriteMask = DepthWriteMask.All,
            DepthComparison = Comparison.Less,
            IsStencilEnabled = RawBool(false), 
            StencilReadMask = byte 0xFF, // 0xff (no mask)
            StencilWriteMask = byte 0xFF, // 0xff (no mask)
            FrontFace = new DepthStencilOperationDescription(
                Comparison = Comparison.Always, DepthFailOperation = StencilOperation.Keep, FailOperation = StencilOperation.Keep, PassOperation = StencilOperation.Keep
            ),
            BackFace = new DepthStencilOperationDescription(
                Comparison = Comparison.Always, DepthFailOperation = StencilOperation.Keep, FailOperation = StencilOperation.Keep, PassOperation = StencilOperation.Keep
            )
         )

    let depthStencilDescription (clientWidth:Int64, clientHeight:int, msaaCount:int, msaaQuality:int)  = 
        new ResourceDescription( 
            Dimension = ResourceDimension.Texture2D,
            Alignment = 0L,
            Width =  clientWidth,
            Height = clientHeight,  
            DepthOrArraySize = 1s,
            MipLevels = 1s,
            Format = Format.R24G8_Typeless,
            SampleDescription = new SampleDescription(  
                Count = msaaCount,
                Quality = msaaQuality
            ),
            Layout = TextureLayout.Unknown,
            Flags = ResourceFlags.AllowDepthStencil
        )

    let samplerStateDescription = 
        new SamplerStateDescription(        
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            Filter = Filter.MinMagMipLinear
        )    

    let samplerStateDescription2 = 
        new SamplerStateDescription(   
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            BorderColor = new RawColor4(0.0f, 0.0f, 0.0f, 0.0f),
            ComparisonFunction = Comparison.Never,
            Filter = Filter.MinMagMipLinear,
            MaximumAnisotropy = 16,
            MaximumLod = Single.MaxValue,
            MinimumLod = 0.0f,
            MipLodBias = 0.0f
        )

    let blendStateOpaque =
        BlendStateDescription.Default()

    let transparencyBlendDesc =
        new RenderTargetBlendDescription(        
            IsBlendEnabled = RawBool(true),
            LogicOpEnable = RawBool(false),
            SourceBlend = BlendOption.SourceAlpha,
            DestinationBlend = BlendOption.InverseSourceAlpha ,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.One,
            DestinationAlphaBlend = BlendOption.Zero,
            AlphaBlendOperation = BlendOperation.Add,
            //LogicOp = LogicOperation.Noop,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        )

    let blendStateTransparent =
        let bs = BlendStateDescription.Default()
        bs.RenderTarget.[0] <- transparencyBlendDesc
        bs

    // CullMode (Cull=Wegschneiden) Back = hinteres wird weggeschnitten
    // IsFrontCounterClockwise, korrespondiert zu der Reihenfolge der Indices bei der Meshberechnung
    let rasterizerStateSolid =
        new RasterizerStateDescription(
            FillMode = FillMode.Solid,
            CullMode = CullMode.Back,
            IsFrontCounterClockwise = RawBool(true)
        ) 

    let rasterizerStateWired =
        new RasterizerStateDescription(
            FillMode = FillMode.Wireframe,
            CullMode = CullMode.None,
            IsFrontCounterClockwise = RawBool(false)
        ) 

    let textDescription width height =
        ResourceDescription.Texture2D(
            Format.R8G8B8A8_UNorm,
            width,
            height
        )

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

    let createRootSignature(device:Device, signatureDesc:RootSignatureDescription) =
        device.CreateRootSignature(new DataPointer (signatureDesc.Serialize().BufferPointer, int (signatureDesc.Serialize().BufferSize)))

    let optClear = 
        new ClearValue( 
            Format = DEPTHSTENCILFORMAT,
            DepthStencil = new DepthStencilValue(
                Depth = 1.0f,
                Stencil = byte 0
            )
        )

    let defaultInputElementsDescriptionNew = 
        new InputLayoutDescription(
            [| 
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0);
                new InputElement("NORMAL",   0, Format.R32G32B32_Float, 12, 0);
                new InputElement("COLOR",    0, Format.R32G32B32A32_Float, 24, 0);    
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 40, 0)
            |]
        )

