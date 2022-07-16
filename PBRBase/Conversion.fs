namespace PBRBase
//
//  Deployment.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open Base.ShaderSupport

open Base.Framework

open Base.ModelSupport 

open ShaderPBR
open Structures

open Builder

type TextureBase  = Base.ModelSupport.Texture
type MaterialBase = Base.ModelSupport.Material
type TextureGltf  = glTFLoader.Schema.Texture

// ----------------------------------------------------------------------------------------------------
// Deployment auf Base.Framework + Vertex
// EcoGrafics Technologie
// ---------------------------------------------------------------------------------------------------- 
module Conversion =

    let ToTexture(texture:TextureGltf, textureType:TextureTypePBR, builder:GltfBuilder) = 
        let gltf                = builder.Gltf
        let samplerIdx          = texture.Sampler.Value
        let textureIdx          = texture.Source.Value
        let sampler             = gltf.Samplers[samplerIdx] 
        let image               = gltf.Images[textureIdx]
        let imageData, bitmap   = builder.CreateImage(textureIdx, image.BufferView.Value) 
        let mimeType            = image.MimeType.Value.ToString()

        let samplerDesc =
            DynamicSamplerDesc(sampler)

        match textureType with
        | TextureTypePBR.baseColourTexture          -> TextureBaseColour(texture.Name, "", "",  imageData, mimeType, samplerDesc) :> TextureBase
        | TextureTypePBR.normalTexture              -> TextureNormal(texture.Name, "", "",  imageData, mimeType, samplerDesc) :> TextureBase
        | TextureTypePBR.emissionTexture            -> TextureEmission(texture.Name, "", "",  imageData, mimeType, samplerDesc) :> TextureBase
        | TextureTypePBR.occlusionTexture           -> TextureOcclusion(texture.Name, "", "",  imageData, mimeType, samplerDesc) :> TextureBase
        | TextureTypePBR.metallicRoughnessTexture   -> TextureMetallicRoughness(texture.Name, "", "",  imageData, mimeType, samplerDesc) :> TextureBase
        | _ -> raiseException("invalid Texture")

    let ToMaterial(builder:GltfBuilder, matIdx:int) =    
        let gltf     = builder.Gltf
        let material = gltf.Materials[matIdx] 
        let mutable textureGltf:TextureGltf = null
        let resultMaterial:MaterialPBR = new MaterialPBR()

        if material.PbrMetallicRoughness <> null then
            if material.PbrMetallicRoughness.BaseColorTexture <> null then
                let textureIdx = material.PbrMetallicRoughness.BaseColorTexture.Index
                textureGltf  <- gltf.Textures[textureIdx]
                if textureGltf <> null then
                    let text = ToTexture(textureGltf, TextureTypePBR.baseColourTexture, builder ) 
                    resultMaterial.BaseColourTexture <- text :?> TextureBaseColour   
                    
            if material.PbrMetallicRoughness.MetallicRoughnessTexture <> null then
                let textureIdx = material.PbrMetallicRoughness.MetallicRoughnessTexture.Index
                textureGltf <- gltf.Textures[textureIdx] 
                if textureGltf <> null then
                    let text = ToTexture(textureGltf, TextureTypePBR.metallicRoughnessTexture, builder )  
                    resultMaterial.MetallicRoughnessTexture <- text :?> TextureMetallicRoughness 
               
        if material.EmissiveTexture <> null then
            let textureIdx = material.EmissiveTexture.Index
            textureGltf <- gltf.Textures[textureIdx] 
            if textureGltf <> null then
                let text = ToTexture(textureGltf, TextureTypePBR.emissionTexture, builder )  
                resultMaterial.EmissionTexture <- text :?> TextureEmission      
                
        if material.NormalTexture <> null then
            let textureIdx = material.NormalTexture.Index
            textureGltf <- gltf.Textures[textureIdx] 
            if textureGltf <> null then
                let text = ToTexture(textureGltf, TextureTypePBR.normalTexture, builder )
                resultMaterial.NormalTexture <- text :?> TextureNormal
                        
        if material.OcclusionTexture <> null then
            let textureIdx = material.OcclusionTexture.Index
            textureGltf <- gltf.Textures[textureIdx] 
            if textureGltf <> null then
                let text = ToTexture(textureGltf, TextureTypePBR.occlusionTexture, builder )
                resultMaterial.OcclusionTexture <- text :?> TextureOcclusion

        resultMaterial