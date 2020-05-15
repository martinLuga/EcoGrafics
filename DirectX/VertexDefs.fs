namespace DirectX
//
//  VertexDefs.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX 
open System.Runtime.InteropServices

// ----------------------------------------------------------------------------------------------------
// Vertex Typ  
// ----------------------------------------------------------------------------------------------------
module VertexDefs =

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
            val Position: Vector3       // 12 bytes
            val Normal:   Vector3       // 12 bytes 
            val Color:    Color4        // 16 bytes 
            val Texture:  Vector2       // 12 bytes
            val Skinning: SkinningVertex
            new (position, normal, color, texture) = {Position=position; Normal=normal; Color=color; Texture=texture; Skinning=new SkinningVertex(0u, 0.0f) }
            new (position, normal, color) = {Position=position; Normal=normal; Color=color; Texture=Vector2.Zero; Skinning=SkinningVertex(0u, 0.0f) }
            new (position, color) = Vertex(position, Vector3.Normalize(position), color)
            new (position) = Vertex(position, Color4.White)
            override this.ToString() = "Vertex(" + this.Position.ToString() + ")"
        end

    let vertexLength = Utilities.SizeOf<Vertex>()
    let vertexPrint (aVertex:Vertex) = "Vertex:" + aVertex.Position.ToString() 

    let meshCompose (vertices1:Vertex[]) (indices1:int[]) (vertices2:Vertex[]) (indices2:int[]) topology =
        let vertices = Array.append vertices1 vertices2  
        let indices2 = indices2 |> Array.map (fun i -> i + vertices1.Length)
        let indices = Array.append indices1 indices2  
        (vertices, indices, topology)

    let mCompose (tup1:Vertex[] * int[] * 'a) (tup2:Vertex[] * int[] * 'a)  =
        let (vertices1 ,indices1, topology) = tup1
        let (vertices2 ,indices2, topology) = tup2
        meshCompose  vertices1  indices1   vertices2   indices2  topology