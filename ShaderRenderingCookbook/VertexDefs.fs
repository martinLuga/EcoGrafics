namespace ShaderRenderingCookbook
//
//  VertexDefs.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open System.Collections.Generic
open System.Runtime.InteropServices

open SharpDX 

open Base.Framework
open Base.PrintSupport

// ----------------------------------------------------------------------------------------------------
// Vertex Typ  
// ----------------------------------------------------------------------------------------------------
module VertexDefs =

    let ToTransparentColor(color:Color4) = 
        let mutable color4 = Color.Transparent.ToColor4()
        color4.Alpha <- 0.5f
        color4

    [<type:StructLayout(LayoutKind.Sequential, Pack = 1)>]
    type SkinningVertex =
        struct
            val  BoneIndex0:uint32
            val  BoneIndex1:uint32
            val  BoneIndex2:uint32
            val  BoneIndex3:uint32
            val  BoneWeight0:float32
            val  BoneWeight1:float32
            val  BoneWeight2:float32
            val  BoneWeight3:float32
            new (defaultint, defaultfloat) =
                {BoneIndex0=defaultint; BoneIndex1=defaultint; BoneIndex2=defaultint; BoneIndex3=defaultint;
                 BoneWeight0=defaultfloat;BoneWeight1=defaultfloat;BoneWeight2=defaultfloat;BoneWeight3=defaultfloat}
    end

    [<StructLayout(LayoutKind.Sequential)>]
    type Vertex =
        struct 
            val mutable Position: Vector3       // 12 bytes
            val mutable Normal:   Vector3       // 12 bytes 
            val mutable Color:    Color4        // 16 bytes 
            val mutable Texture:  Vector2       // 12 bytes
            val mutable Skinning: SkinningVertex
            new (position, normal, color, texture) = {Position=position; Normal=normal; Color=color; Texture=texture; Skinning=new SkinningVertex(0u, 0.0f) }            
            new (position, normal, texture) = Vertex (position, normal, Color4.White, texture)
            new (position, normal, color) = Vertex (position, normal, color, Vector2.Zero)
            new (position, color:Color4) = Vertex(position, Vector3.Normalize(position), color)
            new (position) = Vertex(position, Color4.White) 
            new (
                px:float32, py:float32, pz:float32,
                nx:float32, ny:float32, nz:float32,
                tx:float32, ty:float32, tz:float32,
                u:float32 , v:float32,color) = new Vertex(
                        new Vector3(px, py, pz),
                        new Vector3(nx, ny, nz),
                        color,
                        new Vector2(u, v)
                        )

            override this.ToString() = "Vertex P(" + formatVector3(this.Position) + ")" + " N(" + formatVector3(this.Normal) + ") T(" + formatVector2(this.Texture) + ")"
        end

    let vertexLength = Utilities.SizeOf<Vertex>()
    let vertexPrint (aVertex:Vertex) = "Vertex:" + aVertex.Position.ToString()     

    let createVertex pos normal colr4 uv   = 
       new Vertex(pos, normal, colr4, uv)

    let maxVertex (vec1:Vertex) (vec2:Vertex) =
        new Vertex(
            Vector3.Max(vec1.Position, vec2.Position)
        )
            
    let minVertex(vert1:Vertex) (vert2:Vertex) =
        new Vertex(
            Vector3.Min(vert1.Position, vert2.Position)
        )

    let shiftVertex(vert:Vertex, shift:Vector3) =
        new Vertex(
            vert.Position + shift,
            vert.Normal,
            vert.Color,
            vert.Texture
        )

    let shiftAllVertex(points:Vertex list, shift:Vector3) =
        points |> List.map (fun v -> shiftVertex(v, shift))

    let computeMinimum (points: Vertex list) =
        if points.Length = 0 then new Vertex()
        else 
            let min = points |> List.reduce minVertex  
            min

    let maxVecInY (point1:Vertex) (point2:Vertex) =
        if point1.Position.Y > point2.Position.Y then point1 else point2

    let minVecInY (point1:Vertex) (point2:Vertex) =
        if point1.Position.Y < point2.Position.Y then point1 else point2

    let computeMaximumInY (points: Vertex list) =
        if points.Length = 0 then new Vertex()
        else 
            let max = points |> List.reduce maxVecInY  
            max

    let computeMinimumInY (points: Vertex list) =
        if points.Length = 0 then new Vertex()
        else 
            let min = points |> List.reduce minVecInY  
            min

    let computeMaximum (points: Vertex list) =
        if points.Length = 0 then new Vertex()
        else 
            let max = points |> List.reduce maxVertex
            max

    let computeBoundingBox(points: Vertex list) =
        BoundingBox((computeMinimum (points)).Position, (computeMaximum (points)).Position)        

    let sortInXZ(points: Vertex list) =
        if points.Length = 0 then []
        else 
            points |> List.sortBy (fun v -> v.Position.X, v.Position.Z)

    //-----------------------------------------------------------------------------------------------------
    // Lokale Extrema
    //-----------------------------------------------------------------------------------------------------  

    // Liegt ein Vertex in dem Intervall
    let inInterval(vertex:Vertex, interval:Interval) = 
        match interval.Dim with
        | XZ -> 
            inInterval1(vertex.Position.X, interval) &&
            inInterval2(vertex.Position.Z, interval)
        | XY -> 
            inInterval1(vertex.Position.X, interval) &&
            inInterval2(vertex.Position.Y, interval)
        | YZ -> 
            inInterval1(vertex.Position.Y, interval) &&
            inInterval2(vertex.Position.Z, interval)

    // Alle vertices in dem Intervall
    let selectInterval(vertices, interval)=
        vertices
        |> List.filter(fun v -> inInterval(v, interval))

    // Alle vertices in dem Intervall
    let partition(vertices:Vertex list, dim:Dimensions, partFactor:int, logDebug) =
        let bounds = computeBoundingBox (vertices)
        let mutable result = new List<Vertex>()
        let mutable x = bounds.Minimum.X
        let mutable z = bounds.Minimum.Z
        let deltax = (bounds.Maximum.X - bounds.Minimum.X) / (float32)partFactor
        let deltaz = (bounds.Maximum.Z - bounds.Minimum.Z) / (float32)partFactor
        logDebug ("Partitioning " + vertices.Length.ToString() + " elements in " + dim.ToString() + " dividing " + partFactor.ToString() + " times --- ")
        logDebug ("Delta x --- "  + deltax.ToString())
        logDebug ("Delta z --- "  + deltaz.ToString())
        while x < bounds.Maximum.X do
            logDebug ("Next x --- " + x.ToString())
            z <- bounds.Minimum.Z
            while z < bounds.Maximum.Z do
                logDebug ("Next z --- " + z.ToString())
                let interval = Interval(Dimensions.XZ, x, x + deltax, z, z + deltaz)
                let selected = selectInterval(vertices |> Seq.toList, interval)   
                if selected.Length > 0 then
                    let mutable min = computeMinimumInY (selected)
                    let mutable max = computeMaximumInY (selected)
                    let mi1 = Vector3(x,min.Position.Y,z)
                    let ma1 = Vector3(x,max.Position.Y,z)


                    min.Position <- mi1
                    max.Position <- ma1

                    result.Add(min)
                    result.Add(max)
                    logDebug (">>>>" + printInterval(interval) + " yields")
                    logDebug (" minimum " + min.ToString())
                    logDebug (" maximum " + max.ToString())
                z <- z + deltaz                
            x <- x + deltax
        result |> Seq.toList

