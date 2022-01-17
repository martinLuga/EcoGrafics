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

    let Subdivide(vertices:List<Vertex>, indices:List<int>, maxNumSubdivisions:int) =
    
        // Save a copy of the input geometry.
        let verticesCopy = vertices.ToArray() 
        let indicesCopy  = indices.ToArray()

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

        let numTriangles = indices.Count / 3
        for i in 0..numTriangles-1 do

            let v0 = verticesCopy.[indicesCopy.[i * 3 + 0]]
            let v1 = verticesCopy.[indicesCopy.[i * 3 + 1]]
            let v2 = verticesCopy.[indicesCopy.[i * 3 + 2]]

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

        MeshData.Create(resultVertices, resultIndices) 

    let createVertices(width:float32,  height:float32,  depth:float32, color:Color, isTransparent) =

        let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()

        let mutable result = new List<Vertex>()

        //
        // Create the vertices.
        //

        let w2 = 0.5f * width
        let h2 = 0.5f * height
        let d2 = 0.5f * depth

        // Fill in the front face vertex data.
        result.Add(new Vertex(-w2, -h2, -d2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, color4))
        result.Add(new Vertex(-w2, +h2, -d2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, color4))
        result.Add(new Vertex(+w2, +h2, -d2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, color4))
        result.Add(new Vertex(+w2, -h2, -d2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, color4))
        // Fill in the back face vertex data.
        result.Add(new Vertex(-w2, -h2, +d2, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f, color4))
        result.Add(new Vertex(+w2, -h2, +d2, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, color4))
        result.Add(new Vertex(+w2, +h2, +d2, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, color4))
        result.Add(new Vertex(-w2, +h2, +d2, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f, color4))
        // Fill in the top face vertex data.
        result.Add(new Vertex(-w2, +h2, -d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, color4))
        result.Add(new Vertex(-w2, +h2, +d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, color4))
        result.Add(new Vertex(+w2, +h2, +d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, color4))
        result.Add(new Vertex(+w2, +h2, -d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, color4))
        // Fill in the bottom face vertex data.
        result.Add(new Vertex(-w2, -h2, -d2, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f, color4))
        result.Add(new Vertex(+w2, -h2, -d2, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, color4))
        result.Add(new Vertex(+w2, -h2, +d2, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, color4))
        result.Add(new Vertex(-w2, -h2, +d2, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f, color4))
        // Fill in the left face vertex data.
        result.Add(new Vertex(-w2, -h2, +d2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f, color4))
        result.Add(new Vertex(-w2, +h2, +d2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f, color4))
        result.Add(new Vertex(-w2, +h2, -d2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, color4))
        result.Add(new Vertex(-w2, -h2, -d2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f, color4))
        // Fill in the right face vertex data.
        result.Add(new Vertex(+w2, -h2, -d2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, color4))
        result.Add(new Vertex(+w2, +h2, -d2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, color4))
        result.Add(new Vertex(+w2, +h2, +d2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f, color4))
        result.Add(new Vertex(+w2, -h2, +d2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f, color4))
        result

    let createIndices() =
        let mutable result = new List<int>()
        result.AddRange ( 
            [
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
            ] 
        )
        result

    let CreateMeshData(width:float32,  height:float32, depth:float32, numSubdivisions, color:Color, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        let vertices = createVertices(width,  height, depth, color, isTransparent)
        let indices = createIndices()        
        let maxNumSubdivisions = Math.Min(numSubdivisions, 6) // Put a cap on the number of subdivisions.
        Subdivide(vertices, indices, maxNumSubdivisions)

