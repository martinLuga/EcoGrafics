namespace GltfBase
//
//  GltfSupport.fs
//
//  Created by Martin Luga on 10.09.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open VGltf.Types

open Base.Framework

open SharpDX.Direct3D12
open SharpDX

open System.Runtime.InteropServices
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
                + formatVector (Vector3(this.Position.X, this.Position.Y, this.Position.Z ))
                + ")"
                + " N("
                + formatVector (this.Normal)
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
            val mutable Color: Color3 // 16 bytes
            val _padding1: float32  
            val mutable Direction: Vector3 // 12 bytes
            val _padding2: float32  

            new(color, direction) =
                { Color = color
                  _padding1 = 0.0f 
                  Direction = direction
                  _padding2 = 0.0f }

            new(color) = DirectionalLight(color, Vector3.Zero)
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
            val mutable emissiveFactor: float32[]
            val mutable occlusionStrength: float32
            val mutable metallicRoughnessValues: float32[] 
            val mutable padding1: float32
            val mutable baseColorFactor: float32[]
            val mutable camera: Vector3
            val mutable padding2: float32
            new(material: MyMaterial) =
                {
                    normalScale=1.0f
                    emissiveFactor=material.Material.EmissiveFactor
                    occlusionStrength=20.0f
                    metallicRoughnessValues= material.MetallicRoughnessValues
                    padding1=0.0f
                    baseColorFactor=material.BaseColourFactor
                    camera=Vector3.One
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
                Filter = Filter.MinMagMipLinear,
                MaximumLod = Single.MaxValue,
                MinimumLod = 0.0f,
                MipLodBias = 0.0f
            ) 

    type Roughness() =
        let mutable BaseColorFactor: float32[] = [|0.0f; 0.0f|]        
        let mutable BaseColorTexture: Material.BaseColorTextureInfoType = null       
        let mutable MetallicFactor: float32  = 0.0f       
        let mutable MetallicRoughnessTexture: Material.MetallicRoughnessTextureInfoType  = null          
        let mutable RoughnessFactor: float32 = 0.0f
    
        member this.Gltf =
            let r = new Material.PbrMetallicRoughnessType()
            r.BaseColorFactor           <- BaseColorFactor       
            r.BaseColorTexture          <- BaseColorTexture      
            r.MetallicFactor            <- MetallicFactor       
            r.MetallicRoughnessTexture  <- MetallicRoughnessTexture      
            r.RoughnessFactor           <- RoughnessFactor
    
    type MaterialGltf() =
        let mutable AlphaCutoff:float32 = 0.0f
        let mutable AlphaMode = Material.AlphaModeEnum.Opaque
        let mutable DoubleSided = false
        let mutable EmissiveFactor: float32[]  = [|0.0f; 0.0f|] 
        let mutable EmissiveTexture:Material.EmissiveTextureInfoType = null 
        let mutable NormalTexture:Material.NormalTextureInfoType = null 
        let mutable OcclusionTexture:Material.OcclusionTextureInfoType = null  
        let mutable PbrMetallicRoughness:Material.PbrMetallicRoughnessType = null 
    
        member this.Gltf =
            let mat = new Material()
            mat.AlphaCutoff             <- 0.0f 
            mat.AlphaMode               <- Material.AlphaModeEnum.Opaque 
            mat.DoubleSided             <- false 
            mat.EmissiveFactor          <- [|0.0f; 0.0f|] 
            mat.EmissiveTexture         <- new Material.EmissiveTextureInfoType() 
            mat.NormalTexture           <- new Material.NormalTextureInfoType()
            mat.OcclusionTexture        <- new Material.OcclusionTextureInfoType() 
            mat.PbrMetallicRoughness    <- new Material.PbrMetallicRoughnessType()