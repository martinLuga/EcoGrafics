namespace Gltf2Base
//
//  Builder.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System 
open System.Collections.Generic
open System.IO
open System.Drawing

open SharpDX

open log4net

open Base.VertexDefs

open glTFLoader 
open glTFLoader.Schema 

open Common

// ----------------------------------------------------------------------------------------------------
// GltfBuilder auf Basis Gltf2Loader
// ---------------------------------------------------------------------------------------------------- 
module Builder = 

    // ----------------------------------------------------------------------------------------------------
    // Erzeugen Vertex und Indices
    // ---------------------------------------------------------------------------------------------------- 
    [<AllowNullLiteral>]
    type GltfBuilder(fileName:string) =
        let mutable fileName=fileName        
        let mutable gltf = Interface.LoadModel (fileName) 
        let mutable buffers=gltf.Buffers
        let mutable meshes=gltf.Meshes
        let mutable nodes=gltf.Nodes

        member this.Gltf
            with get() = gltf
        
        member this.Log(logger:ILog) =        
            
            logger.Debug(fileName + " contains " + meshes.Length.ToString()  + " Meshes")  
            logger.Debug(fileName + " contains " + buffers.Length.ToString() + " Buffers")
            logger.Debug(fileName + " contains " + nodes.Length.ToString()   + " Nodes")

        member this.CreateMeshData(nodeName, mesh:Mesh, isTransparent) =
            let primitive       = mesh.Primitives[0]

            // Positions
            let mutable positions = new List<Vector3>()
            let accPos          = primitive.Attributes["POSITION"]  
            let accessor        = gltf.Accessors[accPos]  
            let bufferView      = gltf.BufferViews[accessor.BufferView.Value] 
            let buffer          = buffers.[bufferView.Buffer]             
            let bufferBytes     = gltf.LoadBinaryBuffer(accessor.ByteOffset, fileName)

            for i in bufferView.ByteOffset .. 12 .. bufferView.ByteOffset + bufferView.ByteLength - 1   do 
                let x   = BitConverter.ToSingle (bufferBytes, i)
                let y   = BitConverter.ToSingle (bufferBytes, i + 4)
                let z   = BitConverter.ToSingle (bufferBytes, i + 8)
                let w   = BitConverter.ToSingle (bufferBytes, i + 12)
                let pos = new Vector3(x,y,z)
                positions.Add(pos)

            // Normal
            let mutable normals = new List<Vector3>()
            let accNormal       = primitive.Attributes["NORMAL"] 
            let accessor        = gltf.Accessors[accNormal]  
            let bufferView      = gltf.BufferViews[accessor.BufferView.Value] 
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
                let mutable color4 = if isTransparent then ToTransparentColor(Color4.White) else Color4.White                 
                let vertex = createVertex pos norm color4 tex
                meshVertices.Add(vertex)

            // Index
            let meshIndices     = new List<int>()
            let accIndex        =  primitive.Indices.Value
            let accessor        = gltf.Accessors[accIndex] 
            let bufferView      = gltf.BufferViews[accessor.BufferView.Value] 
            let bufferBytes     = gltf.LoadBinaryBuffer(accessor.ByteOffset, fileName)
            for i in bufferView.ByteOffset .. 2 .. bufferView.ByteOffset + bufferView.ByteLength - 1 do 
                let idx   = BitConverter.ToInt16 (bufferBytes, i)  
                meshIndices.Add(int idx)

            let topology        =  myTopology(primitive)
            nodeName, meshVertices, meshIndices, topology, primitive.Material.Value

        member this.CreateImage(imgIndex, ibuf) = 
            let mutable imageData:byte[] = [||]
            let bufferView      = gltf.BufferViews[ibuf] 

            let bufferStream    = gltf.OpenImageFile(imgIndex, fileName)

            using (new BinaryReader(bufferStream))(fun r ->
                imageData <- r.ReadBytes (int bufferStream.Length)
            )             
            let bufferStream    = gltf.OpenImageFile(bufferView.Buffer, fileName) 
            let bitmap = Image.FromStream(bufferStream , true, false)
            imageData, bitmap