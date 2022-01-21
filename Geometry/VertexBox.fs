namespace Geometry
//
//  VertexBox.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic 

open Base.MeshObjects 
open Base.ModelSupport

open System

open Base.VertexDefs

open SharpDX

// ----------------------------------------------------------------------------------------------------
// Vertexe für einen Quader erzeugen
// Alle Seiten achsenparallel - Drehungen erfolgen über World-Transformationen
// ----------------------------------------------------------------------------------------------------

module VertexBox = 

    // TOTO Color ? Tangent?
    let MidPoint( v0:Vertex, v1:Vertex) = 
        // Compute the midpoints of all the attributes. Vectors need to be normalized
        // since linear interpolating can make them not unit length.
        let pos = 0.5f * (v0.Position + v1.Position);
        let normal  = Vector3.Normalize(0.5f * (v0.Normal + v1.Normal)) 
        //let tangent = Vector3.Normalize(0.5f * (v0.TangentU + v1.TangentU)) 
        let tex = 0.5f * (v0.Texture + v1.Texture);
        new Vertex(pos, normal, Color.White, tex);

    let Subdivide(vertices:Vertex[], indices:int[], maxNumSubdivisions:int) =
    
        let resultVertices = new List<Vertex>()
        let resultIndices = new List<int>()

        //       v1
        //       *
        //      / \
        //     /   \
        //  m0*-----*m1
        //   / \   / \
        //  /   \ /   \
        // *-----*-----*
        // v0    m2     v2

        let numTriangles = indices.Length / 3
        for i in 0..numTriangles-1 do

            let v0 = vertices.[indices.[i * 3 + 0]]
            let v1 = vertices.[indices.[i * 3 + 1]]
            let v2 = vertices.[indices.[i * 3 + 2]]

            //
            // Generate the midpoints.
            //

            let m0 = MidPoint(v0, v1)
            let m1 = MidPoint(v1, v2)
            let m2 = MidPoint(v0, v2)

            //
            // Add new geometry.
            //

            resultVertices.Add(v0) // 0
            resultVertices.Add(v1) // 1
            resultVertices.Add(v2) // 2
            resultVertices.Add(m0) // 3
            resultVertices.Add(m1) // 4
            resultVertices.Add(m2) // 5

            resultIndices.Add(i * 6 + 0)
            resultIndices.Add(i * 6 + 3)
            resultIndices.Add(i * 6 + 5)

            resultIndices.Add(i * 6 + 3)
            resultIndices.Add(i * 6 + 4)
            resultIndices.Add(i * 6 + 5)

            resultIndices.Add(i * 6 + 5)
            resultIndices.Add(i * 6 + 4)
            resultIndices.Add(i * 6 + 2)

            resultIndices.Add(i * 6 + 3)
            resultIndices.Add(i * 6 + 1)
            resultIndices.Add(i * 6 + 4)

        new MeshData<Vertex>(resultVertices.ToArray(), resultIndices.ToArray()) 

    let createVertices(width:float32,  height:float32,  depth:float32, color:Color, isTransparent) =

        let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()

        let mutable result:Vertex[] = Array.create 24  (new Vertex()) 

        //
        // Create the vertices.
        //

        let w2 = 0.5f * width
        let h2 = 0.5f * height
        let d2 = 0.5f * depth

        // Fill in the front face vertex data.
        result.[0]  <- new Vertex(-w2, -h2, -d2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, color4) 
        result.[1]  <- new Vertex(-w2, +h2, -d2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, color4) 
        result.[2]  <- new Vertex(+w2, +h2, -d2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, color4) 
        result.[3]  <- new Vertex(+w2, -h2, -d2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, color4)
        // Fill in the back face vertex data.
        result.[4]  <- new Vertex(-w2, -h2, +d2, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f, color4)
        result.[5]  <- new Vertex(+w2, -h2, +d2, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, color4)
        result.[6]  <- new Vertex(+w2, +h2, +d2, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, color4)
        result.[7]  <- new Vertex(-w2, +h2, +d2, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f, color4)
        // Fill in the top face vertex data.
        result.[8]  <- new Vertex(-w2, +h2, -d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, color4)
        result.[9]  <- new Vertex(-w2, +h2, +d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, color4)
        result.[10] <- new Vertex(+w2, +h2, +d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, color4)
        result.[11] <- new Vertex(+w2, +h2, -d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, color4)
        // Fill in the bottom face vertex data.
        result.[12] <- new Vertex(-w2, -h2, -d2, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f, color4)
        result.[13] <- new Vertex(+w2, -h2, -d2, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, color4)
        result.[14] <- new Vertex(+w2, -h2, +d2, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, color4)
        result.[15] <- new Vertex(-w2, -h2, +d2, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f, color4)
        // Fill in the left face vertex data.
        result.[16] <- new Vertex(-w2, -h2, +d2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f, color4)
        result.[17] <- new Vertex(-w2, +h2, +d2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f, color4)
        result.[18] <- new Vertex(-w2, +h2, -d2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, color4)
        result.[19] <- new Vertex(-w2, -h2, -d2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f, color4)
        // Fill in the right face vertex data.
        result.[20] <- new Vertex(+w2, -h2, -d2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, color4)
        result.[21] <- new Vertex(+w2, +h2, -d2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, color4)
        result.[22] <- new Vertex(+w2, +h2, +d2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f, color4)
        result.[23] <- new Vertex(+w2, -h2, +d2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f, color4)
        result

    let createIndices() =
        [|
            // Fill in the front face index data.
            0; 1; 2; 0; 2; 3;
            // Fill in the back face index data.
            4; 5; 6; 4; 6; 7;
            // Fill in the top face index data.
            8; 9; 10; 8; 10; 11;
            // Fill in the bottom face index data.
            12; 13; 14; 12; 14; 15;
            // Fill in the left face index data
            16; 17; 18; 16; 18; 19;
            // Fill in the right face index data
            20; 21; 22; 20; 22; 23
        |] 

    let CreateMeshData(width:float32,  height:float32, depth:float32, numSubdivisions, color:Color, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        let vertices = createVertices(width, height, depth, color, isTransparent)
        let indices = createIndices()        
        let maxNumSubdivisions = Math.Min(numSubdivisions, 6) // Put a cap on the number of subdivisions.
        //Subdivide(vertices, indices, maxNumSubdivisions)
        new MeshData<Vertex>(vertices , indices)

