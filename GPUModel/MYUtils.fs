namespace GPUModel
//
//  MYUtils.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//  

open System
open System.Collections.Generic
open System.Threading
open System.Linq

open log4net

open SharpDX
open SharpDX.Direct3D 
open SharpDX.Direct3D12
open SharpDX.DXGI

open Base.Framework

open DirectX.Assets
open DirectX.D3DUtilities

open Base.VertexDefs 
  
// ----------------------------------------------------------------------------------------------------
// GPU helper classes
// ----------------------------------------------------------------------------------------------------
module MYUtils = 

    type Device = SharpDX.Direct3D12.Device 
    type Resource = SharpDX.Direct3D12.Resource 
    type Wrapper<'a> = Wrapped of 'a

    let ResourceProducer(device:Device, desc) =
        device.CreateCommittedResource( 
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            desc,
            ResourceStates.Common
        )

    let GetIndexFormat<'TIndex>() = 
        let mutable format = Format.Unknown 
        if (typedefof<'TIndex> = typedefof<int>) then
            format <- Format.R32_UInt 
        else if (typedefof<'TIndex> = typedefof<int16>) then
            format <- Format.R16_UInt 
        assert(format <> Format.Unknown)
        format

    // ----------------------------------------------------------------------------------------------------
    //  Command processing   
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type Recorder(name:string, device:Device, commandQueue:CommandQueue, pipelineState:PipelineState) =
        let mutable commandAllocator=null
        let mutable commandList=null
        let mutable pipelineState=pipelineState
        let mutable commandQueue=commandQueue
        do 
            commandAllocator <- device.CreateCommandAllocator(CommandListType.Direct)
            commandList <- device.CreateCommandList(0, CommandListType.Direct, commandAllocator, pipelineState)
            commandList.Close()

        override this.ToString() = "Recorder-" + name

        member this.PipelineState  
            with get() = pipelineState
            and set(value) = 
                commandList.Name <- value.ToString()
                pipelineState <- value
                commandList.PipelineState <- value

        member this.CommandList
            with get() = commandList

        member this.Rewind() =
            commandAllocator <- device.CreateCommandAllocator(CommandListType.Direct)
            commandList <- device.CreateCommandList(0, CommandListType.Direct, commandAllocator, pipelineState)
            commandList.Close()

        member this.StartRecording() = 
            commandAllocator.Reset() 
            commandList.Name <- name
            commandList.Reset(commandAllocator, pipelineState) 

        member this.StopRecording() =
            commandList.Close() 

        member this.Log(logger:ILog) =
            logger.Info("")

        member this.Play() =
            commandQueue.ExecuteCommandList(commandList)

    // ----------------------------------------------------------------------------------------------------
    //  Heap indexing support   
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type HeapWrapper(device:Device, heap:DescriptorHeap) =
        let mutable device=device
        let mutable index=0
        let mutable cbvSrvUavDescriptorSize=0
        let mutable hDescriptor = CpuDescriptorHandle()
        do  
            cbvSrvUavDescriptorSize <- device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView)
            hDescriptor <- heap.CPUDescriptorHandleForHeapStart

        member this.HDescriptor  
            with get() = hDescriptor
            and set(value) = hDescriptor <- value

        member this.Reset() =
            hDescriptor <- heap.CPUDescriptorHandleForHeapStart

        member this.Increment() =
            hDescriptor <- hDescriptor + cbvSrvUavDescriptorSize

        member this.GetGpuHandle(index) =
            heap.GPUDescriptorHandleForHeapStart + index * cbvSrvUavDescriptorSize

        member this.AddResource(resource:Resource) =
            device.CreateShaderResourceView(resource, Nullable (textureDescription(resource)), hDescriptor)
            this.Increment()

    type ObjectControlblock (StartVertices:int, StartIndices:int, EndVertices:int, EndIndices:int, IndexCount:int, Topology:PrimitiveTopology) =
        let mutable startVertices=StartVertices
        let mutable startIndices=StartIndices 
        let mutable endVertices=EndVertices
        let mutable endIndices = EndIndices 
        let mutable indexCount = IndexCount 
        let mutable topology:PrimitiveTopology = Topology

        member this.StartVertices  
            with get() = startVertices
            and set(value) = startVertices <- value

        member this.StartIndices  
            with get() = startIndices
            and set(value) = startIndices <- value

        member this.EndVertices  
            with get() = endVertices
            and set(value) = endVertices <- value

        member this.EndIndices  
            with get() = endIndices
            and set(value) = endIndices <- value

        member this.IndexCount  
            with get() = indexCount
            and set(value) = indexCount <- value
                    
        member this.Topology
            with get() = topology
            and set(value) = topology <- value

        member this.AnzahlVertices =
             this.EndVertices - this.StartVertices

        member this.AnzahlIndices =
            this.EndIndices - this.StartIndices

    // ----------------------------------------------------------------------------------------------------
    //  Mesh support   
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type MeshCache(device:Device) =
        let mutable device=device
        let mutable indexBufferGPU:Resource = null
        let mutable vertexBufferGPU:Resource = null
        let mutable indexBufferUploader:Resource = null
        let mutable vertexBufferUploader:Resource = null
        let mutable vertexBufferCPU = new List<Vertex>()
        let mutable indexBufferCPU = new List<int>()
        let mutable ocbs = Dictionary<string,ObjectControlblock>()

        interface IDisposable with 
            member this.Dispose() =  
                indexBufferGPU.Dispose() 
                vertexBufferGPU.Dispose() 
                indexBufferUploader.Dispose() 
                vertexBufferUploader.Dispose() 

        member this.ResetBuffers() =
            vertexBufferCPU <- new List<Vertex>()
            indexBufferCPU  <- new List<int>()
            vertexBufferGPU <- null
            indexBufferGPU  <- null

        member this.Append(geometryName, vertices, indices, topology)=
            let mutable ocb = 
                ObjectControlblock(
                    StartVertices = vertexBufferCPU.Count,
                    StartIndices = indexBufferCPU.Count,
                    EndVertices = 0,
                    EndIndices = 0,
                    IndexCount = 0,
                    Topology = topology
                )     
                
            vertexBufferCPU.AddRange(vertices)
            indexBufferCPU.AddRange(indices)

            ocb.EndVertices <- vertexBufferCPU.Count
            ocb.EndIndices  <- indexBufferCPU.Count
            ocb.IndexCount  <- indices.Count()
                
            ocbs.Replace(geometryName, ocb)

        member this.createBuffers(commandList:GraphicsCommandList) =
            let vertexArray = vertexBufferCPU |> Seq.toArray 
            let totalVertexBufferByteSize = Utilities.SizeOf(vertexArray) 
            vertexBufferGPU <-
                D3DUtil.CreateDefaultBuffer(
                    device,
                    commandList,
                    vertexArray,
                    int64 totalVertexBufferByteSize,
                    &vertexBufferUploader
                )

            let indexArray = indexBufferCPU|> Seq.toArray
            let totalIndexBufferByteSize = (Utilities.SizeOf(indexArray))
            indexBufferGPU <-  
                D3DUtil.CreateDefaultBuffer(
                    device,
                    commandList,
                    indexArray,
                    int64 totalIndexBufferByteSize,
                    &indexBufferUploader
                )

        member this.getVertexBuffer(objectName) = 
            let ocb = ocbs.Item(objectName)
            new VertexBufferView(
                BufferLocation = vertexBufferGPU.GPUVirtualAddress + int64 (ocb.StartVertices*sizeof<Vertex>),
                StrideInBytes = sizeof<Vertex>,
                SizeInBytes = ocb.AnzahlVertices*sizeof<Vertex>
            ) 

        member this.getIndexBuffer(objectName) =
            let ocb = ocbs.Item(objectName)
            let indexFormat=GetIndexFormat<int>()
            new IndexBufferView( 
                BufferLocation = indexBufferGPU.GPUVirtualAddress + int64(ocb.StartIndices*sizeof<int>),
                Format = indexFormat,
                SizeInBytes = ocb.AnzahlIndices*sizeof<int>
            )

        member this.getIndexCount(geometryName) = 
            let ocb = ocbs.Item(geometryName)
            ocb.IndexCount

    // ----------------------------------------------------------------------------------------------------
    //  CPU / GPU Coordination   
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type ProcessorCoordinator(commandQueue:CommandQueue, fence:Fence) =
        let mutable commandQueue=commandQueue
        let mutable fence =fence
        let mutable cpuFenceValue=0L
        let mutable fenceEvent = new AutoResetEvent(false)
        
        //
        // Member
        //
        member this.GpuFenceValue
            with get() = fence.CompletedValue
            and  set(value) = commandQueue.Signal(fence, value)

        member this.CpuFenceValue
            with get() = cpuFenceValue
            and  set(value) = cpuFenceValue <- value

        //
        // Method
        //
        member this.AdvanceCPU() =
            this.CpuFenceValue <- this.CpuFenceValue + 1L

        member this.AdvanceGPU() =
            this.GpuFenceValue <- this.CpuFenceValue 

        member this.WaitForGPU(fenceValue, fenceEvent:AutoResetEvent) =
            if (fenceValue <> 0L && this.GpuFenceValue < fenceValue) then 
                fence.SetEventOnCompletion(fenceValue, fenceEvent.SafeWaitHandle.DangerousGetHandle()) 
                fenceEvent.WaitOne() |> ignore

        member this.Wait() =
            this.WaitForGPU(this.CpuFenceValue, fenceEvent )
