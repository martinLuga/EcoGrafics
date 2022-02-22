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
            Runner.Instance.NodeKatalog.Add(node.Name, _adapter.Idx, node)
            this.DeployMesh(_objektName, node.Mesh)

        member this.DeployMesh(_objectName, _mesh ) =
            if _mesh.HasValue then
                
                let mesh = gltf.Meshes[_mesh.Value] 
                let meshName, vertices, indices, topology, matIdx = CreateMeshData(mesh, store)
                Runner.Instance.MeshKatalog.AddMesh(_objectName, _mesh.Value, vertices, indices, topology, matIdx)                 
                
                let material         = gltf.Materials[matIdx] 
                let text = material.GetTextures() |>Seq.toList
                let tinfo = text[0]

                let mutable texture:Texture = null

                if material.PbrMetallicRoughness<> null then
                    if material.PbrMetallicRoughness.BaseColorTexture <> null then
                        texture  <- gltf.Textures[material.PbrMetallicRoughness.BaseColorTexture.Index]
                        if texture <> null then
                            this.DeployTexture(_objectName, matIdx, material, texture,  "BaseColor") 
                    
                    if material.PbrMetallicRoughness.MetallicRoughnessTexture <> null then
                        texture <- gltf.Textures[material.PbrMetallicRoughness.MetallicRoughnessTexture.Index] 
                        if texture <> null then
                            this.DeployTexture(_objectName, matIdx, material, texture, "MetallicRoughness") 
               
                if material.EmissiveTexture <> null then
                    texture <- gltf.Textures[material.EmissiveTexture.Index] 
                    if texture <> null then
                        this.DeployTexture(_objectName, matIdx, material, texture, "Emissive")       
                
                if material.NormalTexture <> null then
                    texture <- gltf.Textures[material.NormalTexture.Index] 
                    if texture <> null then
                        this.DeployTexture(_objectName, matIdx, material, texture,  "Normal")
                        
                if material.OcclusionTexture <> null then
                    texture <- gltf.Textures[material.OcclusionTexture.Index] 
                    if texture <> null then
                        this.DeployTexture(_objectName, matIdx, material, texture, "Occlusion")
                
                if material.Extensions <> null then
                    let glossiness = material.Extensions.Item("KHR_materials_pbrSpecularGlossiness")
                    if glossiness <> null then
                    
                        let diffuseText = glossiness.Item("diffuseTexture")
                        if diffuseText.GenericContent <> null then
                            let index = diffuseText.Item("index")
                            let i   = index.GenericContent.ToString()|> int
                            texture <- gltf.Textures[i]
                            if texture <> null then
                                this.DeployTexture(_objectName, matIdx, material, texture, "Diffuse")

                        let specularGlossinessText = glossiness.Item("specularGlossinessTexture")
                        if specularGlossinessText.GenericContent <> null then
                            let index = specularGlossinessText.Item("index")
                            let i   = index.GenericContent.ToString()|> int
                            texture <- gltf.Textures[i]
                            if texture <> null then
                                this.DeployTexture(_objectName, matIdx, material, texture, "Glossiness")

                let baseColourFactor        = material.PbrMetallicRoughness.BaseColorFactor
                let emissiveFactor          = material.EmissiveFactor
                let metallicRoughnessValues:float32[] = [| 
                     material.PbrMetallicRoughness.MetallicFactor;
                     material.PbrMetallicRoughness.RoughnessFactor 
                 |]

                Runner.Instance.MaterialKatalog.Add(_objectName, matIdx, material, baseColourFactor, emissiveFactor, metallicRoughnessValues)

            member this.DeployTexture(_objectName, _matIdx:int, _material:Material, texture, tinfo) = 
                let samplerIdx          = texture.Sampler.Value
                let sampler             = gltf.Samplers[samplerIdx] 
                let imageInfo           = gltf.Images[texture.Source.Value]
                let imageResource       = store.GetOrLoadImageResourceAt(texture.Source.Value)                
                let image               = ByteArrayToImage(imageResource.Data.Array, imageResource.Data.Offset, imageResource.Data.Count)
                let imageBytes          = ByteArrayToArray(imageResource.Data.Array, imageResource.Data.Offset, imageResource.Data.Count)

                Runner.Instance.TextureKatalog.Add(_objectName, _matIdx, texture.Name, tinfo, samplerIdx, sampler, image, imageBytes, imageInfo, false)
