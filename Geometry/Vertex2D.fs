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

// ----------------------------------------------------------------------------------------------------
// Vertexes für Kreis 2D erzeugen  
// ----------------------------------------------------------------------------------------------------
module Circle2D =

    let g_XMOneHalf  = Vector3( 0.5f,  0.5f,  0.5f)
    let g_XMNegateX  = Vector3(-1.0f,  1.0f,  1.0f)
    let g_XMNegateY  = Vector3( 1.0f, -1.0f,  1.0f)
    let g_XMNegateZ  = Vector3( 1.0f,  1.0f, -1.0f)
    let g_XMNegativeOneHalf = Vector3(-0.5f, -0.5f, -0.5f)
    let g_XMPositiveOneHalf = Vector3( 0.5f,  0.5f,  0.5f)
    let g_XMIdentityR1 = Vector3( 0.0f, 1.0f, 0.0f)

    // ----------------------------------------------------------------------------------------------------
    // Als Punkte
    // ---------------------------------------------------------------------------------------------------- 
    let GetPoints (origin:Vector3, radius:float32, tessellation:int) =
        let mutable points = new List<Vector3>() 
        // Create circle points.
        for i = 0 to tessellation - 1 do  
            let circleVector = CreateCircleVector(i, tessellation)
            let point = origin + Vector3.Add(Vector3.Multiply(circleVector, radius), Vector3.Multiply(g_XMIdentityR1, 0.0f))            
            points.Add(point)
        points.Reverse()
        points

// ----------------------------------------------------------------------------------------------------
// Linie in der Ebene
// ----------------------------------------------------------------------------------------------------
module Line2D =

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

// ----------------------------------------------------------------------------------------------------
//  Erzeugen der Meshdaten für ein Polygon. 
// ----------------------------------------------------------------------------------------------------
module Polygon2D =
    
    let CreatePoints(contour: Vector3[] , color, transparency ) =
        Generic.polygonPointList(contour, color, transparency)

    let CreateLines (contour: Vector3[], color, transparency ) = 
        Generic.polygonLineList(contour, color, transparency)

    let CreateTriangles(center: Vector3, contour: Vector3[], color, transparency ) = 
        Generic.polygonTriangleList center contour color transparency 

// ----------------------------------------------------------------------------------------------------
// Quadrat durch 4 Punkte  
// ----------------------------------------------------------------------------------------------------  
module Square2D =

    open GeometricTypes

    let CreatePoints(p1:Vector3, p2:Vector3, p3:Vector3, p4:Vector3) =
        let mutable points = new List<Vector3>() 
        points.Add(p1)
        points.Add(p2)
        points.Add(p3)
        points.Add(p4)
        points |>Seq.toArray

    let CreateLines(p1, p2, p3, p4) =
        let mutable lines = new List<Vector3*Vector3>() 
        lines.Add(p1, p2)
        lines.Add(p2, p3)
        lines.Add(p3, p4)
        lines.Add(p4, p1)
        lines 

    let CreateTriangles(p1, p2, p3, p4, normal, transparent) =
         square p1  p2  p3  p4 normal Color.White 0 transparent

// ----------------------------------------------------------------------------------------------------
// Zusammenführen  
// ----------------------------------------------------------------------------------------------------  
module Construction =

    open GeometricTypes

    // ----------------------------------------------------------------------------------------------------
    // Typ list -> Vertices, Indices 
    // Für das Erzeugen der Meshdata
    // ----------------------------------------------------------------------------------------------------  
    let FromTriangles (triangles: List<TriangleType> * List<TriangleIndexType>, upper:bool) =
        let vertices = new List<Vertex>()
        let indices = new List<int>()

        for tri in (fst triangles) do
            vertices.AddRange(deconstructTriangle (tri))

        for ind in (snd triangles) do
            if upper then 
                indices.AddRange(triangleIndicesClockwise (ind))
            else 
                indices.AddRange(triangleIndicesCounterClockwise (ind))

        vertices, indices 

    let FromLines(lines: List<LineType> * List<LineIndexType>)=
        let vertices = new List<Vertex>()
        let indices = new List<int>()

        for line in (fst lines) do
            vertices.AddRange(deconstructLine (line))

        for ind in (snd lines) do
            indices.AddRange(lineIndices (ind))
            
        vertices, indices 

    let FromPoints(points: List<PointType> * List<PointIndexType>)=
        let vertices = new List<Vertex>()
        let indices = new List<int>()

        for point in (fst points) do
            vertices.AddRange(deconstructPoint (point))

        for ind in (snd points) do
            indices.AddRange(pointIndices (ind)) 
            
        vertices, indices 