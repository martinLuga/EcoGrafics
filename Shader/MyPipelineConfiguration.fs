namespace Shader
//
//  MyPipelineConfiguration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 

open SharpDX.DXGI
open SharpDX.Direct3D12

open ShaderSupport
open ShaderCompile

open DirectX.Assets
  
// ----------------------------------------------------------------------------------------------------
// ShaderConnector
// ----------------------------------------------------------------------------------------------------
module MyPipelineConfiguration =

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
    type MyPipelineConfiguration(connName:string, inputLayoutDesc:InputLayoutDescription, rootSignatureDescription:RootSignatureDescription, 
        vertexShaderDesc:ShaderDescription, pixelShaderDesc: ShaderDescription, 
        domainShaderDesc: ShaderDescription, hullShaderDesc: ShaderDescription, 
        sampleDesc:SampleDescription, primitiveTopologyType:PrimitiveTopologyType,
        rasterizerStateDesc:RasterizerDescription, blendStateDesc:BlendDescription
        ) =
        let mutable connName=connName
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
        let mutable primitiveTopologyType:PrimitiveTopologyType=primitiveTopologyType
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
            "MyShaderConnector " + connName

        member this.ConnName
            with get() = connName
            
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

        member this.PrimitiveTopologyType
            with get() = primitiveTopologyType
            and set(value) = primitiveTopologyType <- value

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
        psoDesc.PrimitiveTopologyType   <- conf.PrimitiveTopologyType 
        psoDesc.SampleDescription       <- conf.SampleDescription
        psoDesc
 