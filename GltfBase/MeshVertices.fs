namespace GltfBase

//
//  Analyzer.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System.Collections.Generic 

open SharpDX

open VGltf
open VGltf.Types

open Base.Framework

open Base.VertexDefs
open Common

// ----------------------------------------------------------------------------------------------------
// Support für das Deploy auf die GPU
// ---------------------------------------------------------------------------------------------------- 
module MeshVertices = 

    // ----------------------------------------------------------------------------------------------------
    // Analyze Mesh
    // ---------------------------------------------------------------------------------------------------- 
    let CreateMeshData(mesh:Mesh, store:ResourcesStore) =
    
        let primitive = mesh.Primitives[0]

        let gltf = store.Gltf

        let posBuffer  = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["POSITION"])
        let positionen = posBuffer.GetEntity<float32, Vector3> (fromArray3) 
        let ueberAllePositionen  = positionen.GetEnumerable().GetEnumerator()

        let normalBuffer = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["NORMAL"])             
        let normalen = normalBuffer.GetEntity<float32, Vector3> (fromArray3) 
        let ueberAlleNormalen  = normalen.GetEnumerable().GetEnumerator()

        let texCoordBuffer = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["TEXCOORD_0"])
        let alleTexCoord = texCoordBuffer.GetEntity<float32, Vector2> (fromArray2) 
        let ueberAlleTexCoords  = alleTexCoord.GetEnumerable().GetEnumerator()

        // Vertex
        let meshVertices  = new List<Vertex>()
        while ueberAllePositionen.MoveNext() && ueberAlleNormalen.MoveNext() && ueberAlleTexCoords.MoveNext()  do
            let pos  = ueberAllePositionen.Current
            let norm = ueberAlleNormalen.Current
            let tex  = ueberAlleTexCoords.Current
            let vertex = new Vertex(pos, norm, Color4.White, tex)
            meshVertices.Add(vertex)

        // Index
        let indGltf     = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Indices.Value)
        let meshIndices = indGltf.GetPrimitivesAsCasted<int>() 

        let topology    = myTopology(primitive.Mode)
        let material    = gltf.Materials[primitive.Material.Value]
        mesh.Name, meshVertices, meshIndices, topology, material, primitive.Material.Value 