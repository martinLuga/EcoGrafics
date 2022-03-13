namespace GltfBase
//
//  AnotherGPU.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
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

open DirectX.GraficUtils
open DirectX.BitmapSupport 
 
open GPUModel.MyPipelineSupport
open GPUModel.MYUtils

open FrameResource
open GPUInfrastructure
open GPUAccess
open Common
open Structures
  
// ----------------------------------------------------------------------------------------------------
// GPU  
// Abgeleitet von Original MyGPU
// ----------------------------------------------------------------------------------------------------

module AnotherGPU = 

    let loggerGPU = LogManager.GetLogger("AnotherGPU")
    let logDebug = Debug(loggerGPU)
    let logInfo  = Info(loggerGPU)

    let FRAMECOUNT              = 2 
    let NUMFRAMERESOURCES       = 5 
    let SWAPCHAINBUFFERCOUNT    = 2 
    let DSVDESCRIPTORCOUNT      = 1
    let BACKBUFFERFORMAT        = Format.R8G8B8A8_UNorm  
    let RTVDESCRIPTORCOUNT      = SWAPCHAINBUFFERCOUNT 

    let ROP_IDX_OBJECT    = 0
    let ROP_IDX_FRAME     = 1
    let ROP_IDX_MATERIAL  = 2

    let ROP_IDX_TEX_1     = 3 
    let ROP_IDX_TEX_2     = 4  

    let ROP_IDX_SMP_1     = 5
    let ROP_IDX_SMP_2     = 6

    // ----------------------------------------------------------------------------------------------------
    //  Class  MyGPU 
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>] 
    type MyGPU() =

        // Window
        let mutable viewport = new RawViewportF()
        let mutable scissorRectangels:RawRectangle[] = Array.create 1 (new RawRectangle())
        let mutable clientWidth  = 1100 
        let mutable clientHeight = 850       
        let mutable clearColor:RawColor4 = new RawColor4(1.0f, 1.0f, 1.0f, 1.0f) 

        // Shaders
        let mutable pipelineConfigurations=new Dictionary<string, ShaderConfiguration>() 
        let mutable currentPipelineConfigurationName="Basic"
        let mutable pixelShaderDesc:ShaderDescription=null

        // Resources    
        let mutable textureHeap1:TextureHeapWrapper = null
        let mutable textureHeap2:TextureHeapWrapper = null

        let mutable samplerHeap1:SamplerHeapWrapper = null
        let mutable samplerHeap2:SamplerHeapWrapper = null

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

        override this.ToString() = "AnotherGPU: " + pipelineProvider.ToString()

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

        // ---------------------------------------------------------------------------------------------------- 
        // GPU 
        // ---------------------------------------------------------------------------------------------------- 
        member this.InitGPU(form: UserControl) =

            InitDirect3D(form, clientWidth, clientHeight)

            BuildDescriptorHeaps()

            this.FrameResources <- new List<FrameResource>(NUMFRAMERESOURCES)

            directRecorder <- new Recorder("Direct recording", device, commandQueue, null)
            coordinator <- new ProcessorCoordinator(commandQueue, fence)

            pipelineProvider <- new PipelineProvider(device)

        member this.Reset() =
            
            textureHeap1 <- new TextureHeapWrapper(device, srvDescriptorHeap, 5)
            textureHeap2 <- new TextureHeapWrapper(device, srvDescriptorHeap, 3)

            samplerHeap1 <- new SamplerHeapWrapper(device, smpDescriptorHeap, 5)
            samplerHeap2 <- new SamplerHeapWrapper(device, smpDescriptorHeap, 3)

        // ----------------------------------------------------------------------------------------------------
        // Install
        // ----------------------------------------------------------------------------------------------------

        member this.StartInstall() =
            directRecorder.StartRecording() 

        abstract PrepareInstall:int*int->Unit
        default this.PrepareInstall(anzObjects, anzMaterials) =
            logInfo("Install " + anzObjects.ToString() + " objects for display ") 
            this.BuildFrameResources(anzObjects, anzMaterials)

        member this.ExecuteInstall()=
            directRecorder.StopRecording()
            directRecorder.Play()

        // ----------------------------------------------------------------------------------------------------
        // Texture
        // ----------------------------------------------------------------------------------------------------
        member this.InstallTexture(_texture: MyTexture ) =

            let mutable bitmapManager = BitmapManager(device)            
            bitmapManager.InitFromArray(_texture.Info.MimeType, _texture.Data) 
            let texture = bitmapManager.CreateTextureFromBitmap()  

            let sampler = _texture.Sampler
            let sDesc   = DynamicSamplerDesc(sampler) 
            
            match _texture.Kind with
            | TextureTypePBR.envDiffuseTexture ->
                textureHeap2.AddResource(texture, (int _texture.Kind) - 8, _texture.TxtIdx, true)
                samplerHeap2.AddResource(sDesc, (int _texture.Kind) - 8, _texture.SmpIdx)
            | TextureTypePBR.brdfLutTexture ->
                textureHeap2.AddResource(texture, (int _texture.Kind) - 8, _texture.TxtIdx, false)
                samplerHeap2.AddResource(sDesc, (int _texture.Kind) - 8, _texture.SmpIdx)
            | TextureTypePBR.envSpecularTexture ->
                textureHeap2.AddResource(texture, (int _texture.Kind) - 8, _texture.TxtIdx, true)
                samplerHeap2.AddResource(sDesc, (int _texture.Kind) - 8, _texture.SmpIdx)
            | _ ->
                textureHeap1.AddResource(texture, int _texture.Kind, _texture.TxtIdx, false)
                samplerHeap1.AddResource(sDesc, int _texture.Kind, _texture.SmpIdx) 
            logDebug("Install: " + _texture.ToString())  

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
            _TopologyType
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
                blendDesc:BlendDescription
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

        // ----------------------------------------------------------------------------------------------------
        // Vor dem Zeichnen - Umschalten auf die nächste FrameResource
        // ----------------------------------------------------------------------------------------------------
        member this.StartUpdate() =  
            if frameResources.Count > 0 then
                currentFrameResourceIndex <- (currentFrameResourceIndex + 1) % NUMFRAMERESOURCES // Cycle through the circular frame resource array.
                coordinator.WaitForGPU(this.CurrFrameResource.FenceValue, this.CurrentFenceEvent)
        
        // Update Objekt-Eigenschaften (World, View, ...)
        // Parameter ConstantBufferView ObjectCB = reg(b0) ites Element
        member this.UpdateView(i, bytes) =  
            if frameResources.Count > 0 then
                this.CurrFrameResource.ViewCB.CopyData(i, bytes)

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
            logDebug("START")
            if frameResources.Count > 0 then
            
                this.CurrFrameResource.Recorder.PipelineState <- pipelineProvider.InitialPipelineState
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
                commandList.SetGraphicsRootConstantBufferView(ROP_IDX_FRAME, this.CurrFrameResource.FrameCB.ElementAdress(0)) 
    
        // ----------------------------------------------------------------------------------------------------
        // DrawPerObject  
        // ----------------------------------------------------------------------------------------------------
        member this.DrawPerObject(_instanceCount, _bufferIdx, _vertexBuffer, _indexBuffer, _topology:PrimitiveTopology, _textures:MyTexture list) =
         
            this.CurrFrameResource.Recorder.PipelineState <- pipelineProvider.GetCurrentPipelineState()        
            let commandList = this.CurrFrameResource.Recorder.CommandList 

            // Geometrie
            commandList.SetVertexBuffer(0, _vertexBuffer )
            commandList.SetIndexBuffer(Nullable (_indexBuffer ))
            commandList.PrimitiveTopology <- _topology
                        
            commandList.SetGraphicsRootConstantBufferView(ROP_IDX_OBJECT, this.CurrFrameResource.ViewCB.ElementAdress(_bufferIdx))

            commandList.SetGraphicsRootConstantBufferView(ROP_IDX_MATERIAL, this.CurrFrameResource.MaterialCB.ElementAdress(_bufferIdx)) 

            this.DrawTextures(commandList, _textures)
            
            commandList.DrawIndexedInstanced(_instanceCount, 1, 0, 0, 0) 

        member this.DrawTextures (_commandList, _textures) =
            for texture in _textures do 
                match  texture.Kind with
                |  TextureTypePBR.envDiffuseTexture 
                |  TextureTypePBR.brdfLutTexture   
                |  TextureTypePBR.envSpecularTexture  ->
                    ()
                    //_commandList.SetGraphicsRootDescriptorTable(ROP_IDX_TEX_2, textureHeap2.GetGpuHandle((int texture.Kind) - 8, texture.TxtIdx)) 
                | _ ->
                    _commandList.SetGraphicsRootDescriptorTable(ROP_IDX_TEX_1, textureHeap1.GetGpuHandle(int texture.Kind, texture.TxtIdx)) 

                logDebug ("DRAW Texture ! Obj: " + texture.ObjectName+ " ! Mat: " + texture.MatIdx.ToString() + " ! " + texture.ToString() )

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

