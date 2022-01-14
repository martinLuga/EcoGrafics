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

open SharpDX

module MeshObjects =

    // ----------------------------------------------------------------------------------------------------
    // Mesh von Vertexes  
    // Mesh-Repräsentation eines Displayables    
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type MeshData<'VERTX > (vertices:'VERTX[], indices:int[]) =
        let mutable vertices:List<'VERTX> = vertices |> ResizeArray<'VERTX>
        let mutable indices :List<int> = indices |> ResizeArray<int>

        new() = new MeshData<'VERTX>(  [||], [||])
        new(vertAndInd ) = new MeshData<'VERTX>(fst vertAndInd, snd vertAndInd)
        new(len) = new MeshData<'VERTX>(Array.zeroCreate len, Array.zeroCreate len)

        static member Create<'VERTX>(vertices:List<'VERTX>, indices :List<int>) =
            let instance = new MeshData<'VERTX>(vertices.ToArray(), indices.ToArray())
            instance.Vertices <- vertices
            instance.Indices <- indices
            instance

        static member Compose(meshData1:MeshData<'VERTX>, meshData2:MeshData<'VERTX>) =
            let m1Anz = meshData1.AnzVertices()
            let result = new MeshData<'VERTX>()
            result.AddVertices(meshData1.Vertices)
            result.AddVertices(meshData2.Vertices)            
            result.AddIndices(meshData1.Indices)
            let meshData2Indices = meshData2.Indices |> Seq.map (fun i -> i + m1Anz)
            result.AddIndices(meshData2Indices)
            result
        
        member this.Copy() =
            let result = new MeshData<'VERTX>()
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
 
        member this.ToTriangles() =
            this.Indices
            |> Seq.map (fun i -> this.Vertices.Item(i))    // Vertices in order of indices
            |> Seq.chunkBySize (3)                         // Group to 3 vertices

        //member inline this.Minimum() = 
        //    computeMinimum (Seq.map (fun (v:'VERTX) -> v.Position) vertices |>  Seq.toList) 

        //member inline this.Maximum<'VERTX when 'VERTX: (member Position: Vector3)> () = 
        //    let positions = vertices |> Seq.map (fun (v:'VERTX) -> v.Position) 
        //    computeMaximum (Seq.map (fun (v:'VERTX) -> v.Position) vertices |> Seq.toList) 

        //member inline this.Center<'VERTX when 'VERTX: (member Position: Vector3)> () =
        //    computeSchwerpunkt ( 
        //        Seq.map (fun (v:'VERTX) -> v.Position) vertices |> Seq.toList
        //    )
        //member inline this.Resize(aFactor: float32) =
        //    for i = 0 to vertices.Count - 1 do
        //        let mutable resizedVertex = vertices.Item(i)
        //        resizedVertex.Position <- vertices.Item(i).Position * aFactor
        //        vertices.Item(i) <- resizedVertex

        //member this.OfSize(aFactor: float32) =
        //    this.Resize(aFactor)
        //    this

        member this.Properties =
            "MeshData"
            + " V: "
            + vertices.Count.ToString()
            + " I: "
            + indices.Count.ToString()



        member this.Log(debugFun) = 
            debugFun("--------- " ) 
            for i = 0 to indices.Count - 1 do
                debugFun("v(" + indices.[i].ToString() + ") "+ vertices.[i].ToString())
            debugFun("-------------------------------------")


    let fromMeshData(meshData: MeshData<'VERTX>) =
        (meshData.Vertices, meshData.Indices)

    let stdMeshData = new MeshData<Vertex>()