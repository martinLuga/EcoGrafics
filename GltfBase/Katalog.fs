namespace GltfBase
//
//  Katalog.fs
//
//  Created by Martin Luga on 10.09.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System 

open Base.ShaderSupport

open VGltf.Types

open Common
open NodeAdapter
open MeshManager
open AnotherGPU
open Structures
open GPUInfrastructure

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
    type NodeKatalog(gpu: MyGPU) =

        let mutable nodeRegister = new NestedDict2<string, int, NodeAdapter>()

        static let mutable instance = null 
        static member Instance
            with get() = instance
            and set(value) = instance <- value

        static member CreateInstance(gpu: MyGPU) =
            NodeKatalog.Instance <- new NodeKatalog(gpu)

        // Register one node of an object
        member this.Add(_objectName, _adapter:NodeAdapter) =            
            nodeRegister.Add(_objectName, _adapter.Idx, _adapter)

        // Register one node of an object
        member this.Get(objectName, nodeIdx) = nodeRegister.Item(objectName, nodeIdx)

        member this.Reset() = nodeRegister <-  NestedDict2<string, int, NodeAdapter>()

    [<AllowNullLiteral>]
    type MeshKatalog(device) =
    
        // Mesh-Information to objectName/mesh
        let meshRegister = new NestedDict2<string, int, RegistryEntry>()

        // Find Vertexdata to objectName/mesh
        let mutable meshContainer = new MeshContainer<Vertex>(device)

        // Singleton
        static let mutable instance = null 
        static member Instance
            with get() = 
                if instance = null then
                    instance <- new MeshKatalog(DEVICE_RTX3090)
                instance
            and set(value) = instance <- value

        interface IDisposable with 
            member this.Dispose() =  
                (meshContainer:>IDisposable).Dispose()  

        // Register one mesh of an object at (_object, _meshIdx)
        member this.AddMesh(_objectName, _meshName, _meshIdx, _vertices, _indices, _topology, _matIdx:int) =
            meshContainer.Append(_objectName, _meshIdx, _vertices, _indices, _topology)
            meshRegister.Add(_objectName, _meshIdx, new RegistryEntry(_meshIdx, _meshName, _matIdx))

        member this.GetVertexBuffer(objectName, mesh) =
            meshContainer.getVertexBuffer (objectName, mesh)

        member this.GetIndexBuffer(objectName, mesh) =
            meshContainer.getIndexBuffer (objectName, mesh)

        member this.getIndexCount(objectName, mesh) =
            meshContainer.getIndexCount (objectName, mesh)

        member this.Material(objectName, mesh) =
            meshRegister.Item(objectName , mesh ).MatIdx

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
    type MaterialKatalog(_gpu:MyGPU) =

        let mutable gpu = _gpu

        let mutable objectMaterials = new NestedDict2<string, int, MyMaterial>()
        
        static let mutable instance = null 
        static member Instance
            with get() = instance
            and set(value) = instance <- value

        static member CreateInstance(gpu: MyGPU) =
            MaterialKatalog.Instance <- new MaterialKatalog(gpu)

        member this.Add
            (
                _objectName,
                _matIdx,
                _material: Material,
                _baseColourFactor: float32 [],
                _emissiveFactor: float32 [],
                _metallicRoughnessValues: float32 []
            ) =
            let myMaterial = new MyMaterial(_matIdx, _material, _baseColourFactor, _emissiveFactor, _metallicRoughnessValues)
            objectMaterials.Add(_objectName, _matIdx, myMaterial)

        member this.GetMaterial(_objectName, _matIdx) = objectMaterials.Item(_objectName, _matIdx)

        member this.Count() = objectMaterials.Count

        member this.ToGPU(commandList) = ()

        member this.Reset() = objectMaterials <- new NestedDict2<string, int, MyMaterial>()

    // ----------------------------------------------------------------------------------------------------
    // Register all textures of an object
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type TextureKatalog(_gpu: MyGPU) =

        let mutable gpu = _gpu
                                        //                Obj       TextIdx  Name      Texture
        let mutable textureCache        = new NestedDict3<string,   int,     string,   MyTexture>()
        
        let mutable myTexture:MyTexture = null 

        static let mutable instance = null 
        static member Instance
            with get() = instance
            and set(value) = instance <- value

        static member CreateInstance(gpu: MyGPU) =
            TextureKatalog.Instance <- new TextureKatalog(gpu)

        member this.GetTextures(objectName, _material) =
            textureCache.Items(objectName, _material)

        member this.Add
            (
                _objectName: string,
                _materialIdx: int,
                _textureIdx: int,
                _textureName: string,
                _textureType: TextureTypePBR,
                _samplerIdx: int,
                _sampler: Sampler,
                _image: System.Drawing.Image,
                _data: byte [],
                _info: Image,
                _cube: bool
            ) =

            myTexture <- new MyTexture(_objectName, _textureIdx, _textureName, 0, _textureType, _materialIdx, _samplerIdx, _sampler, _image, _data, _info, _cube)  
            textureCache.Add(_objectName, _materialIdx, _textureName, myTexture)

        member this.Get(_objectName, _matIdx, _textName) =
            textureCache.Item(_objectName, _matIdx, _textName)

        member this.ToGPU() =
            for texture in textureCache.Items() do
                gpu.InstallTexture(texture)

        member this.Reset() =
            textureCache    <- new NestedDict3<string, int, string, MyTexture>() 