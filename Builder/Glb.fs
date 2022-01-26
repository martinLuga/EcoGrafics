namespace Builder
//
//  Wavefront.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open Base
open Base.LoggingSupport 
open Base.ModelSupport
open Base.ShaderSupport 
open Base.VertexDefs
open Geometry.GeometricModel
open GlbFormat
open log4net
open SharpDX
open SharpDX.Direct3D
open SharpDX.Direct3D12
open System
open System.Collections.Generic
open VGltf
open VGltf.Types

// ----------------------------------------------------------------------------------------------------
// Support für das Einlesen von glb-Files
// ----------------------------------------------------------------------------------------------------
 
module Glb =

    let fileLogger = LogManager.GetLogger("File")
    let logFile  = Debug(fileLogger)

    let logger = LogManager.GetLogger("Builder.Glb")
    let logDebug = Debug(logger)

    let fromArray3(x:float32[]) =
        Vector3( x.[0],   x.[1],   x.[2])

    let fromArray2(x:float32[]) =
        Vector2( x.[0], x.[1])

    let myMaterial(mat:Material) = 
        new ModelSupport.Material(mat.Name)

    let myTexture(tex:Texture) = 
        new ModelSupport.Texture(tex.Name)

    let myTopologyType(src_typ:Nullable<Types.Mesh.PrimitiveType.ModeEnum>) =
        if src_typ.HasValue then
            let typ = src_typ.Value
            match typ with
            | Types.Mesh.PrimitiveType.ModeEnum.POINTS      -> PrimitiveTopologyType.Point
            | Types.Mesh.PrimitiveType.ModeEnum.LINES       -> PrimitiveTopologyType.Line
            | Types.Mesh.PrimitiveType.ModeEnum.TRIANGLES   -> PrimitiveTopologyType.Triangle
            | _ -> raise(SystemException("Not supported"))
        else 
            PrimitiveTopologyType.Triangle

    // ----------------------------------------------------------------------------------------------------
    // GeometryBuilder
    // ----------------------------------------------------------------------------------------------------
    type GlbBuilder(name, fileName: string) =
        
        let mutable container:GltfContainer = null
        let mutable store:ResourcesStore = null
        let mutable gltf:Gltf = null

        let mutable name = name 
        let mutable fileName = fileName 
        let mutable vertices = new List<Vertex>()
        let mutable indices = new List<int>()
        let mutable parts : List<Part> = new List<Part>()
        let mutable part : Part = null
        let mutable materials: Dictionary<string, ModelSupport.Material> = new Dictionary<string, ModelSupport.Material>()
        let mutable textures : Dictionary<string, ModelSupport.Texture>  = new Dictionary<string, ModelSupport.Texture>()
        let mutable generalSizeFactor = 1.0f
        let mutable augmentation = Augmentation.None
        let mutable isTransparent = false
        let mutable actualMaterial:ModelSupport.Material = null
        let mutable defaultMaterial:ModelSupport.Material = null
        let mutable actualTexture:ModelSupport.Texture = null
        let mutable lastTopology : PrimitiveTopology = PrimitiveTopology.Undefined
        let mutable lastTopologyType : PrimitiveTopologyType = PrimitiveTopologyType.Undefined

        do 
            container <- getContainer(fileName)
            gltf      <- container.Gltf
            store     <- getStore(container, loader )

        member this.AccessData(store:ResourcesStore) =
            let gltf = store.Gltf 
            let rootNodes = gltf.RootNodes |> Seq.toList
            let node:Node = rootNodes.Item(0)
            let childNode = gltf.Nodes.Item(node.Children[0])

            // Mesh
            let mesh = gltf.Meshes[childNode.Mesh.Value]
            let primitive = mesh.Primitives[0]
            lastTopologyType <- myTopologyType(primitive.Mode)

            // Vertex
            let normalBuffer = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["NORMAL"])             
            let normalen = normalBuffer.GetEntity<float32, Vector3> (fromArray3) 
            let ueberAlleNormalen  = normalen.GetEnumerable().GetEnumerator()

            let posBuffer  = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["POSITION"])
            let positionen = posBuffer.GetEntity<float32, Vector3> (fromArray3) 
            let ueberAllePositionen  = positionen.GetEnumerable().GetEnumerator()

            let texCoordBuffer = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["TEXCOORD_0"])
            let alleTexCoord = texCoordBuffer.GetEntity<float32, Vector2> (fromArray2) 
            let ueberAlleTexCoords  = alleTexCoord.GetEnumerable().GetEnumerator()

            while ueberAllePositionen.MoveNext() && ueberAlleNormalen.MoveNext() && ueberAlleTexCoords.MoveNext()  do
                let pos = ueberAllePositionen.Current
                let norm = ueberAlleNormalen.Current
                let tex = ueberAlleTexCoords.Current
                let vertex = new Vertex(pos, norm , Color4.White, tex)
                vertices.Add(vertex)

            // Index
            let indicies = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Indices.Value)
            indices.AddRange(indicies.GetPrimitivesAsCasted<int>())

            this.CreateMaterials(gltf.Materials)
            this.CreateTextures(gltf.Textures) 
 
        // ----------------------------------------------------------------------------------------------------
        //  Erzeugen der Meshdaten für eine Menge von Punkten
        // ----------------------------------------------------------------------------------------------------
        member this.Build(material:ModelSupport.Material, texture:ModelSupport.Texture, sizeFactor: float32, visibility:Visibility, augment:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
            augmentation        <- augment 
            generalSizeFactor   <- sizeFactor
            actualMaterial      <- material
            defaultMaterial     <- material
            actualTexture       <- texture
            isTransparent       <- visibility = Visibility.Transparent
            
            this.AccessData(store)

            this.AddPart(vertices, indices, defaultMaterial, actualTexture, visibility, shaders)

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

        member this.CreateMaterials(cmaterials) =
            materials.Clear()
            for cmat in cmaterials do
                let myMaterial = myMaterial(cmat)
                materials.Add(myMaterial.Name, myMaterial)   
                
        member this.CreateTextures(ctextures) =
            materials.Clear()
            for ctex in ctextures do
                let myTexture = myTexture(ctex)
                textures.Add(myTexture.Name, myTexture)