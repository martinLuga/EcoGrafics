namespace Gltf2Base
//
//  Conversion.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open SharpDX

open Base
open ShaderSupport
open ModelSupport 

open ShaderRenderingCookbook

open Builder
open Common

type TextureBase  = Base.ModelSupport.Texture
type MaterialBase = Base.ModelSupport.Material
type TextureGltf  = glTFLoader.Schema.Texture

// ----------------------------------------------------------------------------------------------------
// Deployment auf Base.Framework + Vertex
// EcoGrafics Technologie
// ---------------------------------------------------------------------------------------------------- 
module Conversion =

    let defaultMaterial = new Material( 
        name="default",
        ambient=Color4(0.2f),
        diffuse=Color.Gray.ToColor4(),
        specular=Color4.White,
        specularPower=5.0f,
        emissive=Color.Transparent.ToColor4()
    )

    let ToTexture(texture:TextureGltf, builder:GltfBuilder) = 
        let gltf                = builder.Gltf
        let textureIdx          = texture.Source.Value
        let image               = gltf.Images[textureIdx]
        let imageData, bitmap   = builder.CreateImage(textureIdx, image.BufferView.Value) 
        let mimeType            = image.MimeType.Value.ToString()        
        TextureBase(texture.Name, mimeType, imageData)

    let ToMaterialAndTexture(builder:GltfBuilder, matIdx:int) =    
        let gltf     = builder.Gltf
        let material = gltf.Materials[matIdx] 
        let mutable textureGltf:TextureGltf = null
        let mutable resultTexture:TextureBase = new TextureBase()        
        let mutable resultMaterial:MaterialBase = defaultMaterial

        if material.PbrMetallicRoughness <> null then
            if material.PbrMetallicRoughness.BaseColorTexture <> null then
                let textureIdx = material.PbrMetallicRoughness.BaseColorTexture.Index
                textureGltf  <- gltf.Textures[textureIdx]
                if textureGltf <> null then
                    resultTexture <- ToTexture(textureGltf, builder) 

        resultMaterial, resultTexture