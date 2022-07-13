namespace GltfBase
//
//  GltfSupport.fs
//
//  Created by Martin Luga on 10.09.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open VGltf.Types

open Base.PrintSupport

open SharpDX.Direct3D12
open SharpDX

open VGltf

open System.Runtime.InteropServices
open System.Collections.Generic 
open System

open Common

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

    [<type: StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type MaterialConstantsPBR =
        struct
            val mutable normalScale: float32
            val mutable emissiveFactor: Vector3
            val mutable occlusionStrength: float32
            val mutable metallicRoughnessValues: Vector2 
            val mutable padding1: float32
            val mutable baseColorFactor: Color4
            val mutable camera: Vector3
            val mutable padding2: float32
            new(material:MyMaterial, _camera:Vector3) =
                {
                    normalScale=1.0f
                    emissiveFactor=Vector3(material.Material.EmissiveFactor)
                    occlusionStrength=1.0f
                    metallicRoughnessValues=Vector2(material.MetallicRoughnessValues) 
                    padding1=0.0f
                    baseColorFactor=Color4(material.BaseColourFactor)
                    camera=_camera
                    padding2=0.0f
                }
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

    let CreateMeshData(mesh:Mesh, store:ResourcesStore) =
    
        let primitive = mesh.Primitives[0]

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
            let vertex = new Vertex(pos, norm, tex)
            meshVertices.Add(vertex)

        // Index
        let indGltf     = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Indices.Value)
        let meshIndices = indGltf.GetPrimitivesAsInt() 

        let topology    = myTopology(primitive.Mode)
        mesh.Name, meshVertices, meshIndices, topology, primitive.Material.Value
