namespace GPUModel
//
//  MyPipelineStore.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//  

open System

open log4net

open System.Collections.Generic 

open SharpDX
open SharpDX.DXGI
open SharpDX.Direct3D12

open Base.Framework
open Base.LoggingSupport
open Base.ShaderSupport
open Base.ShaderCompile

open DirectX.Assets
  
// ----------------------------------------------------------------------------------------------------
// Wrapper und Cache für Pipelinestates
// ----------------------------------------------------------------------------------------------------
module MyPipelineStore = 

    let loggerProvider = LogManager.GetLogger("Pipeline.Store")
    let logDebug = Debug(loggerProvider)
    let logInfo  = Info(loggerProvider)
    let logError = Error(loggerProvider)

    exception PipelineCreateError of string
    exception PipelineStateNotFoundException of string

    type Device = SharpDX.Direct3D12.Device 
    type Resource = SharpDX.Direct3D12.Resource 

    let psoDesc(device, inputLayoutDesc:InputLayoutDescription, rootSignatureDesc:RootSignatureDescription, vertexShaderDesc:ShaderDescription, pixelShaderDesc:ShaderDescription, domainShaderDesc:ShaderDescription, hullShaderDesc:ShaderDescription, blendStateDesc , rasterizerStateDesc, topologyType, sampleDescription:SampleDescription) =  
        let mutable psoDesc =  
            new GraphicsPipelineStateDescription( 
                DepthStencilState = DepthStencilStateDescription.Default(),
                SampleMask = Int32.MaxValue,
                RenderTargetCount = 1,  
                StreamOutput=StreamOutputDescription(),
                DepthStencilFormat = DEPTHSTENCILFORMAT
            )
        psoDesc.InputLayout             <- inputLayoutDesc
        psoDesc.RootSignature           <- createRootSignature(device, rootSignatureDesc) 
        if vertexShaderDesc.IsSet()  then
            psoDesc.VertexShader        <- shaderFromDescription(vertexShaderDesc)  
        if  pixelShaderDesc.IsSet()  then 
            psoDesc.PixelShader         <- shaderFromDescription(pixelShaderDesc)   
        if  domainShaderDesc.IsSet()  then
            psoDesc.DomainShader        <- shaderFromDescription(domainShaderDesc) 
        if  hullShaderDesc.IsSet()  then  
            psoDesc.HullShader          <- shaderFromDescription(hullShaderDesc) 
        psoDesc.BlendState              <- blendStateDesc   
        psoDesc.RasterizerState         <- rasterizerStateDesc 
        psoDesc.PrimitiveTopologyType   <- topologyType
        psoDesc.SampleDescription       <- sampleDescription
        psoDesc
    
    // ----------------------------------------------------------------------------------------------------
    //  NestedDict: Cache für Pipelinestates. Alle Psos werden zu ihren Schlüsseln abgelegt
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type NestedDict<'SHA, 'BLEND, 'RASTR, 'TOPOL, 'PSO when 'SHA:equality and 'BLEND:equality and 'RASTR:equality and 'TOPOL:equality> () =
        let mutable vertexDict = Dictionary<'SHA,   Dictionary<'SHA,    Dictionary<'SHA,    Dictionary<'SHA, Dictionary<'BLEND, Dictionary<'RASTR, Dictionary<'TOPOL, 'PSO>>>>>>>()
        let newPixelDict    () = Dictionary<'SHA,   Dictionary<'SHA,    Dictionary<'SHA,    Dictionary<'BLEND, Dictionary<'RASTR, Dictionary<'TOPOL, 'PSO>>>>>>()
        let newDomainDict   () = Dictionary<'SHA,   Dictionary<'SHA,    Dictionary<'BLEND,  Dictionary<'RASTR, Dictionary<'TOPOL, 'PSO>>>>>()
        let newHullDict     () = Dictionary<'SHA,   Dictionary<'BLEND,  Dictionary<'RASTR,  Dictionary<'TOPOL, 'PSO>>>>()
        let newBlendDict    () = Dictionary<'BLEND, Dictionary<'RASTR,  Dictionary<'TOPOL,'PSO>>>()
        let newRasterDict   () = Dictionary<'RASTR, Dictionary<'TOPOL,'PSO>>()
        let newTopolDict    () = Dictionary<'TOPOL, 'PSO>()

        member this.Add(vtx:'SHA, pxl:'SHA, dom:'SHA, hul:'SHA, bld:'BLEND, rastr:'RASTR, topol:'TOPOL, result:'PSO) =
            vertexDict.
                TryItem(vtx,   newPixelDict()).                
                TryItem(pxl,   newDomainDict()).
                TryItem(dom,   newHullDict()).
                TryItem(hul,   newBlendDict()).
                TryItem(bld,   newRasterDict()).
                TryItem(rastr, newTopolDict()).
                Replace(topol, result)

        member this.Item(vtx:'SHA, pxl:'SHA, dom:'SHA, hull:'SHA, bld:'BLEND, rastr:'RASTR, topol:'TOPOL) =
            vertexDict.Item(vtx).Item(pxl).Item(dom).Item(hull).Item(bld).Item(rastr).Item(topol)

    // ----------------------------------------------------------------------------------------------------
    //  PSO storage: All psos built and kept in a nested dict
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type PipelineStore(device:Device) =
        let mutable device=device
        let mutable ndict = new NestedDict<string, string, string, PrimitiveTopologyType, PipelineState>()
        
        member this.buildPso(inputLayoutDesc, rootSignatureDesc, vertexShaderDesc, pixelShaderDesc, domainShaderDesc, hullShaderDesc, blendStateDesc, rasterizerStateDesc:RasterizerStateDescription, topologyType, isCube) =
            let psoDesc = 
                psoDesc(
                    device,
                    inputLayoutDesc,
                    rootSignatureDesc,
                    vertexShaderDesc,
                    pixelShaderDesc,
                    domainShaderDesc,
                    hullShaderDesc,
                    blendStateDesc, 
                    rasterizerStateDesc,
                    topologyType,
                    SampleDescription(1, 0)
                    )
            if isCube then
                psoDesc.DepthStencilState.DepthComparison <- Comparison.LessEqual
                psoDesc.RasterizerState.CullMode <- CullMode.None

            try                 
                let pipelineState = device.CreateGraphicsPipelineState(psoDesc)
                pipelineState
            with :? SharpDXException as ex -> 
                logError("Pipelinestate createError "  + "\n"  + ex.Message + "\n")
                raise (PipelineCreateError("Pipelinestate create error " + ex.Message))
                null

        member this.Add(vtx:string, pxl:string, dom:string, hull:string, bld:string, rastr:string, topo:PrimitiveTopologyType,  pso)=
            ndict.Add(vtx, pxl, dom, hull, bld, rastr, topo, pso)

        member this.Get(vtx:string, pxl:string, dom:string, hull:string, bld:string, rastr:string, topo:PrimitiveTopologyType) =
            try
                let result = ndict.Item(vtx, pxl, dom, hull, bld, rastr, topo)
                result
            with 
            | :? KeyNotFoundException as ex -> 
                raise (PipelineStateNotFoundException(ex.Message))  
                null