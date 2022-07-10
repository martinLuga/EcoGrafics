namespace GPUModel
//
//  MyGPU.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 

open log4net

open System
open System.Threading 
open System.Windows.Forms
open System.Collections.Generic 

open SharpDX
open SharpDX.Mathematics.Interop
open SharpDX.Direct3D 
open SharpDX.Direct3D12
open SharpDX.DXGI

open Base.LoggingSupport
open Base.ShaderSupport
open Base.VertexDefs
open Base.ModelSupport
open DirectX.GraficUtils
open DirectX.BitmapSupport

open MyFrame
open MyPipelineSupport
open MYUtils
open MyGPUInfrastructure
  
// ----------------------------------------------------------------------------------------------------
// GPU Abstraction
// ----------------------------------------------------------------------------------------------------
module MyGPU = 

    type MeshGeometry= MyMesh.MeshGeometry<Vertex,int>    

    let TEXTUREWIDTH = 256; 
    let TEXTUREHEIGHT = 256 
    let TEXTUREPIXELSIZE = 4    // The number of bytes used to represent a pixel in the texture.
    let FRAMECOUNT = 2 
    let NUMFRAMERESOURCES = 5 
    let SWAPCHAINBUFFERCOUNT = 2 
    let DSVDESCRIPTORCOUNT = 1
    let BACKBUFFERFORMAT = Format.R8G8B8A8_UNorm  
    let RTVDESCRIPTORCOUNT = SWAPCHAINBUFFERCOUNT 

    let ROP_IDX_OBJECT    = 0
    let ROP_IDX_FRAME     = 1
    let ROP_IDX_MATERIAL  = 2
    let ROP_IDX_ARMATURE  = 3
    let ROP_IDX_CUBE      = 4 
    let ROP_IDX_TEXTURE   = 5 

    let logger   = LogManager.GetLogger("GPU")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)
    
    // ----------------------------------------------------------------------------------------------------
    //  Class  MyGPU 
    //      Init
    //      Configure
    //      Draw
    //      Update
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>] 
    type MasterGPU() =

        // Window
        let mutable viewport = new RawViewportF()
        let mutable scissorRectangels:RawRectangle[] = Array.create 1 (new RawRectangle())
        let mutable clientWidth  = 1100 
        let mutable clientHeight = 850       
        let mutable clearColor:RawColor4 = new RawColor4(0.0f, 0.0f, 0.0f, 1.0f) 

        // Shaders
        let mutable pipelineConfigurations=new Dictionary<string, ShaderConfiguration>() 
        let mutable currentPipelineConfigurationName="Basic"
        let mutable pixelShaderDesc:ShaderDescription=null

        // Geometry        
        let mutable meshCache:MeshCache<Vertex> = null
        let mutable geometry:MeshGeometry = null
        let mutable hasCube = false

        // Resources     
        let mutable textures = new Dictionary<string, int>()
        let mutable textureHeapWrapper:HeapWrapper = null
        let mutable textureHeapWrapperCube:HeapWrapper = null
        let mutable bitmapManager = BitmapManager(device)  

        // Pipeline
        let mutable pipelineProvider:PipelineProvider=null
        let mutable directRecorder:Recorder = null
        let mutable directFrameResource:FrameResource = null
        let mutable frameResources = new List<FrameResource>(NUMFRAMERESOURCES)
        let mutable currentFrameResourceIndex = 0 
        let mutable itemLength = 0 
        let mutable matLength = 0 
        let mutable frameLength = 0 

        // Display
        let mutable rasterizerDesc=RasterizerDescription.Default()
        let mutable blendDesc=BlendDescription.Default()

        // Synchronization objects.
        let mutable coordinator:ProcessorCoordinator = null

        override this.ToString() = "MyGPU: " + pipelineProvider.ToString()

        // ----------------------------------------------------------------------------------------------------
        // Member
        // ----------------------------------------------------------------------------------------------------
        member this.AspectRatio =
            (float) clientWidth / (float) clientHeight

        member this.ClearColor
            with get() = clearColor
            and set(value) = clearColor <- value 

        member this.Device
            with get() = device

        member this.Geometry
            with get() = geometry

        member this.ItemLength
            with get() = itemLength
            and set(value) = itemLength <- value 
 
        member this.MatLength
            with get() = matLength
            and set(value) = matLength <- value 

        member this.FrameLength
            with get() = frameLength
            and set(value) = frameLength <- value             
        
        member this.PixelShaderDesc
            with get() = pixelShaderDesc
            and set(value) = 
                pixelShaderDesc <- value
                pipelineProvider.PixelShaderDesc <- pixelShaderDesc
        
        member this.BlendDesc
            with get() = blendDesc
            and set(value) = 
                blendDesc <- value                
                pipelineProvider.BlendDesc <- blendDesc
            
        member this.RasterizerDesc
            with get() = rasterizerDesc
            and set(value) =
                rasterizerDesc <- value
                pipelineProvider.RasterizerDesc <- value 

        member this.FrameResources
            with get() =  frameResources
            and set(value) = frameResources <- value 

        member this.DirectFrameResource
            with get() = directFrameResource
            and set(value) = directFrameResource <- value

        member this.DirectRecorder 
            with get() = directRecorder
            and set(value) = directRecorder <- value 

        member this.ClientHeight 
            with get() = clientHeight

        member this.ClientWidth
            with get() = clientWidth

        member this.TextureHeapWrapper = textureHeapWrapper

        member this.MeshCache = meshCache

        member this.Viewport = viewport

        member this.ScissorRectangels = scissorRectangels

        member this.PipelineProvider = pipelineProvider

        member this.CurrentFrameResourceIndex =  currentFrameResourceIndex 

        member this.CurrFrameResource = frameResources.[currentFrameResourceIndex]
        
        member this.CurrentFenceEvent = fenceEvents.[currentFrameResourceIndex]

        member this.CurrentBackBuffer = swapChainBuffers.[swapChain.CurrentBackBufferIndex]
        
        member this.CurrentBackBufferView = rtvHeap.CPUDescriptorHandleForHeapStart + swapChain.CurrentBackBufferIndex * rtvDescriptorSize
        
        member this.DepthStencilView = dsvHeap.CPUDescriptorHandleForHeapStart 

        member this.CurrentPipelineConf = pipelineConfigurations.Item(currentPipelineConfigurationName)

        member this.Coordinator         
            with get() = coordinator
            and set(value) = coordinator <- value

        // ----------------------------------------------------------------------------------------------------
        // Methoden
        // ----------------------------------------------------------------------------------------------------
        
        member this.Begin() =
            logInfo("Begin")  
            this.FlushCommandQueue()  

        member this.FlushCommandQueue() = 
            coordinator.AdvanceCPU()
            coordinator.AdvanceGPU()
            coordinator.Wait()

        // ----------------------------------------------------------------------------------------------------
        // Initialisierungen
        // ----------------------------------------------------------------------------------------------------

        // 
        // Klasse
        // 
        member this.Initialize(form:UserControl) =

            this.InitGPU(form)

            clientWidth     <- form.ClientSize.Width  
            clientHeight    <- form.ClientSize.Height 
            clearColor      <- ToRawColor4FromDrawingColor(form.BackColor)            
            let vp          =  new ViewportF(0.0f, 0.0f, (float32)clientWidth, (float32)clientHeight, 0.0f, 1.0f) 
            viewport        <- ToRawViewport(vp)
            let sr          = new RectangleF(0.0f, 0.0f, (float32)clientWidth, (float32)clientHeight)
            scissorRectangels.[0] <- ToRawRectangle(sr)            
            this.Size(form.ClientSize.Width, form.ClientSize.Height)

        // 
        // GPU 
        // 
        abstract InitGPU:UserControl-> unit
        default this.InitGPU(form:UserControl) = 

            // Device & Co
            InitDirect3D(form, clientWidth, clientHeight)

            BuildDescriptorHeaps()
                        
            this.FrameResources <- new List<FrameResource>(NUMFRAMERESOURCES)

            // DescriptorHeaps             
            textureHeapWrapper <- new HeapWrapper(device, srvDescriptorHeap)
            textureHeapWrapperCube <- new HeapWrapper(device, srvDescriptorHeap)
            bitmapManager <- BitmapManager(device)  

             // Recorder: Command processing     
            directRecorder <- new Recorder("Direct recording", device, commandQueue, null)
            
            // Coordinator: Synchronization 
            coordinator <- new ProcessorCoordinator(commandQueue, fence)

            // Geometry / Vertex Cache
            this.initializeMeshCache()

            // PipelineProvider
            pipelineProvider <- new PipelineProvider(device)

        // ----------------------------------------------------------------------------------------------------
        // Install
        // ----------------------------------------------------------------------------------------------------

        // 
        // Objects 
        // 
        member this.StartInstall()=
            directRecorder.StartRecording() 

        abstract PrepareInstall:int*int->Unit
        default this.PrepareInstall(anzObjects, anzMaterials) =
            logger.Info("Install " + anzObjects.ToString() + " objects for display ") 
            this.BuildFrameResources(anzObjects, anzMaterials)
            this.resetMeshCache()

        member this.ExecuteInstall()=
            directRecorder.StopRecording()
            directRecorder.Play()

        // ----------------------------------------------------------------------------------------------------
        // MeshData<Vertex>
        // ----------------------------------------------------------------------------------------------------        
        member this.hasMesh(name) =
            meshCache.Contains(name) 

        member this.InstallMesh(name, vertices, indices, topology) =
            meshCache.Append(name, vertices, indices, topology) 

        member this.ReplaceMesh(name, vertices) =
            this.StartInstall()
            meshCache.Replace(name, vertices)  
            this.FinalizeMeshCache()
            this.ExecuteInstall()

        member this.ResetAllMeshes() =
            directRecorder.StartRecording()
            this.resetMeshCache()
            directRecorder.StopRecording()
            directRecorder.Play()

        member this.initializeMeshCache() =
            meshCache <- new MeshCache<Vertex>(device)

        member this.resetMeshCache() =
            meshCache.Reset()

        member this.FinalizeMeshCache() =
            meshCache.createBuffers(directRecorder.CommandList)

        // ----------------------------------------------------------------------------------------------------
        // Texture
        // ----------------------------------------------------------------------------------------------------
        member this.InstallTexture(texture:Texture) = 

            if texture.notEmpty && not (textures.ContainsKey(texture.Name)) then
                bitmapManager.IsCube <- texture.IsCube
                if texture.Data.Length > 0 then
                    bitmapManager.InitFromByteArray(texture.MimeType, texture.Data) 
                else 
                    bitmapManager.InitFromFileSystem(texture.Path)
                    
                bitmapManager.CreateTexture()

                this.AddTexture(texture, bitmapManager )

        member this.AddTexture(texture:Texture, bitmapManager:BitmapManager) =
            if texture.IsCube then
                textureHeapWrapperCube.AddResource(bitmapManager.Resource, texture.Idx, true, bitmapManager.FromArray)
            else 
                textureHeapWrapper.AddResource(bitmapManager.Resource, texture.Idx, false, false)

            textures.Add(texture.Name, texture.Idx)

        member this.ResetTextures() =
            textures.Clear()
            textureHeapWrapperCube.Reset()
            textureHeapWrapper.Reset()
            logDebug("Reset Textures")

        // ---------------------------------------------------------------------------------------------------- 
        // Den PipelineProvider mit einer Konfiguration füllen 
        // Alle benötigten Shader dieser Konfiguration  
        // Dazu die Kombinationen für 
        // Und eine erste aktive Konfiguration setzen
        // ----------------------------------------------------------------------------------------------------
        member this.InstallPipelineProvider(
            _InputLayoutDesc,
            _RootSignatureDesc, 
            _VertexShaderDesc,
            _PixelShaderDesc,
            _DomainShaderDesc,
            _HullShaderDesc,
            _SampleDesc,
            _BlendDesc,
            _RasterizerDesc,
            _TopologyType,
            _shaderDefines
            ) =
            pipelineProvider.Initialize(
                _InputLayoutDesc,
                _RootSignatureDesc,
                _VertexShaderDesc,
                _PixelShaderDesc,
                _DomainShaderDesc,
                _HullShaderDesc, 
                _SampleDesc,
                _BlendDesc,
                _RasterizerDesc,
                _TopologyType
            )

        // ----------------------------------------------------------------------------------------------------
        // Update und Draw
        // ----------------------------------------------------------------------------------------------------
        member this.UpdatePipeline(
                inputLayoutDesc:InputLayoutDescription,
                rootSignatureDefaultDesc:RootSignatureDescription,
                vertexShaderDesc:ShaderDescription,
                pixelShaderDesc:ShaderDescription,
                domainShaderDesc:ShaderDescription,
                hullShaderDesc:ShaderDescription,
                sampleDescription:SampleDescription,
                topologyType:PrimitiveTopologyType,
                topology:PrimitiveTopology,
                rasterizerDesc:RasterizerDescription,
                blendDesc:BlendDescription,
                isCube:bool
            ) =            
            pipelineProvider.InputLayoutDesc    <- inputLayoutDesc
            pipelineProvider.RootSignatureDesc  <- vertexShaderDesc.RootSignature
            pipelineProvider.VertexShaderDesc   <- vertexShaderDesc
            pipelineProvider.PixelShaderDesc    <- pixelShaderDesc 
            pipelineProvider.DomainShaderDesc   <- domainShaderDesc
            pipelineProvider.HullShaderDesc     <- hullShaderDesc
            pipelineProvider.BlendDesc          <- blendDesc 
            pipelineProvider.RasterizerDesc     <- rasterizerDesc 
            pipelineProvider.TopologyType       <- topologyType
            pipelineProvider.Topology           <- topology   
            pipelineProvider.IsCube             <- isCube

        // ----------------------------------------------------------------------------------------------------
        // Vor dem Zeichnen - Umschalten auf die nächste FrameResource
        // ----------------------------------------------------------------------------------------------------
        member this.StartUpdate() =  
            if frameResources.Count > 0 then
                currentFrameResourceIndex <- (currentFrameResourceIndex + 1) % NUMFRAMERESOURCES // Cycle through the circular frame resource array.
                coordinator.WaitForGPU(this.CurrFrameResource.FenceValue, this.CurrentFenceEvent)
        
        // Update Objekt-Eigenschaften (World, WorldView, ...)
        // Parameter ConstantBufferView ObjectCB = reg(b0) ites Element
        member this.UpdateObject(i, bytes) =  
            if frameResources.Count > 0 then
                this.CurrFrameResource.ObjectCB.CopyData(i, bytes)

        // Update Frame-Eigenschaften (Camera-Position, Light,...)
        // Parameter ConstantBufferView FrameCB = reg(b1) ites Element   
        member this.UpdateFrame(bytes) = 
            if frameResources.Count > 0 then
                this.CurrFrameResource.FrameCB.CopyData(0, bytes)
        
        // Update Material-Eigenschaften (Ambient, Diffuse,...)
        // Parameter ConstantBufferView MaterialCB = reg(b2) ites Element            
        member this.UpdateMaterial(i, bytes) = 
            if frameResources.Count > 0 then
                this.CurrFrameResource.MaterialCB.CopyData(i, bytes)

        // ---------------------------------------------------------------------------------------------------- 
        // Draw
        // ----------------------------------------------------------------------------------------------------
        member this.StartDraw() = 
            if frameResources.Count > 0 then
            
                this.CurrFrameResource.Recorder.PipelineState <- pipelineProvider.InitialPipelineState
                //this.CurrFrameResource.Recorder.PipelineState <- pipelineProvider.GetCurrentPipelineState()
                this.CurrFrameResource.Recorder.StartRecording()

                let commandList = this.CurrFrameResource.Recorder.CommandList

                commandList.SetGraphicsRootSignature(pipelineProvider.RootSignature)
                commandList.SetViewport(viewport) 
                commandList.SetScissorRectangles(scissorRectangels)
                commandList.ResourceBarrierTransition(this.CurrentBackBuffer, ResourceStates.Present, ResourceStates.RenderTarget) // back buffer used as render target 
            
                commandList.ClearRenderTargetView(this.CurrentBackBufferView, clearColor) 
                commandList.ClearDepthStencilView(this.DepthStencilView, ClearFlags.FlagsDepth ||| ClearFlags.FlagsStencil, 1.0f, 0uy)
 
                commandList.SetRenderTargets(Nullable this.CurrentBackBufferView, Nullable this.DepthStencilView)
                commandList.SetDescriptorHeaps(descriptorHeaps.Length, descriptorHeaps)

                // Frame Daten
                let rootFrameParmIdx = 2
                commandList.SetGraphicsRootConstantBufferView(ROP_IDX_FRAME, this.CurrFrameResource.FrameCB.ElementAdress(0)) 

        //
        // DrawPerObject mit dem Pipelinestate 
        //
        member this.DrawPerObject(objectIdx, geometryName:string, topology:PrimitiveTopology, materialIdx, texture:Texture) = 
            if frameResources.Count > 0 then

                this.CurrFrameResource.Recorder.PipelineState <- pipelineProvider.GetCurrentPipelineState()         
                let commandList = this.CurrFrameResource.Recorder.CommandList 

                // Geometrie
                commandList.SetVertexBuffer(0, meshCache.getVertexBuffer(geometryName))
                commandList.SetIndexBuffer(Nullable (meshCache.getIndexBuffer(geometryName)))
                commandList.PrimitiveTopology <- topology

                // Objekt Eigenschaften 
                commandList.SetGraphicsRootConstantBufferView(ROP_IDX_OBJECT, this.CurrFrameResource.ObjectCB.ElementAdress(objectIdx))

                // Material Eigenschaften
                commandList.SetGraphicsRootConstantBufferView(ROP_IDX_MATERIAL, this.CurrFrameResource.MaterialCB.ElementAdress(materialIdx)) 

                // Textur (wenn vorhanden)    
                if texture.notEmpty then  
                    if texture.IsCube then                    
                        commandList.SetGraphicsRootDescriptorTable(ROP_IDX_CUBE, textureHeapWrapperCube.GetGpuHandle(texture.Idx)) 
                    else
                        commandList.SetGraphicsRootDescriptorTable(ROP_IDX_TEXTURE, textureHeapWrapper.GetGpuHandle(texture.Idx)) 
            
                commandList.DrawIndexedInstanced(meshCache.getIndexCount(geometryName), 1, 0, 0, 0) 

        member this.EndDraw() = 
            if frameResources.Count > 0 then
                let recorder = this.CurrFrameResource.Recorder
                let commandList = recorder.CommandList
                
                commandList.ResourceBarrierTransition(this.CurrentBackBuffer, ResourceStates.RenderTarget, ResourceStates.Present) // back buffer used to present            
            
                recorder.StopRecording()
                recorder.Play()
            
                swapChain.Present(0, PresentFlags.None) |> ignore 

                coordinator.AdvanceCPU()
                this.CurrFrameResource.FenceValue <- coordinator.CpuFenceValue
                coordinator.AdvanceGPU() 

        // 
        // Size
        // 
        member this.Size(w, h) = 
            clientWidth <- w
            clientHeight <- h
            if swapChain <> null then

                this.FlushCommandQueue()
                directRecorder.StartRecording()
                let commandList = directRecorder.CommandList

                // Release the previous resources we will be recreating.
                if swapChainBuffers <> null then
                    for buffer in swapChainBuffers do
                        if buffer <> null then
                            buffer.Dispose() 

                if depthStencilBuffer <> null then
                    depthStencilBuffer.Dispose()

                swapChain.ResizeBuffers(
                    swapChain.Description.BufferCount,
                    clientWidth, clientHeight,
                    Format.R8G8B8A8_UNorm,
                    SwapChainFlags.AllowModeSwitch
                )

                BuildRenderTargetViews()

                BuildDepthStencil(clientWidth, clientHeight)

                // Transition the resource from its initial state to be used as a depth buffer.
                commandList.ResourceBarrierTransition(depthStencilBuffer, ResourceStates.Common, ResourceStates.DepthWrite) 

                // Execute the resize commands.
                directRecorder.StopRecording()
                directRecorder.Play()   

                // Wait until resize is complete.
                this.FlushCommandQueue() 

                viewport <- ToRawViewport(new ViewportF(0.0f, 0.0f, (float32)clientWidth, (float32)clientHeight, 0.0f, 1.0f))
                scissorRectangels.[0] <- ToRawRectangle(new RectangleF(0.0f, 0.0f, (float32)clientWidth, (float32)clientHeight))

            logDebug("MYGPU INFO: SIZE End\n")

        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        // Frame Verwaltung
        // Zur besseren Auslastung von CPU und GPU werden mehrere Frames parallel abgearbeitet
        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------

        //
        // Frame resources
        //
        abstract BuildFrameResources: int*int  -> unit
        default this.BuildFrameResources(itemCount:int, materialsCount:int) = 
            directFrameResource <- new FrameResource(device, directRecorder, itemCount, itemLength, materialsCount, matLength, frameLength)
            frameResources.Clear()
            for  i = 0 to NUMFRAMERESOURCES - 1 do 
                let frameRecorder = new Recorder("Recorder frame " + i.ToString(), device, commandQueue, null)
                frameResources.Add(new FrameResource(device, frameRecorder, itemCount, itemLength, materialsCount, matLength, frameLength))
                fenceEvents.Add(new AutoResetEvent(false))  