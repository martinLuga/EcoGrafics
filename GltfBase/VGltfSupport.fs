namespace GltfBase
//
//  VGltfSupport.fs
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
open VGltf.Types

module VGltfSupport = 

    [<type: StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type MaterialConstants =
        struct
            val mutable AlphaCutoff: float32
            val mutable AlphaMode: Material.AlphaModeEnum
            val mutable DoubleSided: bool
            val mutable EmissiveFactor: float32 []
            val mutable EmissiveTexture: Material.EmissiveTextureInfoType
            val mutable NormalTexture: Material.NormalTextureInfoType
            val mutable OcclusionTexture: Material.OcclusionTextureInfoType
            val mutable PbrMetallicRoughness: Material.PbrMetallicRoughnessType

            new(material: Material) =
                { AlphaCutoff = material.AlphaCutoff
                  AlphaMode = material.AlphaMode
                  DoubleSided = material.DoubleSided
                  EmissiveFactor = material.EmissiveFactor
                  EmissiveTexture = material.EmissiveTexture
                  NormalTexture = material.NormalTexture
                  OcclusionTexture = material.OcclusionTexture
                  PbrMetallicRoughness = material.PbrMetallicRoughness }
        end

