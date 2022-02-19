namespace GltfBase
//
//  MyGPUInfrastructure.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 


open System
open System.Threading 
open System.Windows.Forms
open System.Collections.Generic 
open System.Diagnostics

open SharpDX.Direct3D 
open SharpDX.Direct3D12
open SharpDX.DXGI

open DirectX.Assets
open DirectX.D3DUtilities

open GPUModel.MYUtils
  
// ----------------------------------------------------------------------------------------------------
// GPU Infrastructure
// ----------------------------------------------------------------------------------------------------
module GPUInfrastructure =
        
    // Multisampling 
    // Funktioniert nicht in DirectX 
    let mutable IS_4X_MSAA_ENABLED = false        
    let mutable MS_4X_MSAA_QUALITY = 0
    let mutable msaaCount = 1
    let mutable msaaQuality = 0

    let MsaaCount = if IS_4X_MSAA_ENABLED then 4 else 1
    let MsaaQuality = if IS_4X_MSAA_ENABLED then MS_4X_MSAA_QUALITY - 1 else 0

    // Heap 
    let mutable rtvHeap:DescriptorHeap = null
    let mutable srvDescriptorHeap:DescriptorHeap = null
    let mutable smpDescriptorHeap:DescriptorHeap = null
    let mutable dsvHeap:DescriptorHeap = null
    let mutable cbvHeap:DescriptorHeap = null
    let mutable descriptorHeaps:DescriptorHeap[] = null

    let mutable rtvDescriptorSize = 0
    let mutable dsvDescriptorSize = 0
    let mutable cbvSrvUavDescriptorSize = 0

    // Pipeline  
    let mutable factory:Factory4 = null
    let mutable device:Device = null
    let mutable swapChain:SwapChain3 = null    
    let mutable swapChainBuffers:Resource[] = Array.create SWAPCHAINBUFFERCOUNT null
    let mutable depthStencilBuffer:Resource = null
    let mutable commandQueue:CommandQueue = null

    // Synchronization  
    let mutable fence = new Fence(nativeint 0)
    let mutable fenceEvents = new List<AutoResetEvent>(NUMFRAMERESOURCES)

    // Debug
    let mutable debugController1:Debug1 = null  

    // ----------------------------------------------------------------------------------------------------
    // Descriptor heaps
    // ----------------------------------------------------------------------------------------------------     
    let BuildDescriptorHeaps() =
        // Constant buffer view (CBV) descriptor heap.
        //let cbvHeapDesc = 
        //    new DescriptorHeapDescription(  
        //        DescriptorCount = 1,
        //        Flags = DescriptorHeapFlags.ShaderVisible,
        //        Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
        //        NodeMask = 0)
        //cbvHeap <- device.CreateDescriptorHeap(cbvHeapDesc)

        // Render target view (RTV) descriptor heap.
        let rtvHeapDesc = 
            new DescriptorHeapDescription(  
                DescriptorCount = FRAMECOUNT,
                Flags = DescriptorHeapFlags.None,
                Type = DescriptorHeapType.RenderTargetView)
        rtvHeap <- device.CreateDescriptorHeap(rtvHeapDesc)
        
        // Describe and create a depth stencil view (DSV) descriptor heap.
        // Für das Render Target
        let dsvHeapDesc = 
            new DescriptorHeapDescription( 
                DescriptorCount = DSVDESCRIPTORCOUNT,
                Type = DescriptorHeapType.DepthStencilView
            )
        dsvHeap <- device.CreateDescriptorHeap(dsvHeapDesc) 

        // Shader resource view (SRV) descriptor heap.
        // Für Texturen
        let srvDescriptorHeapDesc = 
            new DescriptorHeapDescription(  
                DescriptorCount = 8,
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView)
        srvDescriptorHeap <- device.CreateDescriptorHeap(srvDescriptorHeapDesc)

        // Shader resource view (SRV) descriptor heap.
        // Dyn Sampler der Texturen
        let  samplerHeapDesc = 
            new DescriptorHeapDescription(   
                DescriptorCount = 8,
                Type =  DescriptorHeapType.Sampler,
                Flags = DescriptorHeapFlags.ShaderVisible
            )   
        smpDescriptorHeap <- device.CreateDescriptorHeap(samplerHeapDesc)

        descriptorHeaps <- [|srvDescriptorHeap; smpDescriptorHeap|]  

    // 
    // Device & Co
    // 
    let InitDirect3D(form:UserControl,  clientWidth, clientHeight) =

        //DebugInterface.Get().EnableDebugLayer() 
        //debugController1 <- DebugInterface.Get().QueryInterface<Debug1>()
        //debugController1.EnableGPUBasedValidation <- RawBool(true)
        //debugController1.EnableSynchronizedCommandQueueValidation <- RawBool(true)

        factory                 <- new Factory4()
        device                  <- new Device(null, FeatureLevel.Level_11_0)             
        fence                   <- device.CreateFence(0L, FenceFlags.None) 

        rtvDescriptorSize       <- device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView) 
        dsvDescriptorSize       <- device.GetDescriptorHandleIncrementSize(DescriptorHeapType.DepthStencilView) 
        cbvSrvUavDescriptorSize <- device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView)

        // Multisampling
        let mutable msQualityLevels = new FeatureDataMultisampleQualityLevels()
        msQualityLevels.Format <- BACKBUFFERFORMAT 
        msQualityLevels.SampleCount <- 4 
        msQualityLevels.Flags <- MultisampleQualityLevelFlags.None 
        msQualityLevels.QualityLevelCount <- 0             
        let result = device.CheckFeatureSupport(Feature.MultisampleQualityLevels, & msQualityLevels) 
        Debug.Assert(result)
        MS_4X_MSAA_QUALITY <- msQualityLevels.QualityLevelCount
        //msaaCount   <- MsaaCount      1   fest eingestellt, weil Multisampling in DirectX 12 nicht unterstützt
        //msaaQuality <- MsaaQuality    0     
        
        commandQueue            <- device.CreateCommandQueue(CommandQueueDescription(CommandListType.Direct)) 
        swapChain               <- createSwapChain(form.Handle, factory, clientWidth, clientHeight, commandQueue)

    // 
    // RenderTargetViews
    // 
    let BuildRenderTargetViews() =
        let mutable rtvHeapHandle = rtvHeap.CPUDescriptorHandleForHeapStart 
        for  i in 0 .. SWAPCHAINBUFFERCOUNT-1 do 
            let backBuffer = swapChain.GetBackBuffer<Resource>(i) 
            swapChainBuffers.[i] <- backBuffer 
            device.CreateRenderTargetView(backBuffer, System.Nullable(), rtvHeapHandle) 
            rtvHeapHandle <- rtvHeapHandle + rtvDescriptorSize 

    // ----------------------------------------------------------------------------------------------------
    // DepthStencil
    // ----------------------------------------------------------------------------------------------------
    let BuildDepthStencil(viewportWidth, viewportHeight) =
        let depthStencilDesc = depthStencilDescription (int64 viewportWidth, int viewportHeight , msaaCount , msaaQuality)
        depthStencilBuffer <- 
            device.CreateCommittedResource(
                new HeapProperties(HeapType.Default),
                HeapFlags.None,
                depthStencilDesc,
                ResourceStates.Common,
                Nullable optClear
            )

        // Create descriptor to mip level 0 of entire resource using a depth stencil format.
        let depthStencilViewDesc = 
            new DepthStencilViewDescription(
                Dimension = viewDimension(IS_4X_MSAA_ENABLED),
                Format = DEPTHSTENCILFORMAT)
        device.CreateDepthStencilView(depthStencilBuffer, Nullable depthStencilViewDesc, dsvHeap.CPUDescriptorHandleForHeapStart)

