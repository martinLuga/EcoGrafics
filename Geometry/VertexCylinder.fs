namespace Geometry
//
//  open Vertex.Cylinder.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX 

open System.Collections.Generic

open Base.GlobalDefs

open Base.VertexDefs 
open Base.MeshObjects
open Base.ModelSupport

// ----------------------------------------------------------------------------------------------------
// Vertexes erzeugen  
// Für einen Zylinder
// ---------------------------------------------------------------------------------------------------- 
module VertexCylinder =

    let mutable tessellation:int32 = 32

    let g_XMOneHalf  = Vector3( 0.5f,  0.5f,  0.5f)
    let g_XMNegateX  = Vector3(-1.0f,  1.0f,  1.0f)
    let g_XMNegateY  = Vector3( 1.0f, -1.0f,  1.0f)
    let g_XMNegateZ  = Vector3( 1.0f,  1.0f, -1.0f)
    let g_XMNegativeOneHalf = Vector3(-0.5f, -0.5f, -0.5f)
    let g_XMIdentityR1 = Vector3( 0.0f, 1.0f, 0.0f)

    let adjustCenterToUrsprung (vertices: List<Vertex>, radius: float32) =
        let shift = Vector3(radius, 0.0f, radius)

        vertices
        |> Seq.map (fun (v: Vertex) -> shiftVertex (v, shift))
        |> ResizeArray<Vertex>

    // ----------------------------------------------------------------------------------------------------
    // Helper
    // ----------------------------------------------------------------------------------------------------
    let swap (left : 'a byref) (right : 'a byref) =
      let temp = left
      left <- right
      right <- temp
 
    // Helper computes a point on a unit circle, aligned to the x/z plane and centered on the origin.
    let CreateCircleVector (i,tessellation:int) =
        let angle  = float32 i * IIpi  / float32 tessellation 
        let dx =  sin angle 
        let dz =  cos angle  
        new Vector3(dx, 0.0f, dz) 

    let reverseWinding (vertices: List<Vertex> byref) (indices: List<int> byref) =
        assert((indices.Count % 3) = 0) 

        for i in 0 .. 3 .. indices.Count - 3 do
            let mutable i1 = indices.Item(i)
            let mutable i2 = indices.Item(i + 2)
            indices.Item(i) <- i2
            indices.Item(i + 2) <- i1

        let mutable newVertices = List<Vertex>()

        for vert in vertices do 
            newVertices.Add( 
                Vertex(vert.Position,vert.Normal,vert.Color,new Vector2(1.0f - vert.Texture.X, vert.Texture.Y) )                 
            )
        vertices <- newVertices

    // ----------------------------------------------------------------------------------------------------
    // Cylinder Cap  
    // ----------------------------------------------------------------------------------------------------
    let CreateCylinderCap (origin:Vector3, tessellation:int, height:float32, radius:float32, color:Color, isTop:bool, isTransparent:bool) =
    
        let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()
        let mutable vertices = new List<Vertex>() 
        let indices =  new List<int>()

        let shift = Vector3(radius, 0.0f, radius)
        let center = origin + shift

        // Create cap indices.
        for i = 0 to tessellation - 2 do 
 
            let mutable i1 = (i + 1) % tessellation 
            let mutable i2 = (i + 2) % tessellation 

            if isTop then 
                swap &i1 &i2  

            let vbase = vertices.Count
            indices.Add(vbase) 
            indices.Add(vbase + i1) 
            indices.Add(vbase + i2)  

        let mutable normal = g_XMIdentityR1
        let mutable textureScale = g_XMNegativeOneHalf

        if not isTop then  
            normal <- Vector3.Negate(normal) 
            textureScale <- Vector3.Multiply(textureScale, g_XMNegateX)  
        
        // Create cap vertices.
        for i = 0 to tessellation - 1 do 
 
            let circleVector = CreateCircleVector(i, tessellation)

            let position = center + Vector3.Add(Vector3.Multiply(circleVector, radius), Vector3.Multiply(g_XMIdentityR1, height)) 

            let cv3 = new Vector3(circleVector.X, circleVector.Z, 0.0f) 
            let textureCoordinate = Vector3.Add(Vector3.Multiply(cv3, textureScale), g_XMOneHalf) 
            let cv2 = Vector2(textureCoordinate.X, textureCoordinate.Y)

            vertices.Add(                
                createVertex position normal color4 cv2  
            )

        (vertices, indices)

    // ----------------------------------------------------------------------------------------------------
    // Cylinder Cone
    // ----------------------------------------------------------------------------------------------------
    let ComputeCone (origin:Vector3, color:Color, theHeight, radius:float32,tessellation , isTransparent) =    
    
        let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()

        
        let mutable vertices = new List<Vertex>() 
        let indices =  new List<int>()

        let shift = Vector3(radius, 0.0f, radius)
        let center = origin + shift

        let height = theHeight / 2.0f
        let topOffset = Vector3.UnitY * height
        let stride = tessellation + 1

        for i = 0 to tessellation do  
            
            let normal = CreateCircleVector(i, tessellation)
            let sideOffset = normal * radius
            let textureCoordinate = new Vector2((float32) (i/tessellation), 0.0f) 

            vertices.Add(
                createVertex (center + (sideOffset + 2.0f * topOffset))    normal color4 textureCoordinate                      )
            vertices.Add(                
                createVertex (center + sideOffset)                         normal color4 (textureCoordinate + Vector2.UnitY)    )  

            indices.Add(i*2)
            indices.Add((i*2 + 2)%(stride*2))
            indices.Add(i*2 + 1)

            indices.Add(i*2 + 1)
            indices.Add((i*2 + 2)%(stride*2))
            indices.Add((i*2 + 3)%(stride*2))

        (vertices, indices)

    let cylinderVertices (origin, colorCone, colorCap, height, radius, tessellation, withCap, isTransparent) =
        // Cone
        let mutable (verticesC, indicesC) = ComputeCone(origin, colorCone, height, radius, tessellation,  isTransparent)
        reverseWinding &verticesC  &indicesC 
        let cone = MeshData.Create(verticesC, indicesC)
        // upper cap
        let mutable (verticesU, indicesU) = CreateCylinderCap(origin, tessellation, (height) , radius ,colorCap, true , isTransparent)
        reverseWinding &verticesU  &indicesU 
        let capUpper = MeshData.Create(verticesU, indicesU)
        // lower cap
        let mutable (verticesL, indicesL) = CreateCylinderCap(origin ,tessellation, 0.0f, radius, colorCap, false,  isTransparent)
        reverseWinding &verticesL  &indicesL  
        let capLower = MeshData.Create(verticesL, indicesL)
        // Compose
        let caps = MeshData.Compose(capLower, capUpper) 
        let complete = MeshData.Compose(cone, caps)

        if withCap then complete else cone

    let CreateMeshData(origin:Vector3, colorCone:Color, colorCap:Color, height:float32, radius:float32, withCap:bool, raster:int, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        cylinderVertices (origin, colorCone, colorCap, height, radius, raster, withCap, isTransparent ) 