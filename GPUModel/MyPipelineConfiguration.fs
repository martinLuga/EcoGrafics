namespace GPUModel
//
//  MyPipelineConfiguration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 

open log4net

open SharpDX.DXGI
open SharpDX.Direct3D12

open Base.ShaderSupport
open Shader.ShaderCompile

open DirectX.Assets
  
// ----------------------------------------------------------------------------------------------------
// ShaderConnector
// ----------------------------------------------------------------------------------------------------
module MyPipelineConfiguration =

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
        if shaderDesc.Application = "" then
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
        | _     ->       ShaderType.Undefinded

    // ----------------------------------------------------------------------------------------------------
    // Class MyShaderConnector  
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>] 
    type MyPipelineConfiguration(configName:string, inputLayoutDesc:InputLayoutDescription, rootSignatureDescription:RootSignatureDescription, 
        vertexShaderDesc:ShaderDescription, pixelShaderDesc: ShaderDescription, 
        domainShaderDesc: ShaderDescription, hullShaderDesc: ShaderDescription, 
        sampleDesc:SampleDescription, topologyTypeDesc:TopologyTypeDescription,
        rasterizerStateDesc:RasterizerDescription, blendStateDesc:BlendDescription
        ) =
        let mutable configName=configName
        let mutable vertexShaderDesc=vertexShaderDesc
        let mutable pixelShaderDesc=pixelShaderDesc
        let mutable domainShaderDesc=domainShaderDesc
        let mutable hullShaderDesc=hullShaderDesc
        let mutable vertexShader:ShaderBytecode = new ShaderBytecode()
        let mutable pixelShader:ShaderBytecode = new ShaderBytecode()
        let mutable domainShader:ShaderBytecode = new ShaderBytecode()
        let mutable hullShader:ShaderBytecode = new ShaderBytecode()
        let mutable inputLayoutDesc = inputLayoutDesc
        let mutable rootSignatureDesc=rootSignatureDescription
        let mutable topologyTypedesc=topologyTypeDesc
        let mutable sampleDesc=sampleDesc
        let mutable rasterizerStateDesc=rasterizerStateDesc
        let mutable blendStateDesc=blendStateDesc

        do  
            vertexShader <- shaderFromFile(vertexShaderDesc.asFileInfo)
            
            if  pixelShaderDesc.IsEmpty() then
                raise (ShaderError("PixelShader is missing"))
            else
                pixelShader <- shaderFromFile(pixelShaderDesc.asFileInfo)
            
            if domainShaderDesc.IsEmpty() then
                logger.Debug("Config without domain shader")
            else
                domainShader <- shaderFromFile(domainShaderDesc.asFileInfo)
            
            if not (hullShaderDesc.IsEmpty())then
                hullShader <- shaderFromFile(hullShaderDesc.asFileInfo)

        override this.ToString() =
            "Config: " + configName +  ":" + vertexShaderDesc.Klass.ToString() + ":" + pixelShaderDesc.Klass.ToString()
            + ":" + domainShaderDesc.Klass.ToString() + ":" + hullShaderDesc.Klass.ToString()
            + ":" + rasterizerStateDesc.Type.ToString()
            + ":" + blendStateDesc.Type.ToString()
            + ":" + topologyTypeDesc.Type.ToString()

        member this.ConfigName
            with get() = configName
            
        member this.VertexShaderDesc
            with get() = vertexShaderDesc

        member this.VertexShader
            with get() = vertexShader

        member this.PixelShader
            with get() = pixelShader

        member this.PixelShaderDesc
            with get() = pixelShaderDesc
            and set(value) = 
                pixelShaderDesc <- value 
                pixelShader <- shaderFromFile(pixelShaderDesc.asFileInfo)

        member this.DomainShaderDesc
            with get() = domainShaderDesc
            and set(value) = 
                domainShaderDesc <- value
                domainShader <- shaderFromFile(domainShaderDesc.asFileInfo) 

        member this.HullShaderDesc
            with get() = hullShaderDesc
            and set(value) = 
                hullShaderDesc <- value 
                hullShader <- shaderFromFile(hullShaderDesc.asFileInfo)

        member this.DomainShader
            with get() = domainShader

        member this.HullShader
            with get() = hullShader

        member this.RootSignatureDesc
            with get() = rootSignatureDesc
            and set(value) = rootSignatureDesc <- value

        member this.InputLayoutDesc
            with get() = inputLayoutDesc
            and set(value) = inputLayoutDesc <- value

        member this.SampleDescription
            with get() = sampleDesc
            and set(value) = sampleDesc <- value

        member this.TopologyTypeDesc
            with get() = topologyTypedesc
            and set(value) = topologyTypedesc <- value

        member this.RasterizerStateDesc 
            with get() = rasterizerStateDesc
            and set(value) = rasterizerStateDesc <- value

        member this.BlendStateDesc 
            with get() = blendStateDesc
            and set(value) = blendStateDesc <- value

    let psoDescFromConfiguration(device, conf:MyPipelineConfiguration) =
        let mutable psoDesc = emptyPsoDesc() 
        psoDesc.InputLayout             <- conf.InputLayoutDesc 
        psoDesc.RootSignature           <- createRootSignature(device, conf.RootSignatureDesc) 
        psoDesc.VertexShader            <- conf.VertexShader  
        psoDesc.PixelShader             <- conf.PixelShader   
        if not (conf.DomainShaderDesc.IsEmpty()) then
            psoDesc.DomainShader        <- conf.DomainShader 
        if not (conf.HullShaderDesc.IsEmpty()) then  
            psoDesc.HullShader          <- conf.HullShader  
        psoDesc.BlendState              <- conf.BlendStateDesc.Description  
        psoDesc.RasterizerState         <- conf.RasterizerStateDesc.Description 
        psoDesc.PrimitiveTopologyType   <- conf.TopologyTypeDesc.Description
        psoDesc.SampleDescription       <- conf.SampleDescription
        psoDesc

    let blendOpaqueDescription =
        BlendDescription(BlendType.Opaque, blendStateOpaque)
    let blendTransparentDescription =
        BlendDescription(BlendType.Transparent, blendStateTransparent)
    let AllBlendDescriptions =
        [blendOpaqueDescription; blendTransparentDescription]
    let blendDescFromType(blendType:BlendType)=
        AllBlendDescriptions |> List.find (fun blend -> blend.Type = blendType)

    let rasterWiredDescription =
        RasterizerDescription(RasterType.Wired, rasterizerStateWired)
    let rasterSolidDescription =
        RasterizerDescription(RasterType.Solid, rasterizerStateSolid)
    let AllRasterDescriptions =
        [rasterSolidDescription; rasterWiredDescription]
    let rasterizerDescFromType(rasterType:RasterType)=
        AllRasterDescriptions |> List.find (fun rastr -> rastr.Type = rasterType)
    
    let topologyTriangleDescription =
        TopologyTypeDescription(TopologyType.Triangle, topologyTypeTriangle)    
    let topologyPatchDescription =
        TopologyTypeDescription(TopologyType.Patch, topologyTypePatch)
    let topologyLineDescription =
        TopologyTypeDescription(TopologyType.Line, topologyTypeLine)
    let AllTopologyDescriptions =
        [topologyPatchDescription; topologyPatchDescription]  
    let topologyDescFromType(topologyType:TopologyType)=
         AllTopologyDescriptions |> List.find (fun topology -> topology.Type = topologyType)
    let toplogyDescFromDirectX(topoType:PrimitiveTopologyType)=
        match topoType with 
        | PrimitiveTopologyType.Triangle    -> topologyTriangleDescription
        | PrimitiveTopologyType.Patch       -> topologyPatchDescription
        | PrimitiveTopologyType.Line        -> topologyLineDescription 
        | _ ->  TopologyTypeDescription(TopologyType.Undefinded, PrimitiveTopologyType.Undefined)
 