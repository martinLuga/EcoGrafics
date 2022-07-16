﻿namespace GltfBase
//
//  Deployment2.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System.Collections.Generic
open log4net

open VGltf
open VGltf.Types

open Base.ShaderSupport
open Base.LoggingSupport

open Base.Framework
open Base.VertexDefs

open BaseObject
open MeshBuild2 
open NodeAdapter
open Katalog
open GPUInfrastructure

// ----------------------------------------------------------------------------------------------------
// Deployment auf Base.Framework + Vertex
// EcoGrafics Technologie
// ---------------------------------------------------------------------------------------------------- 
module Deployment2 =

    let logger = LogManager.GetLogger("Deployment")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)

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
        let mutable materialCount = 0
        let mutable shaderDefines = new List<ShaderDefinePBR>()
        let mutable meshKatalog:MeshKatalog<Vertex> = new MeshKatalog<Vertex>(DEVICE_RTX3090)
        let mutable textureKatalog:TextureKatalog = new TextureKatalog() 
        let mutable materialKatalog:MaterialKatalog = new MaterialKatalog()
        let mutable nodeKatalog:NodeKatalog = new NodeKatalog()
        
        // ----------------------------------------------------------------------------------------------------
        // Singleton
        // ----------------------------------------------------------------------------------------------------         
        static let mutable instance = null 
        static member Instance
            with get() = 
                if instance=null then
                    Deployer.Instance <- new Deployer()
                    logInfo("Instance")  
                instance
            and set(value) = instance <- value

        static member Deploy(_object:Objekt, _store:ResourcesStore, _correctorGtlf:glTFLoader.Schema.Gltf) =
            Deployer.Instance.Initialize()
            Deployer.Instance.Deploy(_object, _store, _correctorGtlf)

        static member Reset() =  
            Deployer.Instance.Initialize()

        member this.MeshKatalog
            with get() = meshKatalog

        member this.TextureKatalog
            with get () = textureKatalog

        member this.MaterialKatalog
            with get () = materialKatalog
        
        member this.Initialize() = 
            logInfo("Initialize") 
            meshKatalog.Reset()
            textureKatalog.Reset() 
            materialKatalog.Reset()
            nodeKatalog.Reset()
            materialCount   <- 0 
            shaderDefines   <-  new List<ShaderDefinePBR>()
        
        member this.Correct(_nodes:NodeAdapter list, _corrector:glTFLoader.Schema.Gltf) = 
            for node in _nodes do
                let correctNode = _corrector.Nodes[node.Idx]
                node.Node.Scale <- correctNode.Scale
                node.Node.Translation <- correctNode.Translation
                node.Node.Rotation <- correctNode.Rotation
                node.Node.Matrix <- correctNode.Matrix

        // ----------------------------------------------------------------------------------------------------
        // Deploy 1 Gltf
        // ----------------------------------------------------------------------------------------------------   
        member this.Deploy(_objekt:Objekt, _store:ResourcesStore, _correctorGtlf:glTFLoader.Schema.Gltf) =
            
            store           <- _store
            gltf            <- _store.Gltf 
            correctorGtlf   <- _correctorGtlf 

            let allNodes = _objekt.Nodes()
            this.Correct(allNodes, _correctorGtlf)

            //_objekt.Tree.printAll()

            //_objekt.Tree.printAllGltf()

            let allLeafNodes = allNodes |> List.filter(fun node -> node.Node.Children=null)

            for adapter in allLeafNodes do
                this.DeployNode(_objekt.Name, adapter )            

        member this.DeployNode(_objektName, _adapter) =
            shaderDefines <- new List<ShaderDefinePBR>()
            if TEST_MESH_IDX = 0 || _adapter.Node.Mesh.Value = TEST_MESH_IDX then
                this.DeployMesh(_objektName, _adapter.Node.Mesh)
                _adapter.ShaderDefines <- shaderDefines 
                nodeKatalog.Add(_objektName, _adapter)

        member this.DeployMesh(_objectName, _meshIdx ) =
            if _meshIdx.HasValue then

                let meshIdx = _meshIdx.Value
                
                let mesh = gltf.Meshes[meshIdx] 
                let meshName, vertices, indices, topology, matIdx = CreateMeshData (mesh, store)
                meshKatalog.AddMesh(_objectName, meshName, meshIdx, vertices, indices  , topology, matIdx)                  
                
                let material         = gltf.Materials[matIdx] 
                let text = material.GetTextures() |>Seq.toList
                let tinfo = text[0]

                let mutable texture:Texture = null
                let mutable textureIdx:int = 0

                if material.PbrMetallicRoughness<> null then
                    if material.PbrMetallicRoughness.BaseColorTexture <> null then
                        textureIdx <- material.PbrMetallicRoughness.BaseColorTexture.Index
                        texture  <- gltf.Textures[textureIdx]
                        if texture <> null then
                            this.DeployTexture(_objectName, matIdx, material, texture, TextureTypePBR.baseColourTexture, false)
                            shaderDefines.Add(ShaderDefinePBR.HAS_BASECOLORMAP) 
                    
                    if material.PbrMetallicRoughness.MetallicRoughnessTexture <> null then
                        textureIdx <- material.PbrMetallicRoughness.MetallicRoughnessTexture.Index
                        texture <- gltf.Textures[textureIdx] 
                        if texture <> null then
                            this.DeployTexture(_objectName, matIdx, material, texture, TextureTypePBR.metallicRoughnessTexture, false) 
                            shaderDefines.Add(ShaderDefinePBR.HAS_METALROUGHNESSMAP)
               
                if material.EmissiveTexture <> null then
                    textureIdx <- material.EmissiveTexture.Index
                    texture <- gltf.Textures[textureIdx] 
                    if texture <> null then
                        this.DeployTexture(_objectName, matIdx, material, texture, TextureTypePBR.emissionTexture, false)
                        shaderDefines.Add(ShaderDefinePBR.HAS_EMISSIVEMAP)       
                
                if material.NormalTexture <> null then
                    textureIdx <- material.NormalTexture.Index
                    texture <- gltf.Textures[textureIdx] 
                    if texture <> null then
                        this.DeployTexture(_objectName, matIdx, material, texture, TextureTypePBR.normalTexture, false) 
                        shaderDefines.Add(ShaderDefinePBR.HAS_NORMALMAP)
                        
                if material.OcclusionTexture <> null then
                    textureIdx <- material.OcclusionTexture.Index
                    texture <- gltf.Textures[textureIdx] 
                    if texture <> null then
                        this.DeployTexture(_objectName, matIdx, material, texture, TextureTypePBR.occlusionTexture, false)
                        shaderDefines.Add(ShaderDefinePBR.HAS_OCCLUSIONMAP)
                
                if material.Extensions <> null then
                    let glossiness = material.Extensions.Item("KHR_materials_pbrSpecularGlossiness")
                    if glossiness <> null then
                    
                        let diffuseText = glossiness.Item("diffuseTexture")
                        if diffuseText.GenericContent <> null then
                            let index = diffuseText.Item("index")
                            textureIdx <- index.GenericContent.ToString()|> int
                            texture <- gltf.Textures[textureIdx]
                            if texture <> null then
                                this.DeployTexture(_objectName, matIdx, material, texture, TextureTypePBR.envDiffuseTexture, true)
                                shaderDefines.Add(ShaderDefinePBR.USE_IBL)

                        let specularGlossinessText = glossiness.Item("specularGlossinessTexture")
                        if specularGlossinessText.GenericContent <> null then
                            let index = specularGlossinessText.Item("index")
                            textureIdx <- index.GenericContent.ToString()|> int
                            texture <- gltf.Textures[textureIdx]
                            if texture <> null then
                                this.DeployTexture(_objectName, matIdx, material, texture,  TextureTypePBR.envSpecularTexture, true)
                                shaderDefines.Add(ShaderDefinePBR.USE_TEX_LOD)

                let baseColourFactor        = material.PbrMetallicRoughness.BaseColorFactor
                let emissiveFactor          = material.EmissiveFactor
                let metallicRoughnessValues:float32[] = [| 
                    material.PbrMetallicRoughness.MetallicFactor;
                    material.PbrMetallicRoughness.RoughnessFactor;
                 |]

                materialKatalog.Add(_objectName, matIdx, material, baseColourFactor, emissiveFactor, metallicRoughnessValues)

            member this.DeployTexture(_objectName, _matIdx:int, _material:Material, texture, textType, isCube) = 
                let samplerIdx          = texture.Sampler.Value
                let textureIdx          = texture.Source.Value
                let sampler             = gltf.Samplers[samplerIdx] 
                let imageInfo           = gltf.Images[textureIdx]
                let imageResource       = store.GetOrLoadImageResourceAt(textureIdx)                
                let bitmap              = ByteArrayToImage(imageResource.Data.Array, imageResource.Data.Offset, imageResource.Data.Count)
                let imageData           = ByteArrayToArray(imageResource.Data.Array, imageResource.Data.Offset, imageResource.Data.Count)
                
                textureKatalog.Add(_objectName, _matIdx, textureIdx, texture.Name, textType, samplerIdx, sampler, bitmap, imageData, imageInfo, isCube)   