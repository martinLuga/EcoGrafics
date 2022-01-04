namespace GPUModel
//
//  MyPipelineSupport.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//  

open log4net

open SharpDX.DXGI
open SharpDX.Direct3D
open SharpDX.Direct3D12

open Base.LoggingSupport
open Base.ShaderSupport

open DirectX.Assets

open MyPipelineStore
  
// ----------------------------------------------------------------------------------------------------
// GPU helper classes
// ----------------------------------------------------------------------------------------------------
module MyPipelineSupport = 

    exception PipelineCreateError of string

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


    let loggerProvider = LogManager.GetLogger("Pipeline.Provider")
    
    let logDebug = Debug(loggerProvider)
    let logInfo  = Info(loggerProvider)
    let logError = Error(loggerProvider)

    // ----------------------------------------------------------------------------------------------------
    //  Client Interface
    // Control access to pipelinestates
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type PipelineProvider(device:Device) =

        let mutable isDirty=true
        let mutable psoStore:PipelineStore=null
        let mutable currentPipelineState:PipelineState=null
        let mutable initialPipelineState:PipelineState=null

        let mutable device=device 
        let mutable inputLayoutDesc:InputLayoutDescription=null
        let mutable rootSignatureDesc:RootSignatureDescription=null

        let mutable rootSignature:RootSignature=null

        let mutable vertexShaderDesc:ShaderDescription=null
        let mutable pixelShaderDesc:ShaderDescription=null
        let mutable domainShaderDesc:ShaderDescription=null
        let mutable hullShaderDesc:ShaderDescription=null
        let mutable rasterizeStateDesc:RasterizerStateDescription=RasterizerStateDescription.Default()
        let mutable blendStateDesc:BlendStateDescription=BlendStateDescription.Default()
        let mutable topologyType=PrimitiveTopologyType.Triangle
        let mutable topology=PrimitiveTopology.TriangleList

        do            
            rootSignature <- createRootSignature(device, rootSignatureDescCookBook) 
            psoStore <- new PipelineStore(device)

        override this.ToString() =
               " V:" + vertexShaderDesc.Klass.ToString() + " P:" + pixelShaderDesc.Klass.ToString() + " D:" + domainShaderDesc.Klass.ToString() + " H:" + hullShaderDesc.Klass.ToString()
             + " R:" + rasterizeStateDesc.ToString() + " B:" + blendStateDesc.ToString() + " T:" + topologyType.ToString()

        member this.Initialize(_InputLayoutDesc, _RootSignatureDesc, _VertexShaderDesc, _PixelShaderDesc, _DomainShaderDesc, _HullShaderDesc,  _SampleDesc, _BlendStateDesc,  _RasterizerStateDesc, _TopologyType) =
            inputLayoutDesc     <- _InputLayoutDesc
            rootSignatureDesc   <- _RootSignatureDesc
            rootSignature       <- createRootSignature(device, rootSignatureDesc) 
            this.Add (_VertexShaderDesc, _PixelShaderDesc, _DomainShaderDesc, _HullShaderDesc, _BlendStateDesc, _RasterizerStateDesc,_TopologyType)
            try 
                this.InitialPipelineState <- 
                    psoStore.Get(
                        _VertexShaderDesc.ToString(),
                        _PixelShaderDesc.ToString(),
                        _DomainShaderDesc.ToString(),
                        _HullShaderDesc.ToString(),
                        _BlendStateDesc,
                        _RasterizerStateDesc,
                        _TopologyType 
                    )
            with 
            | :? PipelineStateNotFoundException -> 
                logError("PipelineProvider not correctly initialized ")

        member this.Add (vertexShaderDesc, pixelShaderDesc, domainShaderDesc, hullShaderDesc, blendStateDesc,  rasterizerStateDesc,topologyType) =
            let pso = psoStore.buildPso(inputLayoutDesc, rootSignatureDesc, vertexShaderDesc, pixelShaderDesc, domainShaderDesc, hullShaderDesc, blendStateDesc,  rasterizerStateDesc,topologyType)
            psoStore.Add( 
                vertexShaderDesc.ToString(),
                pixelShaderDesc.ToString(),
                domainShaderDesc.ToString(),
                hullShaderDesc.ToString(),
                blendStateDesc,
                rasterizerStateDesc,
                topologyType,
                pso
            )

        // 
        //  PipelineState
        // 
        member this.GetCurrentPipelineState() =
            if isDirty then
                try 
                    currentPipelineState <- 
                        psoStore.Get(
                            vertexShaderDesc.ToString(),
                            pixelShaderDesc.ToString(),
                            domainShaderDesc.ToString(),
                            hullShaderDesc.ToString(),
                            blendStateDesc,
                            rasterizeStateDesc,
                            topologyType
                        )
                    logDebug("PSO retrieved for  " + this.ToString())
                with 
                    | :? PipelineStateNotFoundException -> 
                        logDebug("PSO Not found : " + this.ToString())
                                        
                        let pso = psoStore.buildPso(inputLayoutDesc, rootSignatureDesc, vertexShaderDesc, pixelShaderDesc, domainShaderDesc, hullShaderDesc, blendStateDesc,  rasterizeStateDesc, topologyType)
                    
                        psoStore.Add( 
                            vertexShaderDesc.ToString(),
                            pixelShaderDesc.ToString(),
                            domainShaderDesc.ToString(),
                            hullShaderDesc.ToString(),
                            blendStateDesc,
                            rasterizeStateDesc,
                            topologyType,
                            pso
                        )
                        currentPipelineState <- pso
                isDirty <- false
            currentPipelineState

        // 
        //  MEMBER
        // 
        member this.InitialPipelineState
            with get() = initialPipelineState
            and set(value) = initialPipelineState <- value

        member this.InputLayoutDesc
            with get() = inputLayoutDesc
            and set(value) = 
                if inputLayoutDesc <> value then
                    isDirty <- true
                inputLayoutDesc <- value 

        member this.RootSignatureDesc
            with get() = rootSignatureDesc
            and set(value) = 
                if rootSignatureDesc <> value then
                    isDirty <- true
                rootSignatureDesc <- value 

        member this.VertexShaderDesc
            with get() = vertexShaderDesc
            and set(value) = 
                if vertexShaderDesc <> value then
                    isDirty <- true
                vertexShaderDesc <- value 

        member this.PixelShaderDesc
            with get() = pixelShaderDesc
            and set(value) = 
                if pixelShaderDesc <> value then
                    isDirty <- true
                pixelShaderDesc <- value  

        member this.DomainShaderDesc
            with get() = domainShaderDesc
            and set(value) = 
                if domainShaderDesc <> value then
                    isDirty <- true
                domainShaderDesc <- value  
    
        member this.HullShaderDesc
            with get() = hullShaderDesc
            and set(value) = 
                if hullShaderDesc <> value then
                    isDirty <- true
                hullShaderDesc <- value  

        member this.BlendDesc 
            with get() = blendStateDesc 
            and set(value) = 
                if blendStateDesc <> value then
                    isDirty <- true
                blendStateDesc <- value

        member this.RasterizerDesc 
            with get() = rasterizeStateDesc
            and set(value) = 
                if rasterizeStateDesc <> value then
                    isDirty <- true
                rasterizeStateDesc <- value

        member this.TopologyType 
            with get() = topologyType 
            and set(value) = 
                if topologyType <> value then
                    isDirty <- true
                topologyType <- value

        member this.Topology  
            with get() = topology  
            and set(value) = 
                if topology  <> value then
                    isDirty <- true
                topology  <- value

        member this.RootSignature
            with get() = rootSignature
