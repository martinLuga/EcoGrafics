namespace GltfBase

//
//  MeshBuild.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System.Collections.Generic 

open SharpDX

open VGltf
open VGltf.Types

open Base.PrintSupport
open Base.VertexDefs

open Common

// ----------------------------------------------------------------------------------------------------
// Mesh Build 2
// EcoGrafics Technologie
// ---------------------------------------------------------------------------------------------------- 
module MeshBuild2 = 

    // ----------------------------------------------------------------------------------------------------
    // Erzeugen Vertex und Indices
    // ---------------------------------------------------------------------------------------------------- 
    let CreateMeshData (mesh:Mesh, store:ResourcesStore) =
    
        let primitive = mesh.Primitives[0]

        let gltf = store.Gltf

        let posBuffer  = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["POSITION"])
        let positionen = posBuffer.GetEntity<float32, Vector4> (Mapper<float32, Vector4>(toArray4AndIntFromFloat32)) 
        let ueberAllePositionen  = positionen.AsArray().GetEnumerator()

        let normalBuffer = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["NORMAL"])             
        let normalen = normalBuffer.GetEntity<float32, Vector3> (Mapper<float32, Vector3>(toArray3AndIntFromFloat32))
        let ueberAlleNormalen  = normalen.AsArray().GetEnumerator()

        let texCoordBuffer = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["TEXCOORD_0"])
        let alleTexCoord = texCoordBuffer.GetEntity<float32, Vector2>  (Mapper<float32, Vector2>(toArray2AndIntFromFloat32))
        let ueberAlleTexCoords  = alleTexCoord.AsArray().GetEnumerator()

        // Vertex
        let meshVertices = new List<Vertex>()

        while ueberAllePositionen.MoveNext()
              && ueberAlleNormalen.MoveNext()
              && ueberAlleTexCoords.MoveNext() do
            let pos =  ueberAllePositionen.Current :?> Vector4 
            let norm = ueberAlleNormalen.Current :?> Vector3 
            let tex = ueberAlleTexCoords.Current :?> Vector2 
            let vertex = new Vertex(Vector3(pos.X, pos.Y, pos.Z), norm, tex)
            meshVertices.Add(vertex)

        // Index
        let indGltf     = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Indices.Value)
        let meshIndices = indGltf.GetPrimitivesAsInt () 

        let topology    = myTopology(primitive.Mode)
        mesh.Name, meshVertices.ToArray(), meshIndices|>Seq.toArray, topology, primitive.Material.Value 

