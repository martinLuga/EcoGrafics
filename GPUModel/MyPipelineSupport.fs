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
open Base.LoggingSupport

open DirectX.Assets

open Shader.ShaderSupport
open GPUModel.MyPipelineConfiguration
  
// ----------------------------------------------------------------------------------------------------
// GPU helper classes
// ----------------------------------------------------------------------------------------------------
module MyPipelineSupport = 

    let logger = LogManager.GetLogger("Pipeline")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)

    exception PipelineCreateError of string
    exception PipelineStateNotFoundException of string

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
    //  PSO storage
    //  All psos built and kept in a nested dict
    // ----------------------------------------------------------------------------------------------------
    let loggerPSO = LogManager.GetLogger("Pipeline.Store")

    [<AllowNullLiteralAttribute>]
    type PipelineStore(device:Device) =
        let mutable device=device
        let mutable ndict = new NestedDict<ShaderClass, BlendType, RasterType, TopologyType, PipelineState>()
        let logDebug = Debug(loggerPSO)
        
        member this.AddForPipelineConfig(conf:MyPipelineConfiguration) = 
            logDebug("AddForPipelineConfig " + conf.ConfigName)
            let pso = this.buildPso(conf)
            this.Add(
                conf.VertexShaderDesc.Klass ,
                conf.PixelShaderDesc.Klass ,
                conf.DomainShaderDesc.Klass ,
                conf.HullShaderDesc.Klass ,
                conf.BlendStateDesc.Type  ,
                conf.RasterizerStateDesc.Type,
                conf.TopologyTypeDesc.Type,
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

        member this.Add(vtx:ShaderClass, pxl:ShaderClass, dom:ShaderClass, hull:ShaderClass, bld:BlendType, rastr:RasterType, topo:TopologyType, pso)=
            ndict.Add(vtx, pxl, dom, hull, bld, rastr, topo, pso)

        member this.Get(vtx:ShaderClass, pxl:ShaderClass, dom:ShaderClass, hull:ShaderClass, bld:BlendType, rastr:RasterType, topo:TopologyType) =
            try
                let result = ndict.Item(vtx, pxl, dom, hull, bld, rastr, topo)
                result
            with 
            | :? KeyNotFoundException as ex -> 
                raise (PipelineStateNotFoundException(ex.Message))  
                null

    let loggerProvider = LogManager.GetLogger("Pipeline.Provider")
    // ----------------------------------------------------------------------------------------------------
    //  Client Interface
    // Control access to pipelinestates
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type PipelineProvider(device:Device) =
        let mutable device=device 
        let mutable configName=""
        let mutable rootSignature:RootSignature=null
        let mutable vertexShaderDesc:ShaderDescription=null
        let mutable pixelShaderDesc:ShaderDescription=null
        let mutable domainShaderDesc:ShaderDescription=null
        let mutable hullShaderDesc:ShaderDescription=null
        let mutable rasterizerDesc:RasterizerDescription=null
        let mutable blendDesc:BlendDescription=null
        let mutable topologyTypeDesc:TopologyTypeDescription=null
        let mutable isDirty=true
        let mutable psoStore:PipelineStore=null
        let mutable currentPipelineState:PipelineState=null
        let mutable initialPipelineState:PipelineState=null

        let logDebug = Debug(loggerProvider)
        let logInfo  = Info(loggerProvider)
        let logError = Error(loggerProvider)

        do            
            rootSignature <- createRootSignature(device, rootSignatureDescCookBook) 
            psoStore <- new PipelineStore(device)

        override this.ToString() =
            "P:" + configName + " V:" + vertexShaderDesc.Klass.ToString() + " P:" + pixelShaderDesc.Klass.ToString() + " D:" + domainShaderDesc.Klass.ToString() + " H:" + hullShaderDesc.Klass.ToString()
             + " R:" + rasterizerDesc.Type.ToString() + " B:" + blendDesc.Type.ToString() + " T:" + topologyTypeDesc.Type.ToString()

        // 
        //  CONFIG
        // 
        member this.CurrentConfiguration () =
            new MyPipelineConfiguration(
                configName="Provider",
                inputLayoutDesc=layoutCookBook,
                rootSignatureDescription=rootSignatureDescCookBook,
                vertexShaderDesc=vertexShaderDesc,
                pixelShaderDesc=pixelShaderDesc,
                domainShaderDesc=domainShaderDesc,
                hullShaderDesc=hullShaderDesc,
                sampleDesc=new SampleDescription(1, 0),
                topologyTypeDesc=topologyTypeDesc,
                blendStateDesc=blendDesc,
                rasterizerStateDesc=rasterizerDesc
            )

        member this.ActivateConfig(config:MyPipelineConfiguration) =
            configName              <- config.ConfigName
            vertexShaderDesc        <- config.VertexShaderDesc
            pixelShaderDesc         <- config.PixelShaderDesc 
            domainShaderDesc        <- config.DomainShaderDesc 
            hullShaderDesc          <- config.HullShaderDesc 
            blendDesc               <- config.BlendStateDesc 
            rasterizerDesc          <- config.RasterizerStateDesc
            topologyTypeDesc        <- config.TopologyTypeDesc
            logDebug("Activate Config  "  + this.ToString())
            isDirty <- true

        member this.AddConfig(config:MyPipelineConfiguration) =
            let pso = psoStore.buildPso(config)
            psoStore.Add( 
                config.VertexShaderDesc.Klass,
                config.PixelShaderDesc.Klass,
                config.DomainShaderDesc.Klass,
                config.HullShaderDesc.Klass,
                config.BlendStateDesc.Type ,
                config.RasterizerStateDesc.Type,
                config.TopologyTypeDesc.Type,
                pso
            )
            logDebug("PSO Added : " + config.ToString())
        
        member this.SetInitialConfig(config:MyPipelineConfiguration) =
            try 
                this.InitialPipelineState <- 
                    psoStore.Get(
                        config.VertexShaderDesc.Klass,
                        config.PixelShaderDesc.Klass,
                        config.DomainShaderDesc.Klass,
                        config.HullShaderDesc.Klass,
                        config.BlendStateDesc.Type ,
                        config.RasterizerStateDesc.Type,
                        config.TopologyTypeDesc.Type
                    )
            with 
            | :? PipelineStateNotFoundException -> 
                logError("PipelineProvider not correctly initialized ")
            
        // 
        //  PipelineState
        // 
        member this.GetCurrentPipelineState() =
            if isDirty then
                try 
                    currentPipelineState <- 
                        psoStore.Get(
                            vertexShaderDesc.Klass,
                            pixelShaderDesc.Klass,
                            domainShaderDesc.Klass,
                            hullShaderDesc.Klass,
                            blendDesc.Type,
                            rasterizerDesc.Type,
                            topologyTypeDesc.Type
                        )
                    logDebug("PSO retrieved for  " + this.ToString())
                with 
                | :? PipelineStateNotFoundException -> 
                    logDebug("PSO Not found : " + this.ToString())
                    let pso = psoStore.buildPso(this.CurrentConfiguration())
                    psoStore.Add( 
                        vertexShaderDesc.Klass,
                        pixelShaderDesc.Klass,
                        domainShaderDesc.Klass,
                        hullShaderDesc.Klass,
                        blendDesc.Type ,
                        rasterizerDesc.Type,
                        topologyTypeDesc.Type,
                        pso
                    )
                    currentPipelineState <- pso
                isDirty <- false
            currentPipelineState

        member this.GetPipelineState( pipelineConfigName:string, objectPixelShaderDesc:ShaderDescription, objectBlendDesc:BlendDescription) =             
            logInfo("Get Pipeline State " + pipelineConfigName + objectPixelShaderDesc.Klass.ToString()  + objectBlendDesc.Type.ToString() + rasterizerDesc.Type.ToString())
            psoStore.Get(
                vertexShaderDesc.Klass,
                objectPixelShaderDesc.Klass,
                domainShaderDesc.Klass,
                hullShaderDesc.Klass,
                objectBlendDesc.Type,
                rasterizerDesc.Type,
                topologyTypeDesc.Type
            )

        member this.GetActualPipelineState() = 
            logInfo("Get Actual Pipeline State " )
            psoStore.Get(
                vertexShaderDesc.Klass,
                pixelShaderDesc.Klass,
                domainShaderDesc.Klass,
                hullShaderDesc.Klass,
                blendDesc.Type,
                rasterizerDesc.Type,
                topologyTypeDesc.Type
            ) 

        member this.InitialPipelineState
            with get() = initialPipelineState
            and set(value) = initialPipelineState <- value

        // 
        //  MEMBER
        // 
        member this.PixelShaderDesc
            with get() = pixelShaderDesc
            and set(value) = 
                if pixelShaderDesc <> value then
                    isDirty <- true
                pixelShaderDesc <- value  

        member this.BlendDesc 
            with get() = blendDesc 
            and set(value) = 
                if blendDesc <> value then
                    isDirty <- true
                blendDesc <- value

        member this.RasterizerDesc 
            with get() = rasterizerDesc
            and set(value) = 
                if rasterizerDesc <> value then
                    isDirty <- true
                rasterizerDesc <- value

        member this.TopologyTypeDesc 
            with get() = topologyTypeDesc 
            and set(value) = 
                if topologyTypeDesc <> value then
                    isDirty <- true
                topologyTypeDesc <- value

        member this.RootSignature
            with get() = rootSignature

        member this.ConfigName
            with get() = configName
            and set(value) = configName <- value

