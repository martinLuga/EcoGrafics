namespace Base
//
//  ShaderSupport.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX.DXGI
open SharpDX.Direct3D 
open SharpDX.Direct3D12
open SharpDX.Mathematics.Interop
open SharpDX 

open log4net

// ----------------------------------------------------------------------------------------------------
// Hilfs-klassen für Shaders  
// ----------------------------------------------------------------------------------------------------
module ShaderSupport = 

    type TopologyType   = | Triangle      | Patch         |  Line         | Undefinded
    type RasterType     = | Solid  = 'S'  | Wired = 'W'   |  Undefinded = 'U'
    type BlendType      = | Opaque = 'O'  | Transparent = 'T' | Undefinded = 'U'
    type ShaderType     = | Vertex = 1    | Pixel = 2     |  Domain = 3   | Hull = 4   
    type ShaderUsage    = | Required = 0  | NotRequired = 1 | ToBeFilledIn = 2

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

    let rootSignatureDescEmpty =
        new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout, [||], GetStaticSamplers()) 
        
    let isRootSignatureDescEmpty(rs:RootSignatureDescription) =
        rs.Parameters.Length = 0

    [<AllowNullLiteral>] 
    // ----------------------------------------------------------------------------------------------------
    // Shaderdescription
    // ----------------------------------------------------------------------------------------------------
    type ShaderDescription =
        val Klass:ShaderType
        val Directory:string 
        val File:string
        val Entry:string
        val Mode:string
        val Use:ShaderUsage
        val RootSignature:RootSignatureDescription
        new (klass, directory, file, entry, mode, rootSignature, usage) = {Klass=klass; Directory=directory; File=file; Entry=entry; Mode=mode ;RootSignature=rootSignature; Use=usage}
        new (klass) = {Klass=klass; Directory=""; File=""; Entry=""; Mode=""; RootSignature=rootSignatureDescEmpty;Use=ShaderUsage.ToBeFilledIn}
        new (klass, usage) = {Klass=klass; Directory=""; File=""; Entry=""; Mode=""; RootSignature=rootSignatureDescEmpty;Use=usage}
        override this.ToString() = this.Klass.ToString() + "->" + this.Entry
        member self.NotRequired() = self.Use=ShaderUsage.NotRequired
        member self.ToBeFilledIn() = self.Use=ShaderUsage.ToBeFilledIn
        member self.IsSet() = not (self.IsEmpty())
        member self.IsEmpty()= self.File = ""
        member self.asFileInfo = (self.Directory, self.File, self.Entry, self.Mode)
        member self.asString = self.File + "." + self.Entry
        static member CreateToBeFilledIn(klass) = new ShaderDescription(klass, ShaderUsage.ToBeFilledIn)
        static member CreateNotRequired(klass) = new ShaderDescription(klass, ShaderUsage.NotRequired)

    [<AllowNullLiteral>] 
    type TopologyTypeDescription=
        val Type:TopologyType
        val Description:PrimitiveTopologyType
        new (typ, description) = {Type=typ; Description=description}
        override this.ToString() = this.Type.ToString()

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

    let blendStateOpaque =
        BlendStateDescription.Default()

    let blendStateTransparent =
        let bs = BlendStateDescription.Default()
        bs.RenderTarget.[0] <- transparencyBlendDesc
        bs

    [<AllowNullLiteral>] 
    type RasterizerDescription=
        val Type:RasterType
        val Description:RasterizerStateDescription
        new (typ, description) = {Type=typ; Description=description}
        override this.ToString() =  "RASTR->" + this.Type.ToString()
        static member Default() = 
            new RasterizerDescription(RasterType.Solid, rasterizerStateSolid)       
        member self.asString = string self.Type  

    [<AllowNullLiteral>] 
    type BlendDescription=
        val Type:BlendType
        val Description:BlendStateDescription
        new (typ, description) = {Type=typ; Description=description}
        override this.ToString() = "BLD->" + this.Type.ToString()
        static member Default() = 
            new BlendDescription(BlendType.Opaque, blendStateTransparent)        
        member self.asString = string self.Type 

    let blendDescOpaque =
        BlendDescription(BlendType.Opaque, blendStateOpaque)

    let blendDescTransparent =
        BlendDescription(BlendType.Transparent, blendStateTransparent)

    let topologyTypeTriangle = 
        PrimitiveTopologyType.Triangle

    let topologyTypePatch = 
        PrimitiveTopologyType.Patch

    let topologyTypeLine = 
        PrimitiveTopologyType.Line

    let defaultInputElementsDescriptionNew = 
        new InputLayoutDescription(
            [| 
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0);
                new InputElement("NORMAL",   0, Format.R32G32B32_Float, 12, 0);
                new InputElement("COLOR",    0, Format.R32G32B32A32_Float, 24, 0);    
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 40, 0)
            |]
        )

    let layoutCookBook =
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

    let layoutLunaBook =
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

    let layoutTesselated =
        new InputLayoutDescription(
            [| 
                new InputElement("POSITION",        0, Format.R32G32B32_Float,       0, 0);
                new InputElement("COLOR",           0, Format.R32G32B32A32_Float,   12, 0);
                new InputElement("TEXCOORD",        0, Format.R32G32_Float,         24, 0);
                new InputElement("NORMAL",          0, Format.R32G32B32_Float,      32, 0);
            |]
        ) 

    // ----------------------------------------------------------------------------------------------------
    // Root signature descriptions
    // ----------------------------------------------------------------------------------------------------

    // CookBook App
    let rootSignatureDescCookBook  =
        let textureTable = new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 0) 
        let rootParameter0 = new RootParameter(ShaderVisibility.Pixel, textureTable)                                                     // t0 : Texture
        let rootParameter1 = new RootParameter(ShaderVisibility.All,     new RootDescriptor(0, 0), RootParameterType.ConstantBufferView) // b0 : per Object
        let rootParameter2 = new RootParameter(ShaderVisibility.All,     new RootDescriptor(1, 0), RootParameterType.ConstantBufferView) // b1 : per Frame
        let rootParameter3 = new RootParameter(ShaderVisibility.All,     new RootDescriptor(2, 0), RootParameterType.ConstantBufferView) // b2 : per Material
        let rootParameter4 = new RootParameter(ShaderVisibility.All,     new RootDescriptor(3, 0), RootParameterType.ConstantBufferView) // b3 : per Armature 
 
        let slotRootParameters = [|rootParameter0; rootParameter1; rootParameter2; rootParameter3; rootParameter4|] 
        new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout, slotRootParameters, GetStaticSamplers())  

    // CookBook App with tesselation
    let rootSignatureDescCookBookTesselate  =
        let textureTable   = new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 0) 
        let rootParameter0 = new RootParameter(ShaderVisibility.Pixel, textureTable)                                                  // t0 : Texture
        let rootParameter1 = new RootParameter(ShaderVisibility.All,  new RootDescriptor(0, 0), RootParameterType.ConstantBufferView) // b0 : per Object
        let rootParameter2 = new RootParameter(ShaderVisibility.All,  new RootDescriptor(1, 0), RootParameterType.ConstantBufferView) // b1 : per Frame
        let rootParameter3 = new RootParameter(ShaderVisibility.All,  new RootDescriptor(2, 0), RootParameterType.ConstantBufferView) // b2 : per Material
        let rootParameter4 = new RootParameter(ShaderVisibility.All,  new RootDescriptor(3, 0), RootParameterType.ConstantBufferView) // b3 : per Armature 
 
        let slotRootParameters = [|rootParameter0; rootParameter1; rootParameter2; rootParameter3; rootParameter4|] 
        new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout, slotRootParameters, GetStaticSamplers())  

    // LunaBook App
    let rootSignatureDescLunaBook =
        let slotRootParameters =
            [|
                new RootParameter(ShaderVisibility.All,    new RootDescriptor(0, 0), RootParameterType.ConstantBufferView)     // b0 : Per Object
                new RootParameter(ShaderVisibility.All,    new RootDescriptor(1, 0), RootParameterType.ConstantBufferView)     // b1 : Per Frame
                new RootParameter(ShaderVisibility.All,    new RootDescriptor(0, 1), RootParameterType.ShaderResourceView)     // b2 : Per Material
                new RootParameter(ShaderVisibility.All,    new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 0)) 
                new RootParameter(ShaderVisibility.All,    new DescriptorRange(DescriptorRangeType.ShaderResourceView, 5, 1))
            |]
        new RootSignatureDescription(
            RootSignatureFlags.AllowInputAssemblerInputLayout,
            slotRootParameters,
            GetStaticSamplers())  

    let createRootSignature(device:Device, signatureDesc:RootSignatureDescription) =
        device.CreateRootSignature(new DataPointer (signatureDesc.Serialize().BufferPointer, int (signatureDesc.Serialize().BufferSize)))

    // ----------------------------------------------------------------------------------------------------
    // ShaderConnector
    // ----------------------------------------------------------------------------------------------------
    
    let logger = LogManager.GetLogger("MyPipelineConfiguration")
    
    let emptyInfo = ("","","","","")
    
    let printLayoutElement(elem:InputElement) =
        let mutable result = " --- --- "
        result <- result + elem.SemanticName + " " + elem.SemanticIndex.ToString() +  " " + elem.Format.ToString() + "\n"
        result
    
    let printLayoutDescription (desc:InputLayoutDescription) = 
        let mutable result = " --- InputLayout \n"
        for elem in desc.Elements do
            result <- result + printLayoutElement(elem) 
        result
    
    let shaderName(shaderDesc: ShaderDescription) =
        if shaderDesc.File = "" then
            ""
        else
            shaderDesc.File + "-" + shaderDesc.Entry
    
    let shaderType(shaderDesc: ShaderDescription) = 
        let typ = shaderDesc.Mode.Substring(0,2)
        match typ with
        | "vs"  ->       ShaderType.Vertex
        | "ps"  ->       ShaderType.Pixel        
        | "ds"  ->       ShaderType.Domain   
        | "hs"  ->       ShaderType.Hull           
        | _     ->       raise (System.Exception("Invalid Shadertype")  )
        
    let blendOpaqueDescription =
        BlendDescription(BlendType.Opaque, blendStateOpaque)
    
    let blendTransparentDescription =
        BlendDescription(BlendType.Transparent, blendStateTransparent)
    
    let AllBlendDescriptions =
        [blendOpaqueDescription; blendTransparentDescription]
    
    let blendDescFromType(blendType:BlendType) =
        AllBlendDescriptions |> List.find (fun blend -> blend.Type = blendType)
    
    let rasterWiredDescription =
        RasterizerDescription(RasterType.Wired, rasterizerStateWired)
    let rasterSolidDescription =
        RasterizerDescription(RasterType.Solid, rasterizerStateSolid)
    let AllRasterDescriptions =
        [rasterSolidDescription; rasterWiredDescription]
    let rasterizerDescFromType(rasterType:RasterType)=
        AllRasterDescriptions |> List.find (fun rastr -> rastr.Type = rasterType)
        
    // ----------------------------------------------------------------------------------------------------
    // Class MyShaderConnector  
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>] 
    type ShaderConfiguration(  
        vertexShaderDesc:ShaderDescription, pixelShaderDesc: ShaderDescription, 
        domainShaderDesc: ShaderDescription, hullShaderDesc: ShaderDescription
        ) =
        let mutable vertexShaderDesc=vertexShaderDesc
        let mutable pixelShaderDesc=pixelShaderDesc
        let mutable domainShaderDesc=domainShaderDesc
        let mutable hullShaderDesc=hullShaderDesc
    

        new(vertexShaderDesc, pixelShaderDesc) = new ShaderConfiguration(vertexShaderDesc,pixelShaderDesc, ShaderDescription.CreateNotRequired(ShaderType.Domain), ShaderDescription.CreateNotRequired(ShaderType.Hull))
    
        member this.VertexShaderDesc
            with get() = vertexShaderDesc
    
        member this.PixelShaderDesc
            with get() = pixelShaderDesc
            and set(value) = 
                pixelShaderDesc <- value 
    
        member this.DomainShaderDesc
            with get() = domainShaderDesc
            and set(value) = 
                domainShaderDesc <- value
    
        member this.HullShaderDesc
            with get() = hullShaderDesc
            and set(value) = 
                hullShaderDesc <- value 
    
        member this.IsEmpty() = this.VertexShaderDesc.IsEmpty()

        // Dummy Configuration
        // Alle shader über den Cache
        static member CreateForTesselation() = 
            new ShaderConfiguration(ShaderDescription.CreateToBeFilledIn(ShaderType.Vertex),ShaderDescription.CreateToBeFilledIn(ShaderType.Pixel), ShaderDescription.CreateToBeFilledIn(ShaderType.Domain), ShaderDescription.CreateToBeFilledIn(ShaderType.Hull))
     
        // Dummy Configuration
        // Vertex und Pixel shader über den Cache, die anderen nicht benötigt
        static member CreateNoTesselation() = 
            new ShaderConfiguration(ShaderDescription.CreateToBeFilledIn(ShaderType.Vertex), ShaderDescription.CreateToBeFilledIn(ShaderType.Pixel), ShaderDescription.CreateNotRequired(ShaderType.Domain), ShaderDescription.CreateNotRequired(ShaderType.Hull))
