namespace Geometry
//
//  Vertex2D.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic

open SharpDX

open Base.VertexDefs
open Base.MeshObjects
open Base.ModelSupport

open GeometricTypes
 

// ----------------------------------------------------------------------------------------------------
// Vertexes erzeugen  
// für 2D Elemente
// ----------------------------------------------------------------------------------------------------
module Square2D =

    // ----------------------------------------------------------------------------------------------------
    // Quadrat als Verbindung von 4 Punkten in der XY-Ebene 
    // ---------------------------------------------------------------------------------------------------- 
    let CreateVertexData(p1, p2, p3, p4, color:Color, visibility:Visibility, quality:Quality) =
        let isTransparent = TransparenceFromVisibility(visibility)
        let squareBot, squareIndexBot = square p1  p2  p3  p4  -Vector3.UnitY color 0 isTransparent
        let squareList = squareVerticesClockwise squareBot |> List<Vertex> 
        let squareIndexList = squareIndicesClockwise squareIndexBot  |> List<int> 
        squareList, squareIndexList

module Circle2D =

    let g_XMOneHalf  = Vector3( 0.5f,  0.5f,  0.5f)
    let g_XMNegateX  = Vector3(-1.0f,  1.0f,  1.0f)
    let g_XMNegateY  = Vector3( 1.0f, -1.0f,  1.0f)
    let g_XMNegateZ  = Vector3( 1.0f,  1.0f, -1.0f)
    let g_XMNegativeOneHalf = Vector3(-0.5f, -0.5f, -0.5f)
    let g_XMPositiveOneHalf = Vector3( 0.5f,  0.5f,  0.5f)
    let g_XMIdentityR1 = Vector3( 0.0f, 1.0f, 0.0f)

    // ----------------------------------------------------------------------------------------------------
    // Kreis in der XY-Ebene 
    // ---------------------------------------------------------------------------------------------------- 
    let CreateCircularPlane (origin:Vector3, tessellation:int, radius:float32, color:Color, isTransparent:bool) =
    
        let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()
        let mutable vertices = new List<Vertex>() 
        let mutable points = new List<Vector3>() 
        let indices =  new List<int>()

        let shift = Vector3(radius, 0.0f, radius)
        let center = origin + shift

        // Create cap indices.
        for i = 0 to tessellation - 2 do 
 
            let mutable i1 = (i + 1) % tessellation 
            let mutable i2 = (i + 2) % tessellation 

            swap &i1 &i2  

            let vbase = vertices.Count
            indices.Add(vbase) 
            indices.Add(vbase + i1) 
            indices.Add(vbase + i2)  

        let mutable normal = g_XMIdentityR1
        let mutable textureScale = g_XMNegativeOneHalf
        let mutable textureScale = g_XMPositiveOneHalf

        // Create cap vertices.
        for i = 0 to tessellation - 1 do 
 
            let circleVector = CreateCircleVector(i, tessellation)
            let position = center + Vector3.Add(Vector3.Multiply(circleVector, radius), Vector3.Multiply(g_XMIdentityR1, 0.0f))            
            points.Add(position)

            let cv3 = new Vector3(circleVector.X, circleVector.Z, 0.0f) 
            let textureCoordinate = Vector3.Add(Vector3.Multiply(cv3, textureScale), g_XMOneHalf) 
            let cv2 = Vector2(textureCoordinate.X, textureCoordinate.Y)
            let vertex = createVertex position normal color4 cv2 
            vertices.Add(vertex)

        (points, vertices, indices)

    let circleVertices (origin,  color, radius, tessellation, isTransparent) =
        let mutable (points, verticesU, indicesU) = CreateCircularPlane(origin, tessellation, radius, color, isTransparent)
        reverseWinding &verticesU  &indicesU 
        MeshData.Create(verticesU, indicesU)

    let CreateMeshData(origin:Vector3, color:Color, radius:float32, tessellation:int, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        circleVertices (origin, color, radius, tessellation, isTransparent) 

    let CreatePointData(origin:Vector3, color:Color, radius:float32, tessellation:int, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        let mutable (points, verticesU, indicesU) = CreateCircularPlane(origin, tessellation, radius, color, isTransparent) 
        points

module Line2D =
    // ----------------------------------------------------------------------------------------------------
    // Linie in der Ebene
    // ----------------------------------------------------------------------------------------------------
    let lineFromTo (ursprung:Vector3, target:Vector3, color:Color, isTransparent) = 
        let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()

        let v1 = createVertex ursprung Vector3.UnitZ color4  (new Vector2(0.0f, 0.0f))   
        let v2 = createVertex target Vector3.UnitZ   color4  (new Vector2(0.0f, 0.0f))  

        let vert = seq{v1;v2} |> Seq.toArray
        let ind = seq{0;1} |> Seq.toArray
        vert, ind

    let vertexeFromPoints (points:seq<Vector3>, color:Color, isTransparent) = 
        let mutable idx = 0
        let nextIdx() =
            idx <- idx + 1
            idx
        let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()
        points 
        |> Seq.map (fun p -> createVertex p Vector3.UnitZ color4 (new Vector2(0.0f, 0.0f)))
        |> Seq.toArray ,
        points 
        |> Seq.map (fun p -> nextIdx())
        |> Seq.toArray  

    let CreateMeshData(ursprung:Vector3, target:Vector3, color:Color, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        let vert, ind = lineFromTo (ursprung, target, color, isTransparent)
        new MeshData<Vertex>(vert, ind)

module PolygonPatch =
    
    open Generic
    
    // ----------------------------------------------------------------------------------------------------
    //  Erzeugen der Meshdaten für ein Polygon. 
    // ----------------------------------------------------------------------------------------------------
    let CreateVertexData (center: Vector3, contour: Vector3[], color, visibility ) = 
        let isTransparent = TransparenceFromVisibility(visibility) 
        let polygon = polygonTriangleList center contour color isTransparent 
        let (verticesLower, indicesLower) = polygon
        let verticesL = verticesLower |> List.ofSeq |> List.collect (fun q -> triangleVertices q) |> List<Vertex> 
        let indicesL = indicesLower  |> List.ofSeq |> List.collect (fun ind -> triangleIndicesCounterClockwise ind) |> List<int> 
        verticesL, indicesL
    
    let CreateMeshData(center: Vector3, contour: Vector3[], color, visibility:Visibility) =
        let mutable (verticesL, indicesL) = CreateVertexData (center, contour,  color, visibility)
        MeshData.Create(verticesL, indicesL)    