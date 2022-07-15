namespace ShaderPBR
//
//  VertexDefs.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open System.Runtime.InteropServices

open SharpDX 

open Base.PrintSupport

// ----------------------------------------------------------------------------------------------------
// Vertex Typ  in Gltf
// ----------------------------------------------------------------------------------------------------
module VertexDefs =

    [<StructLayout(LayoutKind.Sequential)>]
    type Vertex =
        struct
            val mutable Position: Vector4   // 12 bytes
            val mutable Normal: Vector3     // 12 bytes
            val mutable Texture: Vector2    // 12 bytes

            new(position, normal, texture) =
                { Position = position
                  Normal = normal
                  Texture = texture }

            new(position, normal) =
                { Position = position
                  Normal = normal
                  Texture = Vector2.Zero }

            new(position) = Vertex(position, Vector3.Normalize(Vector3(position.X, position.Y, position.Z )))

            new(px: float32, py: float32, pz: float32, pw: float32, nx: float32, ny: float32, nz: float32, u: float32, v: float32 ) =
                new Vertex(new Vector4(px, py, pz, pw), new Vector3(nx, ny, nz ), new Vector2(u, v))

            override this.ToString() =
                "Vertex P("
                + formatVector3 (Vector3(this.Position.X, this.Position.Y, this.Position.Z ))
                + ")"
                + " N("
                + formatVector3 (this.Normal)
                + ") T("
                + formatVector2 (this.Texture)
                + ")"
        end

    let vertexLength = Utilities.SizeOf<Vertex>()