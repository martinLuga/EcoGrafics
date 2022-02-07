namespace GPUModel
//
//  MyMesh.fs
//
//  Ported from Luna, Frank D. Introduction To 3D Game Programming With Direct X 12 
//  

open System.Collections.Generic
open System.Runtime.InteropServices 

open SharpDX 
open SharpDX.Direct3D12
open SharpDX.DXGI 

open DirectX.D3DUtilities

open Base.MeshObjects
open Base.VertexDefs

// ----------------------------------------------------------------------------------------------------
// Mesh GPU Support
// Mehrere SubMeshes in einem Mesh in der GPU verwalten
// ----------------------------------------------------------------------------------------------------
module MyMesh = 

    // ----------------------------------------------------------------------------------------------------
    // Ein Submesh
    // ----------------------------------------------------------------------------------------------------
    type SubmeshGeometry() =
        let mutable indexCount = 0
        let mutable startIndexLocation = 0
        let mutable baseVertexLocation = 0

        member this.IndexCount
            with get() = indexCount
            and set(value) = indexCount <- value

        member this.StartIndexLocation
            with get() = startIndexLocation
            and set(value) = startIndexLocation <- value
        
        member this.BaseVertexLocation
            with get()= baseVertexLocation
            and set(value) = baseVertexLocation <- value

    // ----------------------------------------------------------------------------------------------------
    // Mesh 
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type MeshGeometry<'TVertex, 'TIndex 
        when 'TVertex: struct and 'TVertex: (new:Unit -> 'TVertex) and 'TVertex:>System.ValueType
        and  'TIndex : struct and 'TIndex:  (new:Unit -> 'TIndex)  and 'TIndex:>System.ValueType>
        (
            name,
            vertexByteStride , 
            vertexBufferByteSize ,
            vertexBufferGPU:Resource ,
            vertexBufferCPU:'TVertex[] , 
            indexCount , 
            indexFormat , 
            indexBufferByteSize , 
            indexBufferGPU:Resource ,
            indexBufferCPU:'TIndex[]  
        ) =
        let mutable name = name
        let mutable indexBufferGPU:Resource = indexBufferGPU
        let mutable vertexBufferGPU:Resource = vertexBufferGPU
        let mutable vertexBufferCPU:'TVertex[] = vertexBufferCPU
        let mutable indexBufferCPU:'TIndex[] = indexBufferCPU
        let mutable vertexByteStride = vertexByteStride
        let mutable vertexBufferByteSize = vertexBufferByteSize
        let mutable indexFormat:Format = SharpDX.DXGI.Format.Y410
        let mutable indexBufferByteSize = indexBufferByteSize
        let mutable indexCount = 0

        new (name, indexCount, indexFormat, indexBufferByteSize, indexBufferGPU, indexBufferCPU:'TIndex[]) = 
            MeshGeometry(          
                name = name,
                vertexByteStride = 0, vertexBufferByteSize=0,vertexBufferGPU=null, vertexBufferCPU=null,
                indexCount = indexCount,
                indexFormat = indexFormat,
                indexBufferByteSize = indexBufferByteSize,
                indexBufferGPU = indexBufferGPU,
                indexBufferCPU = indexBufferCPU
                )
    
        // Give it a name so we can look it up by name.
        member this. Name 
            with get()= name
            and set(value) = name <- value

        member this.VertexBufferGPU
            with get()= vertexBufferGPU
            and set(value) = vertexBufferGPU <- value
        
        member this.IndexBufferGPU
            with get()= indexBufferGPU
            and set(value) = indexBufferGPU  <- value

        member this.VertexBufferCPU
            with get()= vertexBufferCPU
            and set(value) = vertexBufferCPU <- value

        member this.IndexBufferCPU 
            with get()= indexBufferCPU
            and set(value) = indexBufferCPU <- value

        // Data about the buffers.
        member this.VertexByteStride
            with get() = vertexByteStride
            and set(value) = vertexByteStride <- value

        member this.VertexBufferByteSize
            with get() = vertexBufferByteSize
            and set(value) = vertexBufferByteSize <- value

        member this.IndexFormat
            with get() = indexFormat
            and set(value) = indexFormat <- value

        member this.IndexBufferByteSize
            with get() = indexBufferByteSize
            and set(value) =  indexBufferByteSize <- value

        member this.IndexCount
            with get() = indexCount
            and set(value) = indexCount <- value

        // A MeshGeometry may store multiple geometries in one vertex/index buffer.
        // Use this container to define the Submesh geometries so we can draw
        // the Submeshes individually.
        member this.DrawArgs
            with get() = new Dictionary<string, SubmeshGeometry>() 

        member this.VertexBufferView =
            new VertexBufferView(
                BufferLocation = vertexBufferGPU.GPUVirtualAddress  ,
                StrideInBytes = this.VertexByteStride,
                SizeInBytes = this.VertexBufferByteSize
            ) 

        member this.IndexBufferView = 
            new IndexBufferView( 
                BufferLocation = indexBufferGPU.GPUVirtualAddress,
                Format = this.IndexFormat,
                SizeInBytes = this.IndexBufferByteSize
            )       

        // ----------------------------------------------------------------------------------------------------
        // Below are helper factory methods in order to make use of generic type inference.
        // Note that constructors do not support such inference.        
        // ----------------------------------------------------------------------------------------------------
        static member 
            New<
                'TVertex, 'TIndex when 'TVertex: struct and 'TVertex: (new:Unit -> 'TVertex) and 'TVertex:>System.ValueType
                and 'TIndex:>System.ValueType 
            > 
            ( 
                device:Device,
                commandList:GraphicsCommandList,
                vertices:IEnumerable<'TVertex> ,
                indices:IEnumerable<'TIndex>,
                [<DefaultParameterValue("Default")>]name:string
            ) =         
           
            let mutable indexBufferUploader:Resource = null
            let mutable vertexBufferUploader:Resource = null

            let vertexArray:'TVertex[] = (vertices|> Seq.toArray)
            let vertexBufferByteSize = Utilities.SizeOf(vertexArray)            
            let vertexBuffer = D3DUtil.CreateDefaultBuffer(
                device,
                commandList,
                vertexArray,
                vertexBufferByteSize,
                &vertexBufferUploader
            )

            let indexArray:'TIndex[] = (indices|> Seq.toArray)
            let indexBufferByteSize = Utilities.SizeOf(indexArray)
            let indexBufferLength =  (indices|> Seq.toArray).Length            
            let indexBuffer = D3DUtil.CreateDefaultBuffer(
                device,
                commandList,
                indexArray,
                indexBufferByteSize,
                &indexBufferUploader
            ) 

            new MeshGeometry<'TVertex, 'TIndex>(            
                name = name,
                vertexByteStride = Utilities.SizeOf<'TVertex>(),
                vertexBufferByteSize = vertexBufferByteSize,
                vertexBufferGPU = vertexBuffer,
                vertexBufferCPU = (vertices |> Seq.toArray),
                indexCount = indexBufferLength,
                indexFormat = MeshGeometry.GetIndexFormat<'TIndex>(),
                indexBufferByteSize = indexBufferByteSize,
                indexBufferGPU = indexBuffer,
                indexBufferCPU = (indices |> Seq.toArray)
            )
                                
        // ----------------------------------------------------------------------------------------------------
        // Constructor - Index.        
        // ----------------------------------------------------------------------------------------------------
        static member NewIndex<
            'TVertex, 'TIndex when 'TVertex: struct and 'TVertex: (new:Unit -> 'TVertex) and 'TVertex:>System.ValueType
            and 'TIndex:>System.ValueType 
            > 
            (
                device:Device,
                commandList:GraphicsCommandList,
                indices:IEnumerable<'TIndex>,
                [<DefaultParameterValue("Default")>]name:string
            ) =            
        
            let indexArray:'TIndex[] = (indices|> Seq.toArray)
            let indexBufferByteSize = Utilities.SizeOf(indexArray)
            let mutable indexBufferUploader:Resource = null

            let indexBuffer = D3DUtil.CreateDefaultBuffer(
                device,
                commandList,
                indices |> Seq.toArray,
                indexBufferByteSize, 
                &indexBufferUploader)

            new MeshGeometry<'TVertex, 'TIndex>(            
                name = name,
                indexCount = indexArray.Length,
                indexFormat = MeshGeometry.GetIndexFormat<'TIndex>(),
                indexBufferByteSize = indexBufferByteSize,
                indexBufferGPU = indexBuffer,
                indexBufferCPU = indexArray 
            )

        static member GetIndexFormat<'TIndex> ():Format =
            let mutable format = Format.Unknown 
            if (typedefof<'TIndex> = typedefof<int>) then
                format <- Format.R32_UInt 
            else if (typedefof<'TIndex> = typedefof<int16>) then
                format <- Format.R16_UInt 
            assert(format <> Format.Unknown)
            format

    let AppendMeshData(meshData:MeshData<Vertex>, vertices:List<Vertex>,  indices:List<int>) =
    
        //
        // Define the SubmeshGeometry that cover different
        // regions of the vertex/index buffers.
        //

        let submesh = 
            new SubmeshGeometry (        
                IndexCount = meshData.Indices.Count,
                StartIndexLocation = indices.Count,
                BaseVertexLocation = vertices.Count
            )

        //
        // Extract the vertex elements we are interested in and pack the
        // vertices and indices of all the meshes into one vertex/index buffer.
        //

        vertices.AddRange(meshData.Vertices)
        indices.AddRange(meshData.Indices) 

        submesh 