namespace Geometry
//
//  VertexBox.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic 

open Base.MeshObjects 

open System

open Base.VertexDefs

// ----------------------------------------------------------------------------------------------------
// Vertexe für einen Quader erzeugen
// Alle Seiten achsenparallel - Drehungen erfolgen über World-Transformationen
// ----------------------------------------------------------------------------------------------------

module VertexBox = 

    let createVertices(width:float32,  height:float32,  depth:float32,  numSubdivisions:int) =

        let mutable result = new List<Vertex>()

        //
        // Create the vertices.
        //

        let w2 = 0.5f * width
        let h2 = 0.5f * height
        let d2 = 0.5f * depth

        // Fill in the front face vertex data.
        result.Add(new Vertex(-w2, -h2, -d2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f))
        result.Add(new Vertex(-w2, +h2, -d2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f))
        result.Add(new Vertex(+w2, +h2, -d2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f))
        result.Add(new Vertex(+w2, -h2, -d2, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f))
        // Fill in the back face vertex data.
        result.Add(new Vertex(-w2, -h2, +d2, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f))
        result.Add(new Vertex(+w2, -h2, +d2, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f))
        result.Add(new Vertex(+w2, +h2, +d2, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f))
        result.Add(new Vertex(-w2, +h2, +d2, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f))
        // Fill in the top face vertex data.
        result.Add(new Vertex(-w2, +h2, -d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f))
        result.Add(new Vertex(-w2, +h2, +d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f))
        result.Add(new Vertex(+w2, +h2, +d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f))
        result.Add(new Vertex(+w2, +h2, -d2, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f))
        // Fill in the bottom face vertex data.
        result.Add(new Vertex(-w2, -h2, -d2, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f))
        result.Add(new Vertex(+w2, -h2, -d2, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f))
        result.Add(new Vertex(+w2, -h2, +d2, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f))
        result.Add(new Vertex(-w2, -h2, +d2, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f))
        // Fill in the left face vertex data.
        result.Add(new Vertex(-w2, -h2, +d2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f))
        result.Add(new Vertex(-w2, +h2, +d2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f))
        result.Add(new Vertex(-w2, +h2, -d2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f))
        result.Add(new Vertex(-w2, -h2, -d2, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f))
        // Fill in the right face vertex data.
        result.Add(new Vertex(+w2, -h2, -d2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f))
        result.Add(new Vertex(+w2, +h2, -d2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f))
        result.Add(new Vertex(+w2, +h2, +d2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f))
        result.Add(new Vertex(+w2, -h2, +d2, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f))
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

    let CreateMeshData(width:float32,  height:float32, depth:float32, numSubdivisions) =
        let meshData = MeshData.Create(createVertices(width,  height, depth, numSubdivisions), createIndices()) 
        // Put a cap on the number of subdivisions.
        let maxNumSubdivisions = Math.Min(numSubdivisions, 6);
        meshData.Subdivide(numSubdivisions)
        meshData

