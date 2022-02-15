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

// ----------------------------------------------------------------------------------------------------
// Ein Scene stellt eine graphische Ausgangssituation her
// ----------------------------------------------------------------------------------------------------
module GltfSupport =

    [<type: StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type ViewConstants =
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

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type FrameConstants =
        struct
            val mutable Light: DirectionalLight
            new(light) = { Light = light }
        end

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type ObjectConstants =
        struct
            val mutable emissiveFactor: Vector3
            val mutable occlusionStrength: float32
            val mutable metallicRoughnessValues: Vector2
            val mutable padding1: float32
            val mutable baseColorFactor: Color4
            val mutable camera: Vector3
            val mutable padding2: float32
        end

    let inputLayoutDescription =
        new InputLayoutDescription(
            [| new InputElement("NORMAL", 0, Format.R32G32B32_Float, 0, 0)
               new InputElement("POSITION", 0, Format.R32G32B32_Float, 12, 0)
               new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0) |]
        )

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

    let rootSignatureGltfDesc =
        let slotRootParameters =
            [| new RootParameter(ShaderVisibility.Vertex,new RootDescriptor(0, 0), RootParameterType.ConstantBufferView)    // b0 : ModelViewProjectionConstantBuffer
               new RootParameter(ShaderVisibility.All, new RootDescriptor(1, 0), RootParameterType.ConstantBufferView)      // b1 : Frame, Light
               new RootParameter(ShaderVisibility.All, new RootDescriptor(2, 0), RootParameterType.ConstantBufferView)      // b2 : Object, Material

               new RootParameter(ShaderVisibility.Pixel, new DescriptorRange(DescriptorRangeType.ShaderResourceView, 5, 0)) // t0 : textures
               new RootParameter(ShaderVisibility.Pixel, new DescriptorRange(DescriptorRangeType.ShaderResourceView, 3, 8)) // t8 : textures 
               new RootParameter(ShaderVisibility.Pixel, new DescriptorRange(DescriptorRangeType.Sampler, 5, 0))            // s0 : samplers 
               new RootParameter(ShaderVisibility.Pixel, new DescriptorRange(DescriptorRangeType.Sampler, 3, 8))            // s8 : samplers 
            |]

        new RootSignatureDescription(
            RootSignatureFlags.AllowInputAssemblerInputLayout,
            slotRootParameters
        )