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
open System.Diagnostics

open SharpDX
open SharpDX.Mathematics.Interop
open SharpDX.Direct3D 
open SharpDX.Direct3D12
open SharpDX.DXGI

open DirectX.GraficUtils
open DirectX.TextureSupport 
open DirectX.MeshObjects
open DirectX.Assets

open GPUModel.MyPipelineConfiguration
open Shader.ShaderSupport

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
    let NUMFRAMERESOURCES = 3 
    let SWAPCHAINBUFFERCOUNT = 2 
    let DSVDESCRIPTORCOUNT = 1
    let BACKBUFFERFORMAT = Format.R8G8B8A8_UNorm  
    let RTVDESCRIPTORCOUNT = SWAPCHAINBUFFERCOUNT 

    let logger = LogManager.GetLogger("MyGPU")

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

        // Shader 
        let mutable CurrentPipelineConfigurationName="Basic"
        let mutable currentPixelShaderDesc:ShaderDescription=null
        let mutable pipelineConfigurations=new Dictionary<string, MyPipelineConfiguration>()

        // App resources     
        let mutable textures = new Dictionary<string, int>()
        let mutable textureIdx = 0
        let mutable rootMaterialParmIdx = 0
        let mutable rootObjectParmIdx = 0
        let mutable rootFrameParmIdx = 0
        let mutable textureHeapWrapper:HeapWrapper = null
        let mutable geometryCache:GeometryCache = null

        let mutable pipelineProvider:PipelineProvider=null
        let mutable directRecorder:Recorder = null
        let mutable directFrameResource:MyFrameResource = null
        let mutable frameResources = new List<MyFrameResource>(NUMFRAMERESOURCES)
        let mutable currentFrameResourceIndex = 0 
        let mutable itemLength = 0 
        let mutable matLength = 0 
        let mutable frameLength = 0 
        let mutable rasterizerDesc=RasterizerDescription(RasterType.Solid, rasterizerStateSolid)
        let mutable blendDesc=BlendDescription(BlendType.Opaque, blendStateOpaque)  

        // Synchronization objects.
        let mutable coordinator:ProcessorCoordinator = null

        // Singleton
        static let mutable instance = null
        
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

        static member Instance
            with get() = 
                if instance = null then
                    instance <- new MyGPU()
                instance
            and set(value) = instance <- value

        // initialization
        member this.Initialize(graficWindow:UserControl) =
             logger.Debug("Initialize")
             this.InitGPU(graficWindow)
             this.Size(graficWindow.ClientSize.Width, graficWindow.ClientSize.Height)

        // Member
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

        member this.SetPipelineConfigurations(configs:MyPipelineConfiguration list) = 
            for conn in configs do
                pipelineConfigurations.Add(conn.ConnName, conn) 

        member this.CurrentPixelShaderDesc
            with get() = currentPixelShaderDesc
            and set(value) = 
                currentPixelShaderDesc <- value
                pipelineProvider.PixelShaderDesc <- currentPixelShaderDesc
                //logger.Debug("Set pixelshader to " + (string currentPixelShaderDesc.Klass))

        member this.CurrFrameResource = frameResources.[currentFrameResourceIndex]
        
        member this.CurrentFenceEvent = fenceEvents.[currentFrameResourceIndex]

        member this.CurrentBackBuffer = swapChainBuffers.[swapChain.CurrentBackBufferIndex]
        
        member this.CurrentBackBufferView = rtvHeap.CPUDescriptorHandleForHeapStart + swapChain.CurrentBackBufferIndex * rtvDescriptorSize
        
        member this.DepthStencilView = dsvHeap.CPUDescriptorHandleForHeapStart 

        member this.CurrentPipelineConf = pipelineConfigurations.Item(CurrentPipelineConfigurationName)

        // 
        // Methods  
        // 
        member this.FlushCommandQueue() = 
            coordinator.AdvanceCPU()
            coordinator.AdvanceGPU()
            coordinator.Wait()

        member this.DepthStencilViewDim() =
            if IS_4X_MSAA_ENABLED then DepthStencilViewDimension.Texture2DMultisampled 
            else DepthStencilViewDimension.Texture2D

        // 
        // Initialize 
        // 
        member this.InitGPU(form:UserControl) = 
            logger.Debug("InitGPU")

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
            this.installGeometryCache()

            // Pipelinestate
            pipelineProvider <- new PipelineProvider(device)

        // 
        // Public SetConfig
        // 
        member this.SetConfig(config) =    
            CurrentPipelineConfigurationName <- config
            pipelineProvider.ActivateConfig(this.CurrentPipelineConf)

        member this.Begin() =
            logger.Info("Begin")  
            this.FlushCommandQueue()  

        // 
        // Public InstallObjects 
        // 
        member this.InstallObjects(anzObjects, anzMaterials, meshData:Dictionary<string,MeshData>, textureFiles:Dictionary<string,string>) = 
            logger.Info("InstallObjects")   
            directRecorder.StartRecording()            
            this.installGeometryCache()
            this.InstallGeometry(meshData)
            this.FinalizeGeometryCache(directRecorder.CommandList)
            this.BuildFrameResources(anzObjects, anzMaterials)
            for textureFile in textureFiles do
                this.InstallTextureBMP(textureFile.Key, textureFile.Value)
            directRecorder.StopRecording()
            directRecorder.Play()

        // 
        // Public RefreshGeometry 
        // 
        member this.RefreshGeometry(meshData:Dictionary<string,MeshData>) =
            directRecorder.StartRecording()
            this.resetGeometryCache()
            this.InstallGeometry(meshData)
            this.FinalizeGeometryCache(directRecorder.CommandList)
            directRecorder.StopRecording()
            directRecorder.Play()

        // 
        // Geometry
        // 
        member this.installGeometryCache() =
            geometryCache <- new GeometryCache(device)

        member this.resetGeometryCache() =
            geometryCache.Reset()

        member this.InstallObjectGeometry(geometryName, meshData:MeshData)=
            geometryCache.Append(geometryName, meshData.Vertices, meshData.Indices) 

        member this.FinalizeGeometryCache(commandList) =
            geometryCache.createBuffers(commandList)

        member this.InstallGeometry(meshData:Dictionary<string,MeshData>)=
            for mesh in meshData do
                this.InstallObjectGeometry(mesh.Key, mesh.Value)

        // 
        // Textures 
        //  
        member this.InstallTextureDDS(textureName:string, textureFilename:string) =
            if not (textures.ContainsKey(textureName)) && not (textureName = "") then
                let (resource, isCube) = CreateTextureFromDDS_2(device, textureFilename)
                textureHeapWrapper.AddResource(resource)
                textures.Add(textureName, textureIdx)
                textureIdx <- textureIdx + 1

        member this.InstallTextureBMP(textureName:string, textureFilename:string) =
            if not (textures.ContainsKey(textureName)) && not (textureName = "") then
                let  resource = CreateTextureFromBitmap(device, textureFilename)
                textureHeapWrapper.AddResource(resource)
                textures.Add(textureName, textureIdx)
                textureIdx <- textureIdx + 1

        // 
        // Update  
        // 
        member this.StartUpdate() =            
            if frameResources.Count > 0 then
                currentFrameResourceIndex <- (currentFrameResourceIndex + 1) % NUMFRAMERESOURCES // Cycle through the circular frame resource array.
                coordinator.WaitForGPU(this.CurrFrameResource.FenceValue, this.CurrentFenceEvent)

        member this.UpdateObject(i, bytes) =            
            if frameResources.Count > 0 then
                this.CurrFrameResource.ObjectCB.CopyData(i, bytes)
            
        member this.UpdateMaterial(i, bytes) =            
            if frameResources.Count > 0 then
                this.CurrFrameResource.MaterialCB.CopyData(i, bytes)
            
        member this.UpdateFrame(bytes) =            
            if frameResources.Count > 0 then
                this.CurrFrameResource.FrameCB.CopyData(0, bytes)

        // 
        // Draw
        // 
        member this.StartDraw() = 
            if frameResources.Count > 0 then
            
                this.CurrFrameResource.Recorder.PipelineState <- pipelineProvider.GetPipelineState()
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
        // Achtung: vor DrawPerObject werden Rootsignature und Pipelinestate entsprechen displayable gesetzt
        //
        member this.DrawPerObject(objectIdx, geometryName:string, topology:PrimitiveTopology, materialIdx, textureName:string) =
            
            if frameResources.Count > 0 then

                this.CurrFrameResource.Recorder.PipelineState <- pipelineProvider.GetPipelineState()         
                let commandList = this.CurrFrameResource.Recorder.CommandList 

                // Objekt Geometrie
                commandList.SetVertexBuffer(0, geometryCache.getVertexBuffer(geometryName))
                commandList.SetIndexBuffer(Nullable (geometryCache.getIndexBuffer(geometryName)))
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
            
                commandList.DrawIndexedInstanced(geometryCache.getIndexCount(geometryName), 1, 0, 0, 0) 

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
                //Debug.Print("MYGPU INFO: End Draw\n")

        // ----------------------------------------------------------------------------------------------------
        // Size
        // ----------------------------------------------------------------------------------------------------
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

            Debug.Print("MYGPU INFO: SIZE End\n")

        //
        // Frame resources
        //

        //New
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