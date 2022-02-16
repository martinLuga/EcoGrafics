namespace GltfBase
//
//  Katalog.fs
//
//  Created by Martin Luga on 10.09.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open VGltf.Types

open System
open System.Collections.Generic

open SharpDX.Direct3D

open Common
open VertexDefs
open MyMesh
open AnotherGPU

type Resource = VGltf.Resource

// ----------------------------------------------------------------------------------------------------
// Ein Registrar stellt die Verbindung von Objekt zu GPU her
// ----------------------------------------------------------------------------------------------------

module Katalog =

    // ----------------------------------------------------------------------------------------------------
    // Kataloge
    // Für jedes Objekt Objekt
    // Alle Materials, die in dem Node vorkommen, deployen
    // Alle Meshes, die in dem Node vorkommen, deployen
    // Alle Textures, die in dem Node vorkommen, deployen
    // ----------------------------------------------------------------------------------------------------

    [<AllowNullLiteral>]
    type NodeKatalog(device) =

        let nodeRegister = new NestedDict2<string, int, Node>()

        // Register one node of an object
        member this.Add(objectName, nodeIdx, node) =
            nodeRegister.Add(objectName, nodeIdx, node)

        // Register one node of an object
        member this.Get(objectName, nodeIdx) = nodeRegister.Item(objectName, nodeIdx)

    [<AllowNullLiteral>]
    type MeshKatalog(device) =

        let meshRegister = new NestedDict2<string, int, RegistryEntry>()

        // Find Meshdata to objectName/mesh
        let mutable meshContainer = new MeshContainer<Vertex>(device)

        // Register one mesh of an object
        member this.AddMesh(_object, _mesh, _vertices, _indices, topology: PrimitiveTopology, _material:int) =
            meshContainer.Append(_object, _mesh, _vertices, _indices, topology)
            meshRegister.Add(_object, _mesh, new RegistryEntry(_mesh, _material))

        member this.GetVertexBuffer(objectName, mesh) =
            meshContainer.getVertexBuffer (objectName, mesh)

        member this.GetIndexBuffer(objectName, mesh) =
            meshContainer.getIndexBuffer (objectName, mesh)

        member this.getIndexCount(objectName, mesh) =
            meshContainer.getIndexCount (objectName, mesh)

        member this.Material(objectName, mesh) =
            meshRegister.Item(objectName , mesh ).Material

        member this.Reset() =
            meshContainer <- new MeshContainer<Vertex>(device)
            meshRegister.Clear()

        member this.Mesh(_objectName, _mesh) =
            meshRegister.Item(_objectName, _mesh)
            
        member this.ToGPU(commandList) =
            meshContainer.createBuffers (commandList)

    // ----------------------------------------------------------------------------------------------------
    // Register material 
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type MaterialKatalog(device) =

        let mutable objectMaterials = new NestedDict2<string, int, MyMaterial>()

        member this.Add(_objectName, idx, material: Material) =
            let myMaterial  = new MyMaterial(idx , material) 
            objectMaterials.Add(_objectName, idx, myMaterial)

        member this.GetMaterial(name, idx) = objectMaterials.Item(name, idx)

        member this.Count() = objectMaterials.Count

        member this.ToGPU(commandList) = ()

    // ----------------------------------------------------------------------------------------------------
    // Register all textures of an object
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type TextureKatalog(gpu:MyGPU) = 
        
        let mutable textureCache = new NestedDict3<string, int, int, MyTexture >()

        let mutable idx  = 0

        member this.GetTextures(objectName, _material) =
            textureCache.Items (objectName, _material)

        member this.Add(objectName:string, _material:int, _kind: TextureInfoKind, sampler: Sampler, image: System.Drawing.Image, data:byte[], info:Image, _cube:bool ) =
            let myTexture  = new MyTexture(idx, _kind, sampler, image, data, info, _cube) 
            idx <- idx + 1
            textureCache.Add(objectName, _material, idx, myTexture)

        member this.Get(objectName, _material , _text) = 
            textureCache.Item(objectName, _material, _text)  

        member this.ToGPU() =
            for texture in textureCache.Items() do
                gpu.InstallTexture(texture) 

        member this.Reset() = 
            textureCache <- new NestedDict3<string,  int, int, MyTexture >()
