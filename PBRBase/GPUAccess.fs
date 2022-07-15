namespace PBRBase
//
//  GPUAccess.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open DirectX.Assets
open SharpDX.Direct3D12
open System 

// ----------------------------------------------------------------------------------------------------
// GPU Abstraction
// ----------------------------------------------------------------------------------------------------
module GPUAccess =

    let compOffSet tableLength descriptorSize objIdx typIdx =
        objIdx * tableLength * descriptorSize
        + typIdx * descriptorSize

    [<AllowNullLiteralAttribute>]
    type TextureHeapWrapper(device: Device, heap: DescriptorHeap, _tableLength: int) =
        let mutable device = device
        let mutable tableLength = _tableLength
        let mutable cbvSrvUavDescriptorSize = 0
        let mutable cpuDescriptor = CpuDescriptorHandle() // Heap in CPU
        let mutable gpuDescriptor = GpuDescriptorHandle() // Heap in GPU
        let mutable offSet = 0
        let mutable computeOffset = compOffSet tableLength cbvSrvUavDescriptorSize  

        do
            cpuDescriptor <- heap.CPUDescriptorHandleForHeapStart
            gpuDescriptor <- heap.GPUDescriptorHandleForHeapStart
            cbvSrvUavDescriptorSize <- device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler)
            computeOffset <- compOffSet tableLength cbvSrvUavDescriptorSize 

        member this.HDescriptor
            with get () = cpuDescriptor
            and set (value) = cpuDescriptor <- value

        member this.ComputeOffset(typIdx: int, objIdx) =
            offSet <- computeOffset objIdx typIdx
            cpuDescriptor <- heap.CPUDescriptorHandleForHeapStart + offSet
            gpuDescriptor <- heap.GPUDescriptorHandleForHeapStart + offSet

        member this.GetGpuHandle(typIdx, objIdx) =
            this.ComputeOffset(typIdx, objIdx)
            gpuDescriptor

        member this.AddResource(resource: Resource, typIdx: int, objIdx: int, isCube) =
            this.ComputeOffset(typIdx, objIdx)
            device.CreateShaderResourceView(resource, Nullable(textureDescription (resource, isCube, false)), cpuDescriptor) // HACK (gltf dev inactive)

    [<AllowNullLiteralAttribute>]
    type SamplerHeapWrapper(device: Device, heap: DescriptorHeap, _tableLength: int) =
        let mutable device = device
        let mutable tableLength = _tableLength
        let mutable cbvSrvUavDescriptorSize = 0
        let mutable offSet = 0
        let mutable cpuDescriptor = CpuDescriptorHandle() // Heap in CPU
        let mutable gpuDescriptor = GpuDescriptorHandle() // Heap in GPU
        let mutable computeOffset = compOffSet tableLength cbvSrvUavDescriptorSize

        do
            cbvSrvUavDescriptorSize <- device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler)
            cpuDescriptor <- heap.CPUDescriptorHandleForHeapStart
            gpuDescriptor <- heap.GPUDescriptorHandleForHeapStart
            computeOffset <- compOffSet tableLength cbvSrvUavDescriptorSize 

        member this.HDescriptor
            with get () = cpuDescriptor
            and set (value) = cpuDescriptor <- value

        member this.ComputeOffset(typIdx: int, objIdx) =
            offSet <- computeOffset objIdx typIdx
            cpuDescriptor <- heap.CPUDescriptorHandleForHeapStart + offSet
            gpuDescriptor <- heap.GPUDescriptorHandleForHeapStart + offSet

        member this.GetGpuHandle(objIdx, typIdx) =
            this.ComputeOffset(typIdx, objIdx)
            gpuDescriptor

        member this.AddResource(samplerDescription, typIdx: int, objIdx: int) =
            this.ComputeOffset(typIdx, objIdx)
            device.CreateSampler(samplerDescription, cpuDescriptor)
