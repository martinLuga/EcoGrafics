namespace Gltf2Base

//
//  MeshBuilder.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System 
open System.Collections.Generic

open SharpDX

open log4net

open glTFLoader 
open glTFLoader.Schema 

open Common
open Structures

// ----------------------------------------------------------------------------------------------------
// MeshBuilder auf Basis Gltf2Loader
// ---------------------------------------------------------------------------------------------------- 
module MeshBuilder = 

    // ----------------------------------------------------------------------------------------------------
    // Erzeugen Vertex und Indices
    // ---------------------------------------------------------------------------------------------------- 
    [<AllowNullLiteral>]
    type MeshBuilder(fileName:string) =
        let mutable fileName=fileName        
        let mutable gltf = Interface.LoadModel (fileName) 
        let mutable buffers=gltf.Buffers
        let mutable meshes=gltf.Meshes
        let mutable nodes=gltf.Nodes
        
        member this.Log(logger:ILog) =        
            
            logger.Debug(fileName + " contains " + meshes.Length.ToString()  + " Meshes")  
            logger.Debug(fileName + " contains " + buffers.Length.ToString() + " Buffers")
            logger.Debug(fileName + " contains " + nodes.Length.ToString()   + " Nodes")

        member this.CreateMeshData(mesh:Mesh) =
            let primitive       = mesh.Primitives[0]

            // Positions
            let mutable positions = new List<Vector4>()
            let accPos          = primitive.Attributes["POSITION"]  
            let accessor        = gltf.Accessors[accPos]  
            let bufferView      = gltf.BufferViews[accessor.BufferView.Value] 
            let buffer          = buffers.[bufferView.Buffer]             
            let bufferBytes     = gltf.LoadBinaryBuffer(accessor.ByteOffset, fileName)

            for i in bufferView.ByteOffset .. 16 .. bufferView.ByteOffset + bufferView.ByteLength   do 
                let x   = BitConverter.ToSingle (bufferBytes, i)
                let y   = BitConverter.ToSingle (bufferBytes, i + 4)
                let z   = BitConverter.ToSingle (bufferBytes, i + 8)
                let w   = BitConverter.ToSingle (bufferBytes, i + 8)
                let pos = new Vector4(x,y,z, w)
                positions.Add(pos)

            // Normal
            let mutable normals = new List<Vector3>()
            let accNormal       = primitive.Attributes["NORMAL"] 
            let accessor        = gltf.Accessors[accNormal]  
            let bufferView      = gltf.BufferViews[accessor.BufferView.Value] 
            let buffer          = buffers.[bufferView.Buffer]             
            let bufferBytes     = gltf.LoadBinaryBuffer(accessor.ByteOffset, fileName)

            for i in bufferView.ByteOffset .. 12 .. bufferView.ByteOffset + bufferView.ByteLength - 1    do 
                let x   = BitConverter.ToSingle (bufferBytes, i)
                let y   = BitConverter.ToSingle (bufferBytes, i + 4)
                let z   = BitConverter.ToSingle (bufferBytes, i + 8)
                let norm = new Vector3(x,y,z)
                normals.Add(norm)
            
            // Texture
            let mutable textures    = new List<Vector2>()
            let accTexture          = primitive.Attributes["TEXCOORD_0"]
            let accessor            = gltf.Accessors[accTexture] 
            let bufferView          = gltf.BufferViews[accessor.BufferView.Value] 
            let buffer              = buffers.[bufferView.Buffer]             
            let bufferBytes         = gltf.LoadBinaryBuffer(accessor.ByteOffset, fileName)

            for i in bufferView.ByteOffset .. 8 .. bufferView.ByteOffset + bufferView.ByteLength - 1   do 
                let x   = BitConverter.ToSingle (bufferBytes, i)
                let y   = BitConverter.ToSingle (bufferBytes, i + 4)
                let texture = new Vector2(x,y)
                textures.Add(texture)

            // Vertex
            let meshVertices = new List<Vertex>()
            for i in 0 .. positions.Count-1 do 
                let pos =  positions.Item(i) 
                let norm = normals.Item(i) 
                let tex = textures.Item(i) 
                let vertex = new Vertex(pos, norm, tex)
                meshVertices.Add(vertex)

            // Index
            let meshIndices     = new List<int>()
            let accIndex        =  primitive.Indices.Value
            let accessor        = gltf.Accessors[accIndex] 
            let bufferView      = gltf.BufferViews[accessor.BufferView.Value] 
            let buffer          = buffers.[bufferView.Buffer]             
            let bufferBytes     = gltf.LoadBinaryBuffer(accessor.ByteOffset, fileName)
            for i in bufferView.ByteOffset .. 4 .. bufferView.ByteOffset + bufferView.ByteLength - 1 do 
                let idx   = BitConverter.ToInt32 (bufferBytes, i)  
                meshIndices.Add(idx)

            let topology        =  myTopology(primitive)
            mesh.Name, meshVertices, meshIndices, topology, primitive.Material.Value