namespace GltfBase
//
//  GPUAccess.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open DirectX.Assets
open SharpDX.Direct3D12
open System
open Base.ShaderSupport

// ----------------------------------------------------------------------------------------------------
// GPU Abstraction
// ----------------------------------------------------------------------------------------------------
module GPUAccess =

    [<AllowNullLiteralAttribute>]
    type TextureHeapWrapper(device: Device, heap: DescriptorHeap, _tableLength: int) =
        let mutable device = device
        let mutable tableLength = _tableLength
        let mutable cbvSrvUavDescriptorSize = 0
        let mutable cpuDescriptor = CpuDescriptorHandle() // Heap in CPU
        let mutable gpuDescriptor = GpuDescriptorHandle() // Heap in GPU
        let mutable offSet = 0

        do
            cbvSrvUavDescriptorSize <-
                device.GetDescriptorHandleIncrementSize(
                    DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
                )

            cpuDescriptor <- heap.CPUDescriptorHandleForHeapStart
            gpuDescriptor <- heap.GPUDescriptorHandleForHeapStart

        member this.HDescriptor
            with get () = cpuDescriptor
            and set (value) = cpuDescriptor <- value

        member this.ComputeOffset(typIdx: int, objIdx) =
            offSet <-
                + objIdx * tableLength * cbvSrvUavDescriptorSize
                + typIdx * cbvSrvUavDescriptorSize

            cpuDescriptor <- heap.CPUDescriptorHandleForHeapStart + offSet
            gpuDescriptor <- heap.GPUDescriptorHandleForHeapStart + offSet

        member this.GetGpuHandle(typIdx, objIdx) =
            this.ComputeOffset(typIdx, objIdx)
            gpuDescriptor

        member this.AddResource(resource: Resource, typIdx: int, objIdx: int) =
            this.ComputeOffset(typIdx, objIdx)
            device.CreateShaderResourceView(resource, Nullable(textureDescription (resource, false)), cpuDescriptor)

    [<AllowNullLiteralAttribute>]
    type SamplerHeapWrapper(device: Device, heap: DescriptorHeap, _tableLength: int) =
        let mutable device = device
        let mutable tableLength = _tableLength
        let mutable cbvSrvUavDescriptorSize = 0
        let mutable offSet = 0
        let mutable hDescriptor = CpuDescriptorHandle()

        do
            cbvSrvUavDescriptorSize <- device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler)
            hDescriptor <- heap.CPUDescriptorHandleForHeapStart

        member this.HDescriptor
            with get () = hDescriptor
            and set (value) = hDescriptor <- value

        member this.ComputeOffset(typIdx: int, objIdx) =
            offSet <-
                + objIdx * tableLength * cbvSrvUavDescriptorSize
                + typIdx * cbvSrvUavDescriptorSize

            hDescriptor <- heap.CPUDescriptorHandleForHeapStart + offSet

        member this.GetGpuHandle(index, typIdx) =
            heap.GPUDescriptorHandleForHeapStart
            + index * tableLength * cbvSrvUavDescriptorSize
            + typIdx * cbvSrvUavDescriptorSize

        member this.AddResource(samplerDescription, typIdx: int, objIdx: int) =
            this.ComputeOffset(typIdx, objIdx)
            device.CreateSampler(samplerDescription, hDescriptor)
