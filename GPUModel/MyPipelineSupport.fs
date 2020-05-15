namespace GPUModel
//
//  MyPipelineSupport.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//  

open log4net

open System.Collections.Generic 

open SharpDX
open SharpDX.DXGI
open SharpDX.Direct3D12

open Base.Framework
open Base.Logging

open DirectX.Assets

open Shader.ShaderSupport
open GPUModel.MyPipelineConfiguration
  
// ----------------------------------------------------------------------------------------------------
// GPU helper classes
// ----------------------------------------------------------------------------------------------------
module MyPipelineSupport = 

    exception PipelineCreateError of string
    exception PipelineStateNotFoundError of string

    type Device = SharpDX.Direct3D12.Device 
    type Resource = SharpDX.Direct3D12.Resource 
    
    let blendStateFromType(blendType:BlendType)=
        match blendType with 
        | BlendType.Opaque       -> blendStateOpaque
        | BlendType.Transparent  -> blendStateTransparent
        | BlendType.Undefinded   -> blendStateOpaque

    let rasterStateFromType(rasterType:RasterType)=
        match rasterType with 
        | RasterType.Solid      -> rasterizerStateSolid
        | RasterType.Wired      -> rasterizerStateWired
        | RasterType.Undefinded -> rasterizerStateSolid

    // ----------------------------------------------------------------------------------------------------
    //  NestedDict 
    //      Alle Psos werden zu ihren Schlüsseln abgelegt
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type NestedDict<'SHA, 'BLEND, 'RASTR, 'PSO when 'SHA:equality and 'BLEND:equality and 'RASTR:equality> () =
        let mutable vertexDict = Dictionary<'SHA, Dictionary<'SHA, Dictionary<'SHA, Dictionary<'SHA, Dictionary<'BLEND, Dictionary<'RASTR, 'PSO>>>>>>()
        let newPixelDict  () = Dictionary<'SHA, Dictionary<'SHA, Dictionary<'SHA, Dictionary<'BLEND, Dictionary<'RASTR, 'PSO>>>>>()
        let newDomainDict () = Dictionary<'SHA, Dictionary<'SHA, Dictionary<'BLEND, Dictionary<'RASTR, 'PSO>>>>()
        let newHullDict   () = Dictionary<'SHA, Dictionary<'BLEND, Dictionary<'RASTR, 'PSO>>>()
        let newBlendDict  () = Dictionary<'BLEND, Dictionary<'RASTR, 'PSO>>()
        let newRasterDict () = Dictionary<'RASTR, 'PSO>()

        member this.Add(vtx:'SHA, pxl:'SHA, dom:'SHA, hul:'SHA, bld:'BLEND, rastr:'RASTR, result:'PSO) =
            vertexDict.
                TryItem(vtx, newPixelDict()).                
                TryItem(pxl, newDomainDict()).
                TryItem(dom, newHullDict()).
                TryItem(hul, newBlendDict()).
                TryItem(bld, newRasterDict()).
                Replace(rastr, result)

        member this.Item(vtx:'SHA, pxl:'SHA, dom:'SHA, hull:'SHA, bld:'BLEND, rastr:'RASTR) =
            vertexDict.Item(vtx).Item(pxl).Item(dom).Item(hull).Item(bld).Item(rastr)

    // ----------------------------------------------------------------------------------------------------
    //  PSO storage
    //  All psos built and kept in a nested dict
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type PipelineStore(device:Device) =
        let mutable device=device
        let mutable ndict = new NestedDict<ShaderClass, BlendType, RasterType, PipelineState>()
        let logger = LogManager.GetLogger("pipeline.PipelineStore")
        let logDebug = Debug(logger)
        
        member this.AddForPipelineConfig(conf:MyPipelineConfiguration) = 
            logDebug("AddForPipelineConfig " + conf.ConnName)
            let pso = this.buildPso(conf)
            this.Add(
                conf.VertexShaderDesc.Klass ,
                conf.PixelShaderDesc.Klass ,
                conf.DomainShaderDesc.Klass ,
                conf.HullShaderDesc.Klass ,
                conf.BlendStateDesc.Type  ,
                conf.RasterizerStateDesc.Type,
                pso
            )

        member this.buildPso(conf:MyPipelineConfiguration) =
            let psoDesc = psoDescFromConfiguration(device, conf)
            try                 
                let pso = device.CreateGraphicsPipelineState(psoDesc)
                //logger.Debug("Pso successfully built")
                pso
            with :? SharpDXException as ex -> 
                logger.Fatal("Pipeline createError " + conf.ToString() + "\n" )
                raise (PipelineCreateError("Pipeline create error " + ex.Message))
                null

        member this.Add(vtx:ShaderClass, pxl:ShaderClass, dom:ShaderClass, hull:ShaderClass, bld:BlendType, rastr:RasterType, pso)=
            ndict.Add(vtx, pxl, dom, hull, bld, rastr, pso)

        member this.Get(vtx:ShaderClass, pxl:ShaderClass, dom:ShaderClass, hull:ShaderClass, bld:BlendType, rastr:RasterType) =
            try
                let result = ndict.Item(vtx, pxl, dom, hull, bld, rastr)
                //logDebug("Successfully found")
                result
            with 
            | :? KeyNotFoundException as ex -> 
                logDebug("Not found")
                raise (PipelineStateNotFoundError(ex.Message))  
                null

    // ----------------------------------------------------------------------------------------------------
    //  Client Interface
    // Control access to pipelinestates
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type PipelineProvider(device:Device) =
        let mutable device=device 
        let mutable rootSignature:RootSignature=null
        let mutable vertexShaderDesc:ShaderDescription=null
        let mutable pixelShaderDesc:ShaderDescription=null
        let mutable domainShaderDesc:ShaderDescription=null
        let mutable hullShaderDesc:ShaderDescription=null
        let mutable rasterizerDesc:RasterizerDescription=null
        let mutable blendDesc:BlendDescription=null
        let mutable primitiveTopologyType:PrimitiveTopologyType=PrimitiveTopologyType.Triangle
        let mutable isDirty=true
        let mutable psoStore:PipelineStore=null
        let mutable CurrentPipelineState:PipelineState=null
        let logger = LogManager.GetLogger("pipeline.PipelineProvider")
        let logDebug = Debug(logger)
        let logInfo  = Info(logger)

        do            
            rootSignature <- createRootSignature(device, rootSignatureDescCookBook)
            psoStore <- new PipelineStore(device)

        override this.ToString() =
            "Provider " + vertexShaderDesc.Klass.ToString() + ":" + pixelShaderDesc.Klass.ToString() + ":" + domainShaderDesc.Klass.ToString() + ":" + hullShaderDesc.Klass.ToString()
             + ":" + rasterizerDesc.Type.ToString() + ":" + blendDesc.Type.ToString()

        member this.ActivateConfig(config:MyPipelineConfiguration) =
            vertexShaderDesc        <- config.VertexShaderDesc
            pixelShaderDesc         <- config.PixelShaderDesc 
            domainShaderDesc        <- config.DomainShaderDesc 
            hullShaderDesc          <- config.HullShaderDesc 
            blendDesc               <- config.BlendStateDesc 
            rasterizerDesc          <- config.RasterizerStateDesc 
            primitiveTopologyType   <- config.PrimitiveTopologyType 
            logDebug("Activated Config  "  + this.ToString())
            isDirty <- true
            
        member this.GetPipelineState() =
            if isDirty then
                try 
                    CurrentPipelineState <- 
                        psoStore.Get(
                            vertexShaderDesc.Klass,
                            pixelShaderDesc.Klass,
                            domainShaderDesc.Klass,
                            hullShaderDesc.Klass,
                            blendDesc.Type,
                            rasterizerDesc.Type
                        )
                with 
                | :? PipelineStateNotFoundError -> 
                    let pso = psoStore.buildPso(this.CurrentConfiguration())
                    psoStore.Add( 
                        vertexShaderDesc.Klass,
                        pixelShaderDesc.Klass,
                        domainShaderDesc.Klass,
                        hullShaderDesc.Klass,
                        blendDesc.Type ,
                        rasterizerDesc.Type,
                        pso
                    )
                    CurrentPipelineState <- pso
                isDirty <- false

            CurrentPipelineState

        member this.PixelShaderDesc
            with get() = pixelShaderDesc
            and set(value) = 
                isDirty <- true
                pixelShaderDesc <- value  

        member this.BlendDesc 
            with get() = blendDesc 
            and set(value) = 
                isDirty <- true
                blendDesc <- value

        member this.RasterizerDesc 
            with get() = rasterizerDesc
            and set(value) = 
                isDirty <- true
                rasterizerDesc <- value

        member this.RootSignature
            with get() = rootSignature

        member this.CurrentConfiguration () =
            new MyPipelineConfiguration(
                configName="Provider",
                inputLayoutDesc=layoutCookBook,
                rootSignatureDescription=rootSignatureDescCookBook,
                vertexShaderDesc=vertexShaderDesc,
                pixelShaderDesc=pixelShaderDesc,
                domainShaderDesc=domainShaderDesc,
                hullShaderDesc=hullShaderDesc,
                sampleDesc = new SampleDescription(1, 0),
                primitiveTopologyType=primitiveTopologyType,
                blendStateDesc=blendDesc,
                rasterizerStateDesc=rasterizerDesc
            )