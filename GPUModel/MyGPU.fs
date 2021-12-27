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
open Base.MeshObjects

open DirectX.GraficUtils
open DirectX.TextureSupport 

open DirectX.Assets

open GPUModel.MyPipelineConfiguration
open Base.ShaderSupport

open MyFrame
open MyPipelineSupport
open MYUtils
open MyGPUInfrastructure
  
// ----------------------------------------------------------------------------------------------------
// GPU Abstraction
// ----------------------------------------------------------------------------------------------------
module MyGPU = 

    let TEXTUREWIDTH = 256; 
    let TEXTUREHEIGHT = 256 
    let TEXTUREPIXELSIZE = 4    // The number of bytes used to represent a pixel in the texture.
    let FRAMECOUNT = 2 
    let NUMFRAMERESOURCES = 5 
    let SWAPCHAINBUFFERCOUNT = 2 
    let DSVDESCRIPTORCOUNT = 1
    let BACKBUFFERFORMAT = Format.R8G8B8A8_UNorm  
    let RTVDESCRIPTORCOUNT = SWAPCHAINBUFFERCOUNT 

    let loggerGPU = LogManager.GetLogger("GPU")
    let debugGPU = Debug(loggerGPU)
    let infoGPU  = Info(loggerGPU)
    
    let loggerUPDT = LogManager.GetLogger("GPU.UPDATE")
    let debugUPDT = Debug(loggerUPDT)
    let infoUPDT  = Info(loggerUPDT)

    let loggerDRAW = LogManager.GetLogger("GPU.DRAW")
    let debugDRAW  = Debug(loggerDRAW)
    let infoDRAW   = Info(loggerDRAW)
    let errorDRAW  = Error(loggerDRAW)

    // HACK: Steht auch in ShaderConfiguration, kommt man aber hier nicht ran
    let rasterWiredDescription =
        RasterizerDescription(RasterType.Wired, rasterizerStateWired)

    let rasterSolidDescription =
        RasterizerDescription(RasterType.Solid, rasterizerStateSolid)

    let blendOpaqueDescription =
        BlendDescription(BlendType.Opaque, blendStateOpaque)

    let blendTransparentDescription =
        BlendDescription(BlendType.Transparent, blendStateTransparent)

    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    //  Class  MyGPU 
    //      Init
    //      Configure
    //      Draw
    //      Update
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>] 
    type MyGPU() =

        // Window
        let mutable viewport = new RawViewportF()
        let mutable scissorRectangels:RawRectangle[] = Array.create 1 (new RawRectangle())
        let mutable clientWidth  = 1100 
        let mutable clientHeight = 850       
        let mutable clearColor:RawColor4 = new RawColor4(0.0f, 0.0f, 0.0f, 1.0f) 

        // Shaders
        let mutable pipelineConfigurations=new Dictionary<string, MyPipelineConfiguration>() 
        let mutable currentPipelineConfigurationName="Basic"
        let mutable lastPipelineConfigName=""
        let mutable pixelShaderDesc:ShaderDescription=null

        // Resources     
        let mutable textures = new Dictionary<string, int>()
        let mutable textureIdx = 0
        let mutable rootMaterialParmIdx = 0
        let mutable rootObjectParmIdx = 0
        let mutable rootFrameParmIdx = 0
        let mutable textureHeapWrapper:HeapWrapper = null
        let mutable meshCache:MeshCache = null

        // Pipeline
        let mutable pipelineProvider:PipelineProvider=null
        let mutable directRecorder:Recorder = null
        let mutable directFrameResource:MyFrameResource = null
        let mutable frameResources = new List<MyFrameResource>(NUMFRAMERESOURCES)
        let mutable currentFrameResourceIndex = 0 
        let mutable itemLength = 0 
        let mutable matLength = 0 
        let mutable frameLength = 0 

        // Display
        let mutable rasterizerDesc=RasterizerDescription(RasterType.Solid, rasterizerStateSolid)
        let mutable blendDesc=BlendDescription(BlendType.Opaque, blendStateOpaque)  

        // Synchronization objects.
        let mutable coordinator:ProcessorCoordinator = null

        interface IDisposable with 
            member this.Dispose() =  
                this.FlushCommandQueue()
                commandQueue.Dispose() 
                rtvHeap.Dispose() 
                srvDescriptorHeap.Dispose() 
                dsvHeap.Dispose()  
                fence.Dispose() 
                swapChain.Dispose() 
                device.Dispose() 

        override this.ToString() =
            "MyGPU: " + pipelineProvider.ToString()

        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        // Member
        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        member this.AspectRatio =
            (float) clientWidth / (float) clientHeight

        member this.ClearColor
            with get() = clearColor
            and set(value) = clearColor <- value 

        member this.Device
            with get() = device

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

        member this.CurrFrameResource = frameResources.[currentFrameResourceIndex]
        
        member this.CurrentFenceEvent = fenceEvents.[currentFrameResourceIndex]

        member this.CurrentBackBuffer = swapChainBuffers.[swapChain.CurrentBackBufferIndex]
        
        member this.CurrentBackBufferView = rtvHeap.CPUDescriptorHandleForHeapStart + swapChain.CurrentBackBufferIndex * rtvDescriptorSize
        
        member this.DepthStencilView = dsvHeap.CPUDescriptorHandleForHeapStart 

        member this.CurrentPipelineConf = pipelineConfigurations.Item(currentPipelineConfigurationName)

        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        // Methoden
        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        
        member this.Begin() =
            infoGPU("Begin")  
            this.FlushCommandQueue()  

        member this.FlushCommandQueue() = 
            coordinator.AdvanceCPU()
            coordinator.AdvanceGPU()
            coordinator.Wait()

        member this.DepthStencilViewDim() =
            if IS_4X_MSAA_ENABLED then DepthStencilViewDimension.Texture2DMultisampled 
            else DepthStencilViewDimension.Texture2D

        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        // Initialisierungen
        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------

        // 
        // Klasse
        // 
        member this.Initialize(graficWindow:UserControl) =
             debugGPU("Initialize")
             this.InitGPU(graficWindow)
             this.Size(graficWindow.ClientSize.Width, graficWindow.ClientSize.Height)
        // 
        // GPU 
        // 
        member this.InitGPU(form:UserControl) = 
            debugGPU("InitGPU")

            clientWidth     <- form.ClientSize.Width  
            clientHeight    <- form.ClientSize.Height 

            clearColor      <- ToRawColor4FromDrawingColor(form.BackColor)
            
            let vp          =  new ViewportF(0.0f,  0.0f, (float32)clientWidth, (float32)clientHeight, 0.0f, 1.0f) 
            viewport        <- ToRawViewport(vp)

            let sr          = new RectangleF(0.0f, 0.0f, (float32)clientWidth, (float32)clientHeight)
            scissorRectangels.[0] <- ToRawRectangle(sr)

            // Device & Co
            InitDirect3D(form,  clientWidth, clientHeight)

            // DescriptorHeaps             
            textureHeapWrapper <- new HeapWrapper(device, srvDescriptorHeap)

             // Recorder: Command processing     
            directRecorder <- new Recorder("Direct recording", device, commandQueue, null)
            
            // Coordinator: Synchronization 
            coordinator <- new ProcessorCoordinator(commandQueue, fence)

            // Geometry / Vertex Cache
            this.initializeMeshCache()

            // PipelineProvider
            pipelineProvider <- new PipelineProvider(device)

        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        // Install
        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------

        // 
        // Objects 
        // 

        member this.StartInstall()=
            loggerGPU.Info("Start Install")   
            directRecorder.StartRecording() 

        member this.PrepareInstall(anzObjects) =
            loggerGPU.Info("Install " + anzObjects.ToString() + " objects for display ") 
            this.BuildFrameResources(anzObjects, anzObjects)
            this.resetMeshCache()

        member this.FinalizeInstall() =
            this.FinalizeMeshCache(directRecorder.CommandList)

        member this.FinishInstall()=
            loggerGPU.Info("FinishInstall") 
            directRecorder.StopRecording()
            directRecorder.Play()

        // ----------------------------------------------------------------------------------------------------
        // MeshData
        // ----------------------------------------------------------------------------------------------------        
        member this.hasMesh(name) =
            meshCache.Contains(name) 

        member this.InstallMesh(name, meshData: MeshData, topology) =
            meshCache.Append(name, meshData.Vertices, meshData.Indices, topology) 

        member this.ResetAllMeshes() =
            directRecorder.StartRecording()
            this.resetMeshCache()
            directRecorder.StopRecording()
            directRecorder.Play()

        member this.initializeMeshCache() =
            meshCache <- new MeshCache(device)

        member this.resetMeshCache() =
            meshCache.Reset()

        member this.FinalizeMeshCache(commandList) =
            meshCache.createBuffers(commandList)

        // ----------------------------------------------------------------------------------------------------
        // Texture
        // ----------------------------------------------------------------------------------------------------
        member this.InstallTexture(textureName:string, textureFilename:string) =
            if textureName <> null then
                if not (textures.ContainsKey(textureName)) && not (textureName = "") then
                    let  resource = CreateTextureFromBitmap(device, textureFilename)
                    textureHeapWrapper.AddResource(resource)
                    textures.Add(textureName, textureIdx)
                    textureIdx <- textureIdx + 1

        // ---------------------------------------------------------------------------------------------------- 
        // Den PipelineProvider mit allen Konfigurationen füllen 
        // Alle benötigten Shader (Konfigurationen) 
        // Dazu die Kombinationen für 
        // Und eine erste aktive Konfiguration setzen
        // ----------------------------------------------------------------------------------------------------
        member this.InstallPipelineProvider(configs:MyPipelineConfiguration list) = 
            for conn in configs do  
                pipelineConfigurations.Add(conn.ConfigName, conn) 
                pipelineProvider.AddConfig(conn)  

            pipelineProvider.ActivateConfig(configs.Head)
            pipelineProvider.SetInitialConfig(configs.Head)

        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        // Update und Draw
        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------

        //
        // Rootsignature und Pipelinestate entsprechend Displayable gesetzt
        //       
        member this.UpdatePipeline(pipelineConfigName:string, objectPixelShaderDesc:ShaderDescription, objectBlendDesc:BlendDescription, objectTopologyTypeDesc:TopologyTypeDescription) =             
            debugUPDT("Pipeline " + pipelineConfigName + objectPixelShaderDesc.Klass.ToString()  + objectBlendDesc.Type.ToString() + rasterizerDesc.Type.ToString())
            
            if pipelineProvider.ConfigName <> pipelineConfigName then
               let tmpConfig = pipelineConfigurations.Item(pipelineConfigName)
               pipelineProvider.ActivateConfig(tmpConfig)
            pipelineProvider.ConfigName         <- pipelineConfigName 
            pipelineProvider.PixelShaderDesc    <- objectPixelShaderDesc 
            pipelineProvider.BlendDesc          <- objectBlendDesc 
            pipelineProvider.RasterizerDesc     <- this.RasterizerDesc 
            pipelineProvider.TopologyTypeDesc   <- objectTopologyTypeDesc
            pipelineProvider.GetCurrentPipelineState()|> ignore             // Refresh

        // 
        // Update  
        // 
        member this.StartUpdate() =  
            debugUPDT("Start")
            if frameResources.Count > 0 then
                currentFrameResourceIndex <- (currentFrameResourceIndex + 1) % NUMFRAMERESOURCES // Cycle through the circular frame resource array.
                coordinator.WaitForGPU(this.CurrFrameResource.FenceValue, this.CurrentFenceEvent)

        member this.UpdateObject(i, bytes) =  
            debugUPDT("Object " + i.ToString())          
            if frameResources.Count > 0 then
                this.CurrFrameResource.ObjectCB.CopyData(i, bytes)
            
        member this.UpdateMaterial(i, bytes) = 
            debugUPDT("Update Material " + i.ToString())           
            if frameResources.Count > 0 then
                this.CurrFrameResource.MaterialCB.CopyData(i, bytes)
            
        member this.UpdateFrame(bytes) = 
            debugUPDT("Frame")                      
            if frameResources.Count > 0 then
                this.CurrFrameResource.FrameCB.CopyData(0, bytes)

        // 
        // Draw
        // 
        member this.StartDraw() = 
            debugDRAW("START")
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
                rootFrameParmIdx <- 2
                commandList.SetGraphicsRootConstantBufferView(rootFrameParmIdx, this.CurrFrameResource.FrameCB.ElementAdress(0)) 

        //
        // DrawPerObject mit dem Pipelinestate 
        //
        member this.DrawPerObject(objectIdx, geometryName:string, topology:PrimitiveTopology, materialIdx, textureName:string) =
            debugDRAW("OBJECT " + objectIdx.ToString())            
            if frameResources.Count > 0 then

                this.CurrFrameResource.Recorder.PipelineState <- pipelineProvider.GetCurrentPipelineState()         
                let commandList = this.CurrFrameResource.Recorder.CommandList 

                // Objekt Geometrie
                commandList.SetVertexBuffer(0, meshCache.getVertexBuffer(geometryName))
                commandList.SetIndexBuffer(Nullable (meshCache.getIndexBuffer(geometryName)))
                commandList.PrimitiveTopology <- topology

                // Objekt Daten 
                rootObjectParmIdx <- 1
                commandList.SetGraphicsRootConstantBufferView(rootObjectParmIdx, this.CurrFrameResource.ObjectCB.ElementAdress(objectIdx))

                // Material per object
                rootMaterialParmIdx <- 3
                commandList.SetGraphicsRootConstantBufferView(rootMaterialParmIdx, this.CurrFrameResource.MaterialCB.ElementAdress(materialIdx)) 

                // Texture per Objekt
                if textureName <> "" then  
                    let textureIdx = textures.Item(textureName)
                    commandList.SetGraphicsRootDescriptorTable(0, textureHeapWrapper.GetGpuHandle(textureIdx)) 
            
                commandList.DrawIndexedInstanced(meshCache.getIndexCount(geometryName), 1, 0, 0, 0) 

        member this.EndDraw() =
            infoDRAW("END")   
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
                //Debug.Print("MYGPU INFO: End Draw\n")

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

            debugGPU("MYGPU INFO: SIZE End\n")

        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        // Frame Verwaltung
        // Zur besseren Auslastung von CPU und GPU werden mehrere Frames parallel abgearbeitet
        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------

        //
        // Frame resources
        //
        member this.BuildFrameResources(itemCount:int, materialsCount:int) = 
            directFrameResource <- new MyFrameResource(device, directRecorder, itemCount, itemLength, materialsCount, matLength, frameLength)
            frameResources.Clear()
            for  i = 0 to NUMFRAMERESOURCES - 1 do 
                let frameRecorder = new Recorder("Recorder frame " + i.ToString(), device, commandQueue, null)
                frameResources.Add(new MyFrameResource(device, frameRecorder, itemCount, itemLength, materialsCount, matLength, frameLength))
                fenceEvents.Add(new AutoResetEvent(false))  
        
        // Refresh
        member this.RefreshFrameResources(pipelineState) = 
            for fresoure in frameResources do 
                fresoure.Recorder.PipelineState <- pipelineState
                fresoure.Recorder.Rewind()