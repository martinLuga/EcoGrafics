namespace GltfBase
//
//  ModernGPU.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
// 

open System
open System.Collections.Generic
open System.Threading 
open System.Windows.Forms

open log4net

open SharpDX
open SharpDX.Direct3D 
open SharpDX.Direct3D12 
open SharpDX.DXGI

open Base.LoggingSupport
open Base.ShaderSupport

open DirectX.Assets

open GPUModel.MYUtils
open GPUModel.FieldBuffer
open GPUModel.MyGPUInfrastructure
open GPUModel.MyGPU
open GPUModel.MyPipelineStore

open DirectX.D3DUtilities

open VGltf.Types

open Common
open GltfSupport

// ----------------------------------------------------------------------------------------------------
// GPU Abstraction Gltf style
// ----------------------------------------------------------------------------------------------------
module ModernGPU = 

    let loggerProvider = LogManager.GetLogger("ModernGPU")    
    let logFatal = Fatal(loggerProvider)

    [<AllowNullLiteralAttribute>]
    type FrameResource(device:Device, recorder:Recorder, objectCount:int, objectLength:int, materialCount:int, materialLength:int, frameLength:int) =
        let recorder = recorder 
        let mutable viewCB:FieldBuffer = null
        let mutable frameCB:FieldBuffer = null 
        let mutable materialCB:FieldBuffer = null

        let mutable fenceValue:int64 = 0L
        
        do
            viewCB          <-  new FieldBuffer(device, objectCount, objectLength)
            frameCB         <-  new FieldBuffer(device, 1, frameLength) 
            materialCB      <-  new FieldBuffer(device, materialCount, materialLength) 

        interface IDisposable with 
            member this.Dispose() = 
                (viewCB:> IDisposable).Dispose()
                (materialCB:> IDisposable).Dispose()
                (frameCB:> IDisposable).Dispose()

        override this.ToString() = "MyFrameResource-" + recorder.ToString()

        member this.Recorder  
            with get() = recorder

        member this.FrameCB
            with get() = frameCB

        member this.MaterialCB
            with get() = materialCB

        member this.ViewCB
            with get() = viewCB

        // Fence value to mark commands up to this fence point.  This lets us
        // check if these frame resources are still in use by the GPU.
        member this.FenceValue
            with get() = fenceValue
            and set(value) = fenceValue <- value

    // 
    // Create descriptor heaps
    // 
    let BuildDescriptorHeaps() =
        // Constant buffer view (CBV) descriptor heap.
        let cbvHeapDesc = 
            new DescriptorHeapDescription(  
                DescriptorCount = 1,
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
                NodeMask = 0)
        cbvHeap <- device.CreateDescriptorHeap(cbvHeapDesc)

        // Render target view (RTV) descriptor heap.
        let rtvHeapDesc = 
            new DescriptorHeapDescription(  
                DescriptorCount = FRAMECOUNT,
                Flags = DescriptorHeapFlags.None,
                Type = DescriptorHeapType.RenderTargetView)
        rtvHeap <- device.CreateDescriptorHeap(rtvHeapDesc)

        // Shader resource view (SRV) descriptor heap.
        let  samplerHeapDesc = 
            new DescriptorHeapDescription(   
                DescriptorCount = FRAMECOUNT,
                Type =  DescriptorHeapType.Sampler,
                Flags = DescriptorHeapFlags.ShaderVisible
            )   
        smpDescriptorHeap <- device.CreateDescriptorHeap(samplerHeapDesc)

        // Shader resource view (SRV) descriptor heap.
        let srvDescriptorHeapDesc = 
            new DescriptorHeapDescription(  
                DescriptorCount = 10,
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView)
        srvDescriptorHeap <- device.CreateDescriptorHeap(srvDescriptorHeapDesc)
            
        // Describe and create a depth stencil view (DSV) descriptor heap.
        let dsvHeapDesc = 
            new DescriptorHeapDescription( 
                DescriptorCount = DSVDESCRIPTORCOUNT,
                Type = DescriptorHeapType.DepthStencilView
            )
        dsvHeap <- device.CreateDescriptorHeap(dsvHeapDesc) 

        descriptorHeaps <- [|srvDescriptorHeap; smpDescriptorHeap|]  

    // ----------------------------------------------------------------------------------------------------
    //  Class MyModernGPU  
    //      Init
    //      Configure
    //      Draw
    //      Update
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>] 
    type MyModernGPU() = 
        inherit MyGPU() 
        
        let mutable currentPipelineState:PipelineState=null
        let mutable currentRootSignature:RootSignature=null
        
        let mutable frameResources = new List<FrameResource>(NUMFRAMERESOURCES)
        
        let mutable textureHeap:HeapWrapper = null
        let mutable samplerHeap:SamplerHeapWrapper = null
        
        member this.FrameResources
            with get() = frameResources
            and set(value) = frameResources <- value

        member this.CurrFrameResource = frameResources.[this.CurrentFrameResourceIndex]

        override this.PrepareInstall(_anzObjects, _anzMaterials) =
            loggerGPU.Info("Install " + _anzObjects.ToString() + " objects for display ") 
            this.BuildFrameResources(_anzObjects, _anzMaterials)

        member this.InstallPipelineState(
            _inputLayoutDesc ,      
            _rootSignatureDesc  , 
            _vertexShaderDesc ,
            _pixelShaderDesc ,  
            _domainShaderDesc ,
            _hullShaderDesc  ,
            _sampleDesc  ,      
            _blendDesc:BlendDescription ,   
            _rasterizerDesc:RasterizerDescription , 
            _topologyType     
        ) = (
            let psoDesc = 
                psoDesc(
                    device,
                    _inputLayoutDesc,
                    _rootSignatureDesc,
                    _vertexShaderDesc,
                    _pixelShaderDesc,
                    _domainShaderDesc,
                    _hullShaderDesc,
                    _blendDesc.Description, 
                    _rasterizerDesc.Description,
                    _topologyType,
                    SampleDescription(1, 0)
                    )
            try                 
                currentPipelineState <- device.CreateGraphicsPipelineState(psoDesc) 
                currentRootSignature <- createRootSignature(device, _rootSignatureDesc) 
            with :? SharpDXException as ex -> 
                logFatal("Pipelinestate createError "  + "\n"  + ex.Message + "\n")
                raise (PipelineCreateError("Pipelinestate create error " + ex.Message)) 
        )

        // ----------------------------------------------------------------------------------------------------
        // GPU 
        // ----------------------------------------------------------------------------------------------------
        override this.InitGPU(form:UserControl) =  

            // Device & Co
            InitDirect3D(form, this.ClientWidth, this.ClientHeight)

            BuildDescriptorHeaps()
                        
            this.FrameResources <- new List<FrameResource>(NUMFRAMERESOURCES)

            // DescriptorHeaps  
            textureHeap <- new HeapWrapper(device, srvDescriptorHeap)             
            samplerHeap <- new SamplerHeapWrapper(device, smpDescriptorHeap) 

            // Recorder: Command processing     
            this.DirectRecorder <- new Recorder("Direct recording", device, commandQueue, null)
            
            // Coordinator: Synchronization 
            this.Coordinator <- new ProcessorCoordinator(commandQueue, fence)

        // ----------------------------------------------------------------------------------------------------
        // Texture
        // ----------------------------------------------------------------------------------------------------
        member this.InstallTexture(_texture:MyTexture) = 
            let bitmap = _texture.Image :?> System.Drawing.Bitmap
            let texture = CreateTextureFromBitmap(device, bitmap) 
            
            textureHeap.AddResource(texture, _texture.Cube) 

            samplerHeap.AddResource() 

        // ----------------------------------------------------------------------------------------------------
        // Update in cycle
        // ----------------------------------------------------------------------------------------------------
        // Parameter ConstantBufferView ObjectCB = reg(b0) Vertex 
        member this.UpdateView(i, bytes) = 
            if this.FrameResources.Count > 0 then
                this.CurrFrameResource.ViewCB.CopyData(i, bytes)

        // Parameter ConstantBufferView FrameCB = reg(b0) Pixel
        member this.UpdateFrame(i, bytes) =  
            if frameResources.Count > 0 then
                this.CurrFrameResource.FrameCB.CopyData(i, bytes)
        
        // Update Material-Eigenschaften (Ambient, Diffuse,...)
        // Parameter ConstantBufferView MaterialCB = reg(b1) ites Element            
        member this.UpdateMaterial(i, bytes) = 
            if frameResources.Count > 0 then
                this.CurrFrameResource.MaterialCB.CopyData(i, bytes)

        // ----------------------------------------------------------------------------------------------------
        // Texturen in den jeweiligen Heap
        // ----------------------------------------------------------------------------------------------------
        member this.UpdateTextures(textNormal, textOcclusion, textEmissive, pbrMetallicRoughness) =
            let commandList = this.CurrFrameResource.Recorder.CommandList            
            if textNormal <> null then  
                commandList.SetGraphicsRootDescriptorTable(0, textureHeap.GetGpuHandle(0))   

        // ---------------------------------------------------------------------------------------------------- 
        // Draw
        // ----------------------------------------------------------------------------------------------------
        member this.StartDraw() =
           
            this.CurrFrameResource.Recorder.PipelineState <- currentPipelineState 
            this.CurrFrameResource.Recorder.StartRecording()

            let commandList = this.CurrFrameResource.Recorder.CommandList

            commandList.SetGraphicsRootSignature(currentRootSignature)
            commandList.SetViewport(this.Viewport) 
            commandList.SetScissorRectangles(this.ScissorRectangels)
            commandList.ResourceBarrierTransition(this.CurrentBackBuffer, ResourceStates.Present, ResourceStates.RenderTarget) // back buffer used as render target 
            
            commandList.ClearRenderTargetView(this.CurrentBackBufferView, this.ClearColor) 
            commandList.ClearDepthStencilView(this.DepthStencilView, ClearFlags.FlagsDepth ||| ClearFlags.FlagsStencil, 1.0f, 0uy)
 
            commandList.SetRenderTargets(Nullable this.CurrentBackBufferView, Nullable this.DepthStencilView)
            commandList.SetDescriptorHeaps(descriptorHeaps.Length, descriptorHeaps)

            // Frame Daten
            let rootFrameParmIdx = 1
            commandList.SetGraphicsRootConstantBufferView(rootFrameParmIdx, this.CurrFrameResource.FrameCB.ElementAdress(0)) 

        // ----------------------------------------------------------------------------------------------------
        // DrawPerObject  
        // ----------------------------------------------------------------------------------------------------
        member this.DrawPerObject(_instanceCount, _objectIdx, _materialIdx:Nullable<int>, _vertexBuffer, _indexBuffer, _topology:PrimitiveTopology, _textures:MyTexture list) =
         
            this.CurrFrameResource.Recorder.PipelineState <- currentPipelineState         
            let commandList = this.CurrFrameResource.Recorder.CommandList 

            // Geometrie
            commandList.SetVertexBuffer(0, _vertexBuffer )
            commandList.SetIndexBuffer(Nullable (_indexBuffer ))
            commandList.PrimitiveTopology <- _topology

            let rootObjectParmIdx = 0
            commandList.SetGraphicsRootConstantBufferView(rootObjectParmIdx, this.CurrFrameResource.ViewCB.ElementAdress(_objectIdx))

            let rootMaterialParmIdx = 2
            commandList.SetGraphicsRootConstantBufferView(rootMaterialParmIdx, this.CurrFrameResource.MaterialCB.ElementAdress(_materialIdx.Value)) 

            this.DrawTextures(commandList, _textures)
            
            commandList.DrawIndexedInstanced(_instanceCount, 1, 0, 0, 0) 

        member this.DrawTextures (_commandList, _textures) =
            
            for texture in _textures do 
                _commandList.SetGraphicsRootDescriptorTable(3, textureHeap.GetGpuHandle(texture.Index)) 
                 

        member this.EndDraw() = 

            let recorder = this.CurrFrameResource.Recorder
            let commandList = recorder.CommandList

            commandList.ResourceBarrierTransition(this.CurrentBackBuffer, ResourceStates.RenderTarget, ResourceStates.Present) // back buffer used to present            
            
            recorder.StopRecording()
            recorder.Play()
            
            swapChain.Present(0, PresentFlags.None) |> ignore 

            this.Coordinator.AdvanceCPU()
            this.CurrFrameResource.FenceValue <- this.Coordinator.CpuFenceValue
            this.Coordinator.AdvanceGPU()
        
        override this.BuildFrameResources(itemCount:int, materialsCount:int) = 
            this.FrameResources.Clear()
            for  i = 0 to NUMFRAMERESOURCES - 1 do 
                let frameRecorder = new Recorder("Recorder frame " + i.ToString(), device, commandQueue, null)
                this.FrameResources.Add(new FrameResource(device, frameRecorder, itemCount, this.ItemLength, materialsCount, this.MatLength, this.FrameLength))
                fenceEvents.Add(new AutoResetEvent(false))  