namespace GltfBase
//
//  MyMesh.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2018 Martin Luga. All rights reserved.
//  

open Base.Framework
open DirectX.D3DUtilities
open GPUModel.MYUtils
open SharpDX
open SharpDX.Direct3D12
open System
open System.Collections.Generic
open System.Linq
 
// ----------------------------------------------------------------------------------------------------
// Container für Meshes unter Objektnamen und Teil-Nummer
// Ausserdem Hochladen Mesh zur GPU
// ---------------------------------------------------------------------------------------------------- 
module MyMesh =

    [<AllowNullLiteralAttribute>]
    type NestedDict () =
        let mutable objectNameDict  = Dictionary<string, Dictionary<int, ObjectControlblock>>()
        let newMeshNameDict()       = Dictionary<int, ObjectControlblock>()

        member this.Add(objectName:string, mesh:int, result: ObjectControlblock) =
            objectNameDict.
                TryItem(objectName, newMeshNameDict()).
                Replace(mesh, result)

        member this.Item(objectName:string, mesh:int) =
            try
                objectNameDict.Item(objectName).Item(mesh) 
            with
            | :? KeyNotFoundException -> null

        member this.ContainsKey(geometryName, meshName) =
            this.Item(geometryName, meshName) <> null

        member this.Clear() =
            objectNameDict.Clear()

    [<AllowNullLiteralAttribute>]
    type RegistryEntry (mesh:int, material:int) =
        let mutable mesh=mesh
        let mutable material=material  
        member this.Material = material
        member this.Mesh = mesh

    [<AllowNullLiteralAttribute>]
    type MeshContainer<'T when 'T:struct and 'T:(new:unit->'T) and 'T:>ValueType>(device:Device) =
        let mutable device=device
        let mutable indexBufferGPU:Resource = null
        let mutable vertexBufferGPU:Resource = null
        let mutable indexBufferUploader:Resource = null
        let mutable vertexBufferUploader:Resource = null
        let mutable vertexBufferCPU = new List<'T>()
        let mutable indexBufferCPU = new List<int>()
        let mutable ocbs = new NestedDict ()

        interface IDisposable with 
            member this.Dispose() =  
                indexBufferGPU.Dispose() 
                vertexBufferGPU.Dispose() 
                indexBufferUploader.Dispose() 
                vertexBufferUploader.Dispose() 

        member this.ResetBuffers() =
            vertexBufferCPU <- new List<'T>()
            indexBufferCPU  <- new List<int>()
            vertexBufferGPU <- null
            indexBufferGPU  <- null

        member this.Reset() =
            this.ResetBuffers()
            ocbs.Clear()

        member this.FreeBuffers() =
            vertexBufferUploader.Dispose()
            vertexBufferUploader <- null

        member this.Contains(geometryName, meshName) =
            ocbs.ContainsKey(geometryName, meshName)

        member this.Append(geometryName, _mesh, vertices, indices, topology)=
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
            
            ocbs.Add(geometryName, _mesh, ocb)

        member this.Replace(geometryName:string, _mesh:int, vertices:List<'T>)=
            let ocb = ocbs.Item(geometryName, _mesh) 
            let mutable idx = 0
            for i in ocb.StartVertices .. ocb.EndVertices - 1 do 
                vertexBufferCPU.[i] <- vertices.[idx]
                idx <- idx + 1

        member this.createBuffers(commandList:GraphicsCommandList) =
            let vertexArray = vertexBufferCPU |> Seq.toArray 
            let totalVertexBufferByteSize = Utilities.SizeOf(vertexArray) 
            try 
                vertexBufferGPU <-
                    D3DUtil.CreateDefaultBuffer(
                        device,
                        commandList,
                        vertexArray,
                        int64 totalVertexBufferByteSize,
                        &vertexBufferUploader
                    )
            with  
            | :? SharpDXException as ex -> 
                let reason = device.DeviceRemovedReason 
                raise (Exception("SharpDX Error: " + reason.ToString()))

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

        member this.getVertexBuffer(objectName, meshName) = 
            let ocb = ocbs.Item(objectName, meshName)
            new VertexBufferView(
                BufferLocation = vertexBufferGPU.GPUVirtualAddress + int64 (ocb.StartVertices*sizeof<'T>),
                StrideInBytes = sizeof<'T>,
                SizeInBytes = ocb.AnzahlVertices*sizeof<'T>
            ) 

        member this.getIndexBuffer(objectName, meshName) =
            let ocb = ocbs.Item(objectName, meshName)
            let indexFormat=GetIndexFormat<int>()
            new IndexBufferView( 
                BufferLocation = indexBufferGPU.GPUVirtualAddress + int64(ocb.StartIndices*sizeof<int>),
                Format = indexFormat,
                SizeInBytes = ocb.AnzahlIndices*sizeof<int>
            )

        member this.getIndexCount(geometryName, meshName) = 
            let ocb = ocbs.Item(geometryName, meshName)
            ocb.IndexCount

