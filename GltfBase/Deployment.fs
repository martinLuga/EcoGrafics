namespace GltfBase

//
//  Deployment.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System

open VGltf
open VGltf.Types

open Base.Framework

open BaseObject
open Running
open MeshBuild

// ----------------------------------------------------------------------------------------------------
// Support für das Deploy auf die GPU
// ---------------------------------------------------------------------------------------------------- 
module Deployment =

    let rec collect (gltf:Gltf, idx) = 
        // Process recursively all children
        let node = gltf.Nodes[idx]
 
        if node.Children <> null then
            node.Children
            |> Seq.append([idx])
            |> Seq.append(
                node.Children
                    |> Seq.toList
                    |> Seq.collect(fun child -> collect(gltf, child))
                )
        else [idx]
    
    // ----------------------------------------------------------------------------------------------------
    // Deployer
    // Lesen der Gltf Struktur für ein Objekt
    // Alle Materials, die in dem Node vorkommen, deployen 
    // Alle Meshes, die in dem Node vorkommen, deployen
    // ---------------------------------------------------------------------------------------------------- 
    [<AllowNullLiteral>]
    type Deployer() =
        let mutable store:ResourcesStore = null
        let mutable gltf:VGltf.Types.Gltf = null 
        let mutable correctorGtlf:glTFLoader.Schema.Gltf = null 
        let mutable nodeKatalog     = Runner.Instance.NodeKatalog
        let mutable meshKatalog     = Runner.Instance.MeshKatalog
        let mutable materialKatalog = Runner.Instance.MaterialKatalog
        let mutable textureKatalog  = Runner.Instance.TextureKatalog
        let mutable materialCount   = 0
        
        // ----------------------------------------------------------------------------------------------------
        // Singleton
        // ----------------------------------------------------------------------------------------------------         
        static let mutable instance = null 
        static member Instance
            with get() = instance
            and set(value) = instance <- value

        static member CreateInstance() =
            Deployer.Instance <- new Deployer()
            Deployer.Instance.Initialize()

        static member Deploy(_object:Objekt, _store:ResourcesStore, _correctorGtlf:glTFLoader.Schema.Gltf) =
            instance.Initialize()
            instance.Deploy(_object, _store, _correctorGtlf)
        
        member this.Initialize() =  
            materialCount <- 0 

        member this.MaterialKatalog
            with get() = materialKatalog

        member this.MeshKatalog
            with get() = meshKatalog

        // ----------------------------------------------------------------------------------------------------
        // Deploy 1 Gltf
        // ----------------------------------------------------------------------------------------------------   
        member this.Deploy(_objekt:Objekt, _store:ResourcesStore, _correctorGtlf:glTFLoader.Schema.Gltf) =
            
            store           <- _store
            gltf            <- _store.Gltf 
            correctorGtlf   <- _correctorGtlf 

             // Node
            let rootNodes = gltf.RootNodes |> Seq.toList
            assert (rootNodes.Length = 1)
            let rootNode:Node = rootNodes.Item(0) 

            let allNodes = _objekt.Nodes()
            this.Correct(allNodes, _correctorGtlf)

            let allLeafNodes = allNodes |> List.filter(fun node -> node.Node.Children=null)

            for adapter in allLeafNodes do
                this.DeployNode(_objekt.Name, adapter )            

        member this.Correct(_nodes, _corrector) = 
            for node in _nodes do
                let correctNode = _corrector.Nodes[node.Idx]
                node.Node.Scale <- correctNode.Scale
                node.Node.Translation <- correctNode.Translation
                node.Node.Rotation <- correctNode.Rotation

        member this.DeployNode(_objektName, _adapter) =
            let node = _adapter.Node
            nodeKatalog.Add(node.Name, _adapter.Idx, node)
            this.DeployMesh(_objektName, node.Mesh)

        member this.DeployMesh(_objektName, _mesh ) =
            if _mesh.HasValue then
                let mesh = gltf.Meshes[_mesh.Value] 
                let meshName, vertices, indices, topology, matIdx = CreateMeshData(mesh, store)
                meshKatalog.AddMesh(_objektName, _mesh.Value, vertices, indices, topology, matIdx)                 
                
                let material        = gltf.Materials[matIdx]
                let corrMaterial    = correctorGtlf.Materials[matIdx]

                this.DeployMaterial(_objektName, matIdx, material )

        member this.DeployMaterial(_objectName, _material, material) =
            //materialKatalog.Add(_objectName, _material, material)

            let textures = material.GetTextures()
            for text in textures do 
                this.DeployTexture(_objectName, _material, material, text)

        member this.DeployTexture(_objectName, _matIdx, _material, _text) =         
            
            if _text <> null then

                let mutable texture:Texture = null
                let mutable baseColourFactor:float32[] = [||]
                let mutable emissiveFactor:float32[] = [||]
                let mutable metallicRoughnessValues:float32[] = [|0.0f;0.0f|]

                match _text.Kind with
                | TextureInfoKind.BaseColor -> 
                    texture <- gltf.Textures[_material.PbrMetallicRoughness.BaseColorTexture.Index]

                | TextureInfoKind.Emissive  ->  
                    texture    <- gltf.Textures[_material.EmissiveTexture.Index] 
            
                | TextureInfoKind.Normal    ->   
                    texture    <- gltf.Textures[_material.NormalTexture.Index] 
            
                | TextureInfoKind.Occlusion -> 
                    texture    <- gltf.Textures[_material.OcclusionTexture.Index] 

                | TextureInfoKind.MetallicRoughness -> 
                    texture    <- gltf.Textures[_material.PbrMetallicRoughness.MetallicRoughnessTexture.Index] 

                | _ ->  raise (new Exception("TextureInfoKind"))
            
                let samplerIdx          = texture.Sampler.Value
                let sampler             = gltf.Samplers[samplerIdx] 
                let imageInfo           = gltf.Images[texture.Source.Value]
                let imageResource       = store.GetOrLoadImageResourceAt(texture.Source.Value)                
                let image               = ByteArrayToImage(imageResource.Data.Array, imageResource.Data.Offset, imageResource.Data.Count)
                let imageBytes          = ByteArrayToArray(imageResource.Data.Array, imageResource.Data.Offset, imageResource.Data.Count)

                textureKatalog.Add(_objectName, _matIdx, texture.Name, _text.Kind, samplerIdx, sampler, image, imageBytes, imageInfo, false)
                
                baseColourFactor            <- _material.PbrMetallicRoughness.BaseColorFactor
                emissiveFactor              <- _material.EmissiveFactor
                metallicRoughnessValues[0]  <- _material.PbrMetallicRoughness.MetallicFactor
                metallicRoughnessValues[1]  <- _material.PbrMetallicRoughness.RoughnessFactor 

                materialKatalog.Add(_objectName, _matIdx, _material, baseColourFactor, emissiveFactor, metallicRoughnessValues)