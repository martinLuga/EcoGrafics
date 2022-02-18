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
open DirectX.D3DUtilities 
 
open GPUModel.MyPipelineSupport
open GPUModel.MYUtils

open MyFrame
open MyGPUInfrastructure
open Common
open GltfSupport
  
// ----------------------------------------------------------------------------------------------------
// GPU  
// Abgeleitet von Original MyGPU
// ----------------------------------------------------------------------------------------------------
module AnotherGPU = 

    let FRAMECOUNT              = 2 
    let NUMFRAMERESOURCES       = 5 
    let SWAPCHAINBUFFERCOUNT    = 2 
    let DSVDESCRIPTORCOUNT      = 1
    let BACKBUFFERFORMAT        = Format.R8G8B8A8_UNorm  
    let RTVDESCRIPTORCOUNT      = SWAPCHAINBUFFERCOUNT 

    let ROOT_PARM_IDX_OBJECT    = 0
    let ROOT_PARM_IDX_FRAME     = 1
    let ROOT_PARM_IDX_MATERIAL  = 2
    let ROOT_PARM_IDX_TEXTURE   = 3
    let ROOT_PARM_IDX_SAMPLER   = 5

    let loggerGPU = LogManager.GetLogger("GPU")
    let debugGPU = Debug(loggerGPU)
    let infoGPU  = Info(loggerGPU)

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
        let mutable clearColor:RawColor4 = new RawColor4(0.0f, 0.0f, 0.0f, 1.0f) 

        // Shaders
        let mutable pipelineConfigurations=new Dictionary<string, ShaderConfiguration>() 
        let mutable currentPipelineConfigurationName="Basic"
        let mutable pixelShaderDesc:ShaderDescription=null

        // Resources     
        let mutable textures = new Dictionary<string, int>()
        let mutable textureIdx = 0
        let mutable textureHeap:HeapWrapper = null
        let mutable samplerHeap:SamplerHeapWrapper = null

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

        member this.TextureHeapWrapper = textureHeap


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
            infoGPU("Begin")  
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
        member this.InitGPU(form:UserControl) = 

            InitDirect3D(form, clientWidth, clientHeight)

            BuildDescriptorHeaps()
                        
            this.FrameResources <- new List<FrameResource>(NUMFRAMERESOURCES)

            textureHeap <- new HeapWrapper(device, srvDescriptorHeap)
            samplerHeap <- new SamplerHeapWrapper(device, smpDescriptorHeap) 

            directRecorder <- new Recorder("Direct recording", device, commandQueue, null)
            coordinator <- new ProcessorCoordinator(commandQueue, fence)

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
            loggerGPU.Info("Install " + anzObjects.ToString() + " objects for display ") 
            this.BuildFrameResources(anzObjects, anzMaterials)

        member this.ExecuteInstall()=
            directRecorder.StopRecording()
            directRecorder.Play()

        // ----------------------------------------------------------------------------------------------------
        // Texture
        // ----------------------------------------------------------------------------------------------------
        member this.InstallTexture(_texture:MyTexture) =  
            let bitmap = _texture.Image :?> System.Drawing.Bitmap
            let texture = CreateTextureFromBitmap(device, bitmap)             
            textureHeap.AddResource(texture, _texture.Cube) 

            let sampler = _texture.Sampler
            let sDesc   = DynamicSamplerDesc(sampler) 
            samplerHeap.AddResource(sDesc) 

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
        
        // Update Objekt-Eigenschaften (World, WorldView, ...)
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
            debugGPU("START")
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
                commandList.SetGraphicsRootConstantBufferView(ROOT_PARM_IDX_FRAME, this.CurrFrameResource.FrameCB.ElementAdress(0)) 
    
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
                        
            commandList.SetGraphicsRootConstantBufferView(ROOT_PARM_IDX_OBJECT, this.CurrFrameResource.ViewCB.ElementAdress(_bufferIdx))

            commandList.SetGraphicsRootConstantBufferView(ROOT_PARM_IDX_MATERIAL, this.CurrFrameResource.MaterialCB.ElementAdress(_bufferIdx)) 

            this.DrawTextures(commandList, _textures)
            
            commandList.DrawIndexedInstanced(_instanceCount, 1, 0, 0, 0) 

        member this.DrawTextures (_commandList, _textures) =
            
            for texture in _textures do 
                _commandList.SetGraphicsRootDescriptorTable(ROOT_PARM_IDX_TEXTURE, textureHeap.GetGpuHandle(texture.Index)) 
                _commandList.SetGraphicsRootDescriptorTable(ROOT_PARM_IDX_SAMPLER, samplerHeap.GetGpuHandle(texture.SamplerIdx)) 

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
        abstract BuildFrameResources: int*int  -> unit
        default this.BuildFrameResources(itemCount:int, materialsCount:int) = 
            directFrameResource <- new FrameResource(device, directRecorder, itemCount, itemLength, materialsCount, matLength, frameLength)
            frameResources.Clear()
            for  i = 0 to NUMFRAMERESOURCES - 1 do 
                let frameRecorder = new Recorder("Recorder frame " + i.ToString(), device, commandQueue, null)
                frameResources.Add(new FrameResource(device, frameRecorder, itemCount, itemLength, materialsCount, matLength, frameLength))
                fenceEvents.Add(new AutoResetEvent(false))  

