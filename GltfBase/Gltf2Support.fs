namespace GltfBase
//
//  Gltf2Support.fs
//
//  Created by Martin Luga on 14.02.22.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open glTFLoader.Schema


open Gltf2Reader

open System.Runtime.InteropServices
open System

module Gltf2Support =

    [<type: StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type MaterialConstants =
        struct
            val mutable AlphaCutoff: float32
            val mutable AlphaMode: Material.AlphaModeEnum
            val mutable DoubleSided: bool
            //val mutable EmissiveFactor: float32 []
            //val mutable EmissiveTexture: Material.AlphaModeEnum
            //val mutable NormalTexture: Material.NormalTextureInfoType
            //val mutable OcclusionTexture: Material.OcclusionTextureInfoType
            //val mutable PbrMetallicRoughness: Material.PbrMetallicRoughnessType

            new(material: Material) =
                { AlphaCutoff = material.AlphaCutoff
                  AlphaMode = material.AlphaMode
                  DoubleSided = material.DoubleSided
                  //EmissiveFactor = material.EmissiveFactor
                  //EmissiveTexture = material.EmissiveTexture
                  //NormalTexture = material.NormalTexture
                  //OcclusionTexture = material.OcclusionTexture
                  //PbrMetallicRoughness = material.PbrMetallicRoughness
                }
        end

    let correctorGltf(path) = getGltf (path)

