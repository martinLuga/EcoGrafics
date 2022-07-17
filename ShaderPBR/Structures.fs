﻿namespace ShaderPBR
//
//  Structures.fs
//
//  Created by Martin Luga on 10.09.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System
open System.Runtime.InteropServices

open Base.PrintSupport

open SharpDX
open SharpDX.Direct3D12

open glTFLoader.Schema 

// ----------------------------------------------------------------------------------------------------
// Ein Scene stellt eine graphische Ausgangssituation her
// ----------------------------------------------------------------------------------------------------
module Structures =

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

    [<type: StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type ObjectConstantsPBR =
        struct
            val mutable Model: Matrix
            val mutable View: Matrix
            val mutable Projection: Matrix

            new(model, view, projection) =
                { Model = model
                  View = view
                  Projection = projection }
        end

    // Transpose the matrices so that they are in row major order for HLSL
    let Transpose (perObject:ObjectConstantsPBR) =
        perObject.Model.Transpose()
        perObject.View.Transpose()
        perObject.Projection.Transpose()
        perObject

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type DirectionalLight =
        struct
            val mutable Color: Color3  
            val _padding1: float32  
            val mutable Direction: Vector3  
            val _padding2: float32  

            new(color, direction) =
                { Color = color
                  _padding1 = 0.0f 
                  Direction = direction
                  _padding2 = 0.0f }
        end

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type FrameConstants =
        struct
            val mutable Light: DirectionalLight 
            new(light ) = { Light = light }
        end

    // Wrap Filter 
    let DynamicSamplerDesc(sampler:Sampler) =
        new SamplerStateDescription (
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                ComparisonFunction = Comparison.Never,
                Filter = Filter.MaximumAnisotropic,
                MaximumLod = Single.MaxValue,
                MinimumLod = 0.0f,
                MipLodBias = 0.0f
            ) 