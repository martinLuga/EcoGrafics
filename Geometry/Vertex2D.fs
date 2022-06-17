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
// Vertexes erzeugen  
// für 2D Elemente
// ----------------------------------------------------------------------------------------------------
module Square2D =

    // ----------------------------------------------------------------------------------------------------
    // Quadrat als Folge von 4 Linien in der XY-Ebene 
    // ---------------------------------------------------------------------------------------------------- 
    let squareVertices (p1:Vector3, p2:Vector3, p3:Vector3, p4:Vector3, color:Color, isTransparent) = 

        let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()

        let v1 = createVertex p1 Vector3.UnitZ  color4  (new Vector2(0.0f, 0.0f))  
        let v2 = createVertex p2 Vector3.UnitZ  color4  (new Vector2(1.0f, 0.0f))  
        let v3 = createVertex p3 Vector3.UnitZ  color4  (new Vector2(1.0f, 1.0f))  
        let v4 = createVertex p4 Vector3.UnitZ  color4  (new Vector2(0.0f, 1.0f))  

        let vert = seq{v1;  v2; v3; v4; v1} |> Seq.toArray
        let ind = seq{0;1;2;3;0} |> Seq.toArray
        new MeshData<Vertex>(vert, ind)

    let CreateMeshData(p1, p2, p3, p4,color:Color, visibility:Visibility, quality:Quality) =
        let isTransparent = TransparenceFromVisibility(visibility)
        squareVertices (p1, p2, p3, p4,color, isTransparent)

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

            let cv3 = new Vector3(circleVector.X, circleVector.Z, 0.0f) 
            let textureCoordinate = Vector3.Add(Vector3.Multiply(cv3, textureScale), g_XMOneHalf) 
            let cv2 = Vector2(textureCoordinate.X, textureCoordinate.Y)

            vertices.Add(                
                createVertex position normal color4 cv2  
            )

        (vertices, indices)

    let circleVertices (origin,  color, radius, tessellation, isTransparent) =
        let mutable (verticesU, indicesU) = CreateCircularPlane(origin, tessellation, radius, color, isTransparent)
        reverseWinding &verticesU  &indicesU 
        MeshData.Create(verticesU, indicesU)

    let CreateMeshData(origin:Vector3,  color:Color, radius:float32, tessellation:int, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        circleVertices (origin, color, radius, tessellation, isTransparent ) 

module Line2D =
    // ----------------------------------------------------------------------------------------------------
    // Linie In der XY-Ebene
    // ----------------------------------------------------------------------------------------------------
    let lineVertices (ursprung:Vector3, target:Vector3, color:Color, isTransparent) = 

        let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()

        let v1 = createVertex ursprung Vector3.UnitZ color4  (new Vector2(0.0f, 0.0f))   
        let v2 = createVertex target Vector3.UnitZ   color4  (new Vector2(0.0f, 0.0f))  

        let vert = seq{v1;v2} |> Seq.toArray
        let ind = seq{0;1} |> Seq.toArray
        new MeshData<Vertex>(vert, ind)

    let CreateMeshData(ursprung:Vector3, target:Vector3, color:Color, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        lineVertices (ursprung, target, color, isTransparent)