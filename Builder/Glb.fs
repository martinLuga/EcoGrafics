namespace Builder
//
//  Wavefront.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open Base
open Base.PrintSupport 
open Base.ModelSupport
open Base.ShaderSupport 
open Base.VertexDefs

open Geometry.GeometricModel3D 

open VGltf
open VGltf.Types

open SharpDX
open SharpDX.Direct3D
open SharpDX.Direct3D12 

open System.Collections.Generic 

open GlbFormat

// ----------------------------------------------------------------------------------------------------
// Support für das Einlesen von glb-Files (physically based rendering)
// ----------------------------------------------------------------------------------------------------
 
module Glb =

    type Worker(fileName: string) =
        
        let mutable store:ResourcesStore = null
        let mutable gltf:Gltf = null

        let mutable size = Vector3.One

        let mutable fileName        = fileName 
        let mutable vertices        = new List<Vertex>()
        let mutable indices         = new List<int>()        
        let mutable materials       = new Dictionary<string, ModelSupport.Material>()
        let mutable textures        = new Dictionary<string, ModelSupport.Texture>()
        let mutable topologyType    = PrimitiveTopologyType.Triangle

        do 
            let _store = getContainer(fileName)
            store       <- _store           // Der Store enthält alles
            gltf        <- store.Gltf       // Gltf zeigt in den Store

        member this.Vertices 
            with get() = vertices

        member this.Indices 
            with get() = indices

        member this.Initialize(_generalSizeFactor) =
            size <- _generalSizeFactor

            // Node
            let gltf = store.Gltf 
            let sceneIdx = gltf.Scene 
            let scenes = gltf.Scenes 
            let scene = scenes.Item(sceneIdx.Value)

            // Node
            let rootNodes = gltf.RootNodes |> Seq.toList
            let node:Node = rootNodes.Item(0)
            let childNode = gltf.Nodes.Item(node.Children[0])

            // Mesh
            let mesh = gltf.Meshes[childNode.Mesh.Value]
            let primitive = mesh.Primitives[0]
            topologyType <- myTopologyType(primitive.Mode)

            // Material
            let material = gltf.Materials[primitive.Material.Value]
            let roughness = material.PbrMetallicRoughness 
            let bct = roughness.BaseColorTexture 
            let bcti = bct.Index 
            let mf = roughness.MetallicFactor 

            // Material
            let material = gltf.Materials[primitive.Material.Value]
            let roughness = material.PbrMetallicRoughness 
            let bct = roughness.BaseColorTexture 
            let bcti = bct.Index 
            let mf = roughness.MetallicFactor 

            // Textures
            let texture = gltf.Textures[bcti]

            // Vertex
            let posBuffer  = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["POSITION"])
            let positionen = posBuffer.GetEntity<float32, Vector4> (Mapper<float32, Vector4>(toArray4AndIntFromFloat32)) 
            let ueberAllePositionen  = positionen.AsArray().GetEnumerator()

            let normalBuffer = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["NORMAL"])             
            let normalen = normalBuffer.GetEntity<float32, Vector3> (Mapper<float32, Vector3>(toArray3AndIntFromFloat32))
            let ueberAlleNormalen  = normalen.AsArray().GetEnumerator()

            let texCoordBuffer = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["TEXCOORD_0"])
            let alleTexCoord = texCoordBuffer.GetEntity<float32, Vector2>  (Mapper<float32, Vector2>(toArray2AndIntFromFloat32))
            let ueberAlleTexCoords  = alleTexCoord.AsArray().GetEnumerator()

            // Vertex
            let meshVertices = new List<Vertex>()

            while ueberAllePositionen.MoveNext()
                  && ueberAlleNormalen.MoveNext()
                  && ueberAlleTexCoords.MoveNext() do
                let pos =  ueberAllePositionen.Current :?> Vector4 
                let norm = ueberAlleNormalen.Current :?> Vector3 
                let tex = ueberAlleTexCoords.Current :?> Vector2 
                let vertex = new  Vertex(Vector3(pos.X, pos.Y, pos.Z), norm ,tex)
                vertices.Add(vertex)

            // Index
            let indicies = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Indices.Value)
            indices.AddRange(indicies.GetPrimitivesAsInt())

        member this.CreateMaterials(cmaterials) =
            materials.Clear()
            for cmat in gltf.Materials do
                let myMaterial = myMaterial(cmat)
                materials.Add(myMaterial.Name, myMaterial)   
                
        member this.CreateTextures(ctextures) =
            materials.Clear()
            for ctex in gltf.Textures do
                let myTexture = myTexture(ctex)
                textures.Add(myTexture.Name, myTexture)

        member this.CreateImages () =
            for  i in 0.. gltf.Images.Count-1 do
                let img = gltf.Images[i];
                let imgResN = store.GetOrLoadImageResourceAt(i) 
                let myTexture = new ModelSupport.Texture(img.Name, img.Uri, "", false)
                textures.Add(myTexture.Name, myTexture)

    // ----------------------------------------------------------------------------------------------------
    // GeometryBuilder
    // ----------------------------------------------------------------------------------------------------
    type GlbBuilder(name, fileName: string) =
        
        let worker = new Worker(fileName)

        let mutable name = name 
        let mutable parts : List<Part> = new List<Part>()
        let mutable part : Part = null
        let mutable generalSizeFactor = Vector3.One
        let mutable augmentation = Augmentation.None
        let mutable isTransparent = false
        let mutable actualMaterial:ModelSupport.Material = null
        let mutable defaultMaterial:ModelSupport.Material = null
        let mutable actualTexture:ModelSupport.Texture = null
        let mutable lastTopology : PrimitiveTopology = PrimitiveTopology.Undefined
        let mutable lastTopologyType : PrimitiveTopologyType = PrimitiveTopologyType.Undefined
 
        // ----------------------------------------------------------------------------------------------------
        //  Erzeugen des Gltf Models
        // ----------------------------------------------------------------------------------------------------
        member this.Build(material:ModelSupport.Material, texture:ModelSupport.Texture, sizeFactor: Vector3, visibility:Visibility, augment:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
            augmentation        <- augment 
            generalSizeFactor   <- sizeFactor
            actualMaterial      <- material
            defaultMaterial     <- material
            actualTexture       <- texture
            isTransparent       <- visibility = Visibility.Transparent
            
            worker.Initialize(generalSizeFactor)
            this.AddPart(worker.Vertices, worker.Indices, defaultMaterial, actualTexture, visibility, shaders) 

        // ----------------------------------------------------------------------------------------------------
        //  Erzeugen des Parts
        // ----------------------------------------------------------------------------------------------------
        member this.AddPart(vertices, indices, material, texture, visibility, shaders) =
            part <- 
                new Part(
                    name,
                    new TriangularShape(name, Vector3.Zero, vertices, indices, generalSizeFactor, Quality.High),
                    material,
                    texture,
                    visibility,
                    shaders
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