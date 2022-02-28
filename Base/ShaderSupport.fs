namespace Base
//
//  ShaderSupport.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX.Direct3D
open SharpDX.Direct3D12
open SharpDX.Mathematics.Interop

// ----------------------------------------------------------------------------------------------------
// Eigene Typen für Shaders  
// ----------------------------------------------------------------------------------------------------
module ShaderSupport = 

    type TopologyType   = | Triangle      | Patch               | Line         | Undefinded
    type RasterType     = | Solid  = 'S'  | Wired = 'W'         | Transparent = 'T'|  Undefinded = 'U'
    type BlendType      = | Opaque = 'O'  | Transparent = 'T'   | Undefinded = 'U'
    type ShaderType     = | Vertex = 1    | Pixel = 2           | Domain = 3   | Hull = 4  |  Undefinded = 99 
    type ShaderUsage    = | Required = 0  | NotRequired = 1     | ToBeFilledIn = 2 | Undefinded = 99 

    let  one = "1" 

    // ----------------------------------------------------------------------------------------------------
    // Shader-Defines
    // ----------------------------------------------------------------------------------------------------
    type ShaderDefinePBR =
        | HAS_BASECOLORMAP = 0
        | HAS_NORMALMAP = 1
        | HAS_EMISSIVEMAP = 2
        | HAS_OCCLUSIONMAP = 3
        | HAS_METALROUGHNESSMAP = 4

    type TextureTypePBR =
        | baseColourTexture = 0
        | normalTexture = 1
        | emissionTexture = 2
        | occlusionTexture = 3
        | metallicRoughnessTexture = 4
        | envDiffuseTexture = 5
        | brdfLutTexture = 6
        | envSpecularTexture = 7    
        
    [<AllowNullLiteral>] 
    type ShaderDefineMacros(_macros:string list) =
        let mutable macros:ShaderMacro[] = [||]
        do
            macros <-
                _macros|> List.map (fun m -> new ShaderMacro(m, one))|> List.toArray

        member this.Defines = macros
        override this.ToString() = macros|> Array.map(fun m -> m.Name) |> Seq.fold(fun s result -> result + s) ""  
        
        static member CreateFrom(_shaderDefines:ShaderDefinePBR list) = new ShaderDefineMacros((_shaderDefines |> Seq.map(fun sd -> string sd ) |> Seq.toList) ) 

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
        val mutable Defines:string list
        val Use:ShaderUsage
        val RootSignature:RootSignatureDescription
        new (klass, directory, file, entry, mode, defines, usage, rootSignature) = {Klass=klass; Directory=directory; File=file; Entry=entry; Mode=mode; Defines=defines; Use=usage ;RootSignature=rootSignature}
        new (klass, directory, file, entry, mode, usage, rootSignature) = {Klass=klass; Directory=directory; File=file; Entry=entry; Mode=mode; Defines=[]; Use=usage ;RootSignature=rootSignature}
        new (klass, usage) = {Klass=klass; Directory=""; File=""; Entry=""; Mode=""; Defines=[]; RootSignature=new RootSignatureDescription();Use=usage}
        new () = {Klass=ShaderType.Undefinded; Directory=""; File=""; Entry=""; Mode=""; Defines=[]; RootSignature=new RootSignatureDescription(); Use=ShaderUsage.Undefinded}
        
        override this.Equals other =
            match other with
            | :? ShaderDescription as p -> p.asString.Equals(this.asString)
            | _ -> false

        override this.GetHashCode () = this.asString.GetHashCode()

        override this.ToString() = this.Klass.ToString() + "->" + this.Entry

        member self.NotRequired() = self.Use=ShaderUsage.NotRequired
        member self.ToBeFilledIn() = self.Use=ShaderUsage.ToBeFilledIn
        member self.IsSet() = not (self.IsEmpty())
        member self.IsEmpty()= self.File = ""
        member self.asFileInfo = (self.Directory, self.File, self.Entry, self.Mode)
        member self.asString = self.File + "." + self.Entry + self.Defines.ToString()
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

    let rasterizerStateTransparent =
        let mutable result = RasterizerStateDescription.Default()
        result.CullMode <- CullMode.None
        result

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
        val mutable Type:RasterType
        val mutable Description:RasterizerStateDescription
        new (typ, description) = {Type=typ; Description=description}
        override this.ToString() =  "RASTR->" + this.Type.ToString()
        static member Default() = 
            new RasterizerDescription(RasterType.Solid, rasterizerStateSolid) 
        static member Copy(rd:RasterizerDescription) = 
            new RasterizerDescription(rd.Type, rd.Description)      
        member self.asString = string self.Type  

    [<AllowNullLiteral>] 
    type BlendDescription=
        val Type:BlendType
        val Description:BlendStateDescription
        new (typ, description) = {Type=typ; Description=description}
        override this.ToString() = "BLD->" + this.Type.ToString()
        static member Default() = 
            new BlendDescription(BlendType.Opaque, blendStateOpaque)        
        member self.asString = string self.Type 

    let blendDescOpaque =
        BlendDescription(BlendType.Opaque, blendStateOpaque)

    let blendDescTransparent =
        BlendDescription(BlendType.Transparent, blendStateTransparent)

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
    // Shader Konfiguration
    // Kombination der Shaders, die für ein Part benötigt wird
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
