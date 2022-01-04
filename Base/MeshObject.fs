namespace  Base
//
//  MeshGeometry.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open System.Collections.Generic 

open VertexDefs

open MathSupport

module MeshObjects =

    // ----------------------------------------------------------------------------------------------------
    // Mesh von Vertexes  
    // Mesh-Repräsentation eines Displayables    
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type MeshData(vertices:Vertex[], indices:int[]) =
        let mutable vertices:List<Vertex> = vertices |> ResizeArray<Vertex>
        let mutable indices :List<int> = indices |> ResizeArray<int>

        new() = new MeshData([||], [||])
        new(vertAndInd: Vertex [] * int []) = new MeshData(fst vertAndInd, snd vertAndInd)
        new(len) = new MeshData(Array.zeroCreate len, Array.zeroCreate len)

        static member Create(vertices:List<Vertex>, indices :List<int>) =
            let instance = new MeshData()
            instance.Vertices <- vertices
            instance.Indices <- indices
            instance

        static member Compose(meshData1:MeshData, meshData2:MeshData) =
            let m1Anz = meshData1.AnzVertices()
            let result = new MeshData()
            result.AddVertices(meshData1.Vertices)
            result.AddVertices(meshData2.Vertices)            
            result.AddIndices(meshData1.Indices)
            let meshData2Indices = meshData2.Indices |> Seq.map (fun i -> i + m1Anz)
            result.AddIndices(meshData2Indices)
            result
        
        member this.Copy() =
            let result = new MeshData()
            result.AddVertices(this.Vertices)
            result.AddIndices(this.Indices) 
            result

        member this.Vertices
            with get () = vertices
            and set (value) = vertices <- value

        member this.Indices
            with get () = indices
            and set (value) = indices <- value

        member this.Add(vertex, index) =
            vertices.Add(vertex)
            indices.Add(index)

        member this.AddVertices(newVertices)=
            for v in newVertices do
                vertices.Add(v)

        member this.AnzVertices() =
            vertices.Count

        member this.AddIndices(newIndices: int seq) =
            for i in newIndices do
                indices.Add(i)

        member this.Minimum = 
            computeMinimum (Seq.map (fun (v:Vertex) -> v.Position) vertices |>  Seq.toList) 

        member this.Maximum = 
            computeMaximum (Seq.map (fun (v:Vertex) -> v.Position) vertices |> Seq.toList) 

        member this.Center() =
            computeSchwerpunkt ( 
                Seq.map (fun (v:Vertex) -> v.Position) vertices |> Seq.toList
            )

        member this.Properties =
            "MeshData"
            + " V: "
            + vertices.Count.ToString()
            + " I: "
            + indices.Count.ToString()

        member this.Resize(aFactor: float32) =
            for i = 0 to vertices.Count - 1 do
                let mutable resizedVertex = vertices.Item(i)
                resizedVertex.Position <- vertices.Item(i).Position * aFactor
                vertices.Item(i) <- resizedVertex

        member this.OfSize(aFactor: float32) =
            this.Resize(aFactor)
            this

        member this.Log(debugFun) = 
            debugFun("--------- " ) 
            for i = 0 to indices.Count - 1 do
                debugFun("v(" + indices.[i].ToString() + ") "+ vertices.[i].ToString())
            debugFun("-------------------------------------")

        member this.Subdivide() =         
            // Save a copy of the input geometry.
            let verticesCopy = this.Vertices.ToArray() 
            let indicesCopy = this.Indices.ToArray() 
            
            this.Vertices.Clear()
            this.Indices.Clear()
            
            //       v1
            //       *
            //      / \
            //     /   \
            //  m0*-----*m1
            //   / \   / \
            //  /   \ /   \
            // *-----*-----*
            // v0    m2     v2
            
            let numTriangles = indicesCopy.Length / 3
            for   i in 0 .. numTriangles-1 do
         
                let v0 = verticesCopy[indicesCopy[i * 3 + 0]]
                let v1 = verticesCopy[indicesCopy[i * 3 + 1]]
                let v2 = verticesCopy[indicesCopy[i * 3 + 2]]
            
                //
                // Generate the midpoints.
                //
            
                let m0 = MidPoint(v0, v1)
                let m1 = MidPoint(v1, v2)
                let m2 = MidPoint(v0, v2)
            
                //
                // Add new geometry.
                //            
                this.Vertices.Add(v0) // 0
                this.Vertices.Add(v1) // 1
                this.Vertices.Add(v2) // 2
                this.Vertices.Add(m0) // 3
                this.Vertices.Add(m1) // 4
                this.Vertices.Add(m2) // 5
            
                this.Indices.Add(i * 6 + 0)
                this.Indices.Add(i * 6 + 3)
                this.Indices.Add(i * 6 + 5)
            
                this.Indices.Add(i * 6 + 3)
                this.Indices.Add(i * 6 + 4)
                this.Indices.Add(i * 6 + 5)
            
                this.Indices.Add(i * 6 + 5)
                this.Indices.Add(i * 6 + 4)
                this.Indices.Add(i * 6 + 2)
            
                this.Indices.Add(i * 6 + 3)
                this.Indices.Add(i * 6 + 1)
                this.Indices.Add(i * 6 + 4) 

        member this.Subdivide(numSubdivisions) =
            for  i in 0 .. numSubdivisions-1 do
                this.Subdivide()

    let fromMeshData(meshData: MeshData) =
        (meshData.Vertices, meshData.Indices)

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
        let mutable data  = new  MeshData()
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
        object.Data <- MeshData.Compose(object.Data, mdata)
        let part = new MeshPart(name, materialIndex, textureIndex, mdata.Indices.Count, object.VertexStart, object.IndexStart)
        object.Parts.Add(name, part)
        object.VertexStart <- object.VertexStart + mdata.Vertices.Count
        object.IndexStart  <- object.IndexStart  + mdata.Indices.Count

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