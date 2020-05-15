namespace DirectX
//
//  MeshGeometry.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic 
open SharpDX.Direct3D
open Base.Framework

open D3DUtilities

open VertexDefs

module MeshObjects =

     // ----------------------------------------------------------------------------------------------------
     // Mesh von Vertexes  
     // Mesh-Repräsentation eines Displayables    
     // ----------------------------------------------------------------------------------------------------
    type MeshData =
        struct 
            val Vertices    : Vertex[] 
            val Indices     : int[]          
            val Topology    : PrimitiveTopology
            new (vertices, indices, topology) = {Vertices=vertices; Indices=indices;Topology=topology}
            new (topology) = {Vertices=Array.empty; Indices=Array.empty;Topology=topology}
        end

    let createMeshData(meshData: Vertex[] * int[] * PrimitiveTopology) =
        let (mvertices, mindices, mtopology) = meshData
        new MeshData(mvertices, mindices, mtopology)

    let fromMeshData(meshData: MeshData) =
        (meshData.Vertices, meshData.Indices, meshData.Topology) 

    let meshCompose (meshData1:MeshData) (meshData2:MeshData) =
        let vertices = Array.append meshData1.Vertices meshData2.Vertices 
        let meshData2Indices = meshData2.Indices |> Array.map (fun i -> i + meshData1.Vertices.Length)
        let indices = Array.append meshData1.Indices meshData2Indices  
        new MeshData(vertices, indices, meshData1.Topology)

    // ----------------------------------------------------------------------------------------------------
    // Mesh-Repräsentation eines Gebildes
    // bestehend aus mehreren Teilen
    // ----------------------------------------------------------------------------------------------------
    type MeshPart =
        struct  
            val Name                : string
            val MaterialIndex       : int
            val TextureIndex        : int
            val IndexCount          : int
            val BaseVertexLocation  : int
            val StartIndexLocation  : int
            new (name, materialIndex, textureIndex, indexCount, baseVertexLocation, startIndexLocation) ={Name=name; MaterialIndex=materialIndex; TextureIndex=textureIndex; IndexCount=indexCount; BaseVertexLocation=baseVertexLocation; StartIndexLocation=startIndexLocation}
            new (name) ={Name=name; MaterialIndex=0; TextureIndex=0; IndexCount=0; BaseVertexLocation=0; StartIndexLocation=0}
        end

    // ----------------------------------------------------------------------------------------------------
    // Mesh-Repräsentation eines Gebildes
    // bestehend aus mehreren Teilen
    // Neuanlage mit dem ersten Teil
    // ----------------------------------------------------------------------------------------------------
    type MeshObject (name) =
        let mutable name  = name
        let mutable parts = new  Dictionary<string, MeshPart>()
        let mutable data  = new  MeshData(PrimitiveTopology.TriangleList)
        let mutable vertexStart = 0
        let mutable indexStart = 0
        member this.Name  
            with get() = name
            and set(value) = name <- value
        member this.Parts 
            with get() = parts
            and set(value) = parts <- value 
        member this.Data  
            with get() = data
            and set(value) = data <- value
        member this.VertexStart
                with get() = vertexStart
                and set(value) = vertexStart <- value
        member this.IndexStart
                with get() = indexStart
                and set(value) = indexStart <- value

    let partOfObject(object:MeshObject, name:string) =
        let mutable result = new MeshPart(name)
        if object.Parts.TryGetValue(name, ref result) then
            result
        else 
            result <- new MeshPart(name)
            result

    // Vertices und Indices verlängern (compose)
    // Neues part hinzufügen
    // Index MaxValue = no value
    let meshDataToObject(object:MeshObject, name:string, mdata:MeshData, materialIndex:int, textureIndex:int) =
        object.Data <- meshCompose object.Data mdata
        let part = new MeshPart(name, materialIndex, textureIndex, mdata.Indices.Length, object.VertexStart, object.IndexStart)
        object.Parts.Add(name, part)
        object.VertexStart <- object.VertexStart + mdata.Vertices.Length
        object.IndexStart  <- object.IndexStart  + mdata.Indices.Length

    // ----------------------------------------------------------------------------------------------------
    // Mesh-Gesamtheit  
    // ----------------------------------------------------------------------------------------------------
    type MeshScene =
        struct  
            val Name    :string
            val Objects   :Dictionary<string, MeshObject>
            new (name) = {Name=name;Objects=new Dictionary<string, MeshObject>()}
        end

    let objectOfScene(world:MeshScene, name:string) =
        let mutable result = new MeshObject(name)
        match world.Objects.TryGetValue name with
        | true, value -> result <- value
        | _           -> world.Objects.Add(name, result) 
        result
