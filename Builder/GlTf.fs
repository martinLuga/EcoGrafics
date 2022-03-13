﻿namespace Builder
//
//  Wavefront.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System.Collections.Generic 

open VGltf
open VGltf.Types
open SharpDX

open Base.ModelSupport
open Base.MaterialsAndTextures
open Base.ShaderSupport
open Base.VertexDefs

open Geometry.GeometricModel

open GltfBase.Deployment2
open GltfBase.VGltfReader
open GltfBase.Gltf2Reader
open GltfBase.BaseObject

open GltfBase.Katalog

// ----------------------------------------------------------------------------------------------------
// Support für das Einlesen von glb-Files
// ----------------------------------------------------------------------------------------------------
module GlTf =

    let correctorGltf(path) = getGltf (path)

    [<AllowNullLiteral>]
    type GlTfBuilder(_objectName, _fileName) = 
        let mutable objekt:Objekt = null
        let mutable objectName = _objectName 
        let mutable fileName = _fileName 
        let mutable scale:Vector3 = Vector3.One
        let mutable gltf:Gltf = null
        let mutable container:GltfContainer = null
        let mutable store:ResourcesStore = null
        let mutable parts : List<Part> = new List<Part>()
        let mutable part : Part = null
        let mutable deployer = Deployer ()
                
        let defaultMaterial = MAT_NONE

        member this.Build(_scale:Vector3, _visibility:Visibility, _augment:Augmentation, _quality:Quality, _shaders:ShaderConfiguration) =
            scale <- _scale
            let correctorGtlf = correctorGltf(fileName)
            store <- this.Read(_objectName, fileName)
            objekt <- new Objekt(objectName, store.Gltf, Vector3.Zero, Vector4.Zero, _scale)             
            deployer.Deploy(objekt, store, correctorGtlf)

            for node in objekt.Nodes() do
                let mesh = deployer.MeshKatalog.GetMesh(objectName, node.Node.Mesh.Value)
                let material = deployer.MeshKatalog.Material(objectName, node.Node.Mesh.Value)
                let textures = deployer.TextureKatalog.GetTextures(objectName, material)
                let texture = 
                    textures 
                    |> List.find (fun text -> text.Kind = TextureTypePBR.baseColourTexture)

                this.AddPart(node.Node.Name, mesh.Vertices, mesh.Indices, defaultMaterial, texture, _visibility, _shaders) 

        member this.Initialize() =  
            objekt <- null
            objectName <- "" 
            gltf <- null
            store <- null

        member this.Read(_objectName, _path) = 
            this.Initialize()
            objectName  <- _objectName
            store       <- getStore(_path)
            gltf        <- store.Gltf
            container   <- store.Container 
            store

        member this.AddPart(_name, _vertices, _indices, _material, _texture, _visibility, _shaders) =
            let mutable myTexture = new Base.ModelSupport.Texture(_texture.Name, _texture.Data) 
            part <- 
                new Part(
                    _name,
                    new TriangularShape(_name, Vector3.Zero, _vertices, _indices, scale, Quality.High),
                    _material,
                    myTexture,
                    _visibility,
                    _shaders
                )
            parts.Add(part)

        member this.Parts =
            parts 
            |> Seq.toList
            
        member this.Vertices =
            parts 
            |> Seq.map(fun p -> p.Shape.Vertices)   
            |> Seq.concat
            |> Seq.toList 