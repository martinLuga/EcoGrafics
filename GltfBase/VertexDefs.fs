namespace GltfBase
//
//  VertexDefs.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open System.Runtime.InteropServices

open SharpDX 

open Base.Framework

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
            val mutable Texture:  Vector2       // 12 bytes
            val mutable Skinning: SkinningVertex
            new (position, normal, texture) = {Position=position; Normal=normal; Texture=texture; Skinning=new SkinningVertex(0u, 0.0f) }
            new (position, normal) = {Position=position; Normal=normal; Texture=Vector2.Zero; Skinning=SkinningVertex(0u, 0.0f) }
            new (position) = Vertex(position, Vector3.Normalize(position) ) 
            new (
                px:float32, py:float32, pz:float32,
                nx:float32, ny:float32, nz:float32,
                tx:float32, ty:float32, tz:float32,
                u:float32 , v:float32,color) = new Vertex(
                        new Vector3(px, py, pz),
                        new Vector3(nx, ny, nz), 
                        new Vector2(u, v)
                        )

            override this.ToString() = "Vertex P(" + formatVector(this.Position) + ")" + " N(" + formatVector(this.Normal) + ") T(" + formatVector2(this.Texture) + ")"
        end

    let vertexLength = Utilities.SizeOf<Vertex>()
    let vertexPrint (aVertex:Vertex) = "Vertex:" + aVertex.Position.ToString()     

