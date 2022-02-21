namespace GltfBase
//
//  Katalog.fs
//
//  Created by Martin Luga on 10.09.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System.Collections.Generic

open VGltf.Types

open Base.VertexDefs

open Common
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

        let mutable nodeRegister = new NestedDict2<string, int, Node>()

        // Register one node of an object
        member this.Add(objectName, nodeIdx, node) =
            nodeRegister.Add(objectName, nodeIdx, node)

        // Register one node of an object
        member this.Get(objectName, nodeIdx) = nodeRegister.Item(objectName, nodeIdx)

        member this.Reset() = nodeRegister <-  NestedDict2<string, int, Node>()

    [<AllowNullLiteral>]
    type MeshKatalog(device) =
    
        // Mesh-Information to objectName/mesh
        let meshRegister = new NestedDict2<string, int, RegistryEntry>()

        // Find Vertexdata to objectName/mesh
        let mutable meshContainer = new MeshContainer<Vertex>(device)

        // Register one mesh of an object
        member this.AddMesh(_object, _mesh, _vertices, _indices, _topology, _material:int) =
            meshContainer.Append(_object, _mesh, _vertices, _indices, _topology)
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

        member this.Add
            (
                _objectName,
                idx,
                material: Material,
                baseColourFactor: float32 [],
                emissiveFactor: float32 [],
                metallicRoughnessValues: float32 []
            ) =
            let myMaterial = new MyMaterial(idx, material, baseColourFactor, emissiveFactor, metallicRoughnessValues)
            objectMaterials.Add(_objectName, idx, myMaterial)

        member this.GetMaterial(name, idx) = objectMaterials.Item(name, idx)

        member this.Count() = objectMaterials.Count

        member this.ToGPU(commandList) = ()

        member this.Reset() = objectMaterials <- new NestedDict2<string, int, MyMaterial>()

    // ----------------------------------------------------------------------------------------------------
    // Register all textures of an object
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type TextureKatalog(gpu: MyGPU) =

        let mutable textureCache = new NestedDict3<string, int, string, MyTexture>()
        let textureRegister      = new NestedDict3<string, TextureInfoKind, string, MyTexture >()

        
        let mutable myTexture:MyTexture = null 
        let mutable textureIdx   = 0

        member this.GetTextures(objectName, _material) =
            textureCache.Items(objectName, _material)

        member this.Add
            (
                objectName: string,
                _materialIdx: int,
                _textureName: string,
                _kind: TextureInfoKind,
                _samplerIdx: int,
                _sampler: Sampler,
                _image: System.Drawing.Image,
                _data: byte [],
                _info: Image,
                _cube: bool
            ) =

            if textureRegister.Items(objectName, _kind).Length = 0 then                 // Noch keiner zu dem Kind
                textureIdx <- 0                                                         // Ersten anlegen
                myTexture <- new MyTexture(objectName, _textureName, textureIdx, _kind, _materialIdx, _samplerIdx, _sampler, _image, _data, _info, _cube)  
                textureRegister.Add(objectName, _kind, _textureName, myTexture)
                textureCache.Add(objectName, _materialIdx, _textureName, myTexture)
            else 
                if textureRegister.ContainsKey(objectName, _kind, _textureName) then    // Schon da
                    ()                                                                  // Nichts tun
                else 
                    let anz = textureRegister.Items(objectName, _kind).Length           // Neu anlegen
                    textureIdx <- anz                                                   // Nummer hochzählen
                    myTexture <- new MyTexture(objectName, _textureName, textureIdx, _kind, _materialIdx, _samplerIdx, _sampler, _image, _data, _info, _cube)  
                    textureRegister.Add(objectName, _kind, _textureName, myTexture)
                    textureCache.Add(objectName, _materialIdx, _textureName, myTexture)

        member this.Get(objectName, _material, _textName) =
            textureCache.Item(objectName, _material, _textName)

        member this.ToGPU() =
            for texture in textureCache.Items() do
                gpu.InstallTexture(texture)

        member this.Reset() =
            textureCache <- new NestedDict3<string, int, string, MyTexture>()