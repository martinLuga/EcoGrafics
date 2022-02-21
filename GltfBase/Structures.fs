namespace GltfBase
//
//  GltfSupport.fs
//
//  Created by Martin Luga on 10.09.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open VGltf.Types

open SharpDX.DXGI
open SharpDX.Direct3D12
open SharpDX

open System.Runtime.InteropServices
open System

open SharpDX.Mathematics.Interop

open Common

// ----------------------------------------------------------------------------------------------------
// Ein Scene stellt eine graphische Ausgangssituation her
// ----------------------------------------------------------------------------------------------------
module Structures =

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

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type DirectionalLight =
        struct
            val mutable Color: Color4 // 16 bytes
            val mutable Direction: Vector3 // 12 bytes
            val _padding: float32 // 4 bytes

            new(color, direction) =
                { Color = color
                  Direction = direction
                  _padding = 0.0f }

            new(color) = DirectionalLight(color, Vector3.Zero)
        end

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type FrameConstants =
        struct
            val mutable Light: DirectionalLight
            new(light) = { Light = light }
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
                    emissiveFactor=material.EmissiveFactor
                    occlusionStrength=1.0f
                    metallicRoughnessValues= material.MetallicRoughnessValues
                    padding1=0.0f
                    baseColorFactor=material.BaseColourFactor
                    camera=Vector3.One
                    padding2=0.0f
                }
        end

    // TODO
    // Wrap Filter etc konvertieren
    let DynamicSamplerDesc(sampler:Sampler) =
        new SamplerStateDescription (
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                BorderColor = new RawColor4(0.0f, 0.0f, 0.0f, 0.0f),
                ComparisonFunction = Comparison.Never,
                Filter = Filter.MinMagMipLinear,
                MaximumAnisotropy = 16,
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