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
open SharpDX.Direct3D
open SharpDX.D3DCompiler
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

    // Describe and create a shader resource view (SRV) descriptor heap.
    // Für das Render Target
    let CreateTextHeap(device:Device) =
        let srvHeapDesc =  
            new DescriptorHeapDescription(  
                DescriptorCount = 64,
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView 
            )
        device.CreateDescriptorHeap(srvHeapDesc) 

    let CreateSampHeap(device:Device) =
        let  samplerHeapDesc = 
            new DescriptorHeapDescription(   
                DescriptorCount = 64,
                Type =  DescriptorHeapType.Sampler,
                Flags = DescriptorHeapFlags.ShaderVisible
            )   
        device.CreateDescriptorHeap(samplerHeapDesc)

    let mutable srvDesc = 
        new ShaderResourceViewDescription(
            Shader4ComponentMapping = 0x00001688,
            Dimension = ShaderResourceViewDimension.Texture2D 
        )

    // ----------------------------------------------------------------------------------------------------
    // Texture 
    // ----------------------------------------------------------------------------------------------------   
    // 2D
    let textureDesc2D(resource:Resource) =
        srvDesc.Format              <- resource.Description.Format
        srvDesc.Dimension           <- ShaderResourceViewDimension.Texture2D
        srvDesc.Texture2D           <-
            new ShaderResourceViewDescription.Texture2DResource(
                MostDetailedMip = 0,
                MipLevels = int resource.Description.MipLevels,
                ResourceMinLODClamp = 0.0f
            )
        srvDesc
   
    // Cube
    let textureDescCube(resource:Resource) =
        srvDesc.Format      <- resource.Description.Format
        srvDesc.Dimension   <- ShaderResourceViewDimension.TextureCube 
        srvDesc.TextureCube <-  
            new ShaderResourceViewDescription.TextureCubeResource(            
                MostDetailedMip = 0,
                MipLevels = int resource.Description.MipLevels,
                ResourceMinLODClamp = 0.0f
            )
        srvDesc 
        
    // CubeArray
    let textureDescCubeArray(resource:Resource) =
        srvDesc.Format      <- resource.Description.Format
        srvDesc.Dimension   <- ShaderResourceViewDimension.TextureCube 
        srvDesc.Texture2DArray <- 
            new ShaderResourceViewDescription.Texture2DArrayResource( 
                ArraySize=6,
                MostDetailedMip = 0,
                MipLevels = int resource.Description.MipLevels,
                ResourceMinLODClamp = 0.0f
            )
        srvDesc  
    
    let textureDescription(resource:Resource, isCube:bool, fromArray:bool) =
        if isCube then
            if fromArray then
                textureDescCubeArray(resource)
            else
                textureDescCube(resource)
        else 
            textureDesc2D(resource)

    // ----------------------------------------------------------------------------------------------------
    // Sampler
    // ----------------------------------------------------------------------------------------------------    
    let samplerDescription=
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

    // ----------------------------------------------------------------------------------------------------
    // Depth stencil  
    // ----------------------------------------------------------------------------------------------------  
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

    // ----------------------------------------------------------------------------------------------------
    // Sampler 
    // ---------------------------------------------------------------------------------------------------- 
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
    // Blendstate and RasterizerState 
    // ---------------------------------------------------------------------------------------------------- 
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