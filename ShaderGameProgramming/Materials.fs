namespace ShaderGameProgramming
//
//  MaterialsAndTextures.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open SharpDX

open Base.ModelSupport

// ----------------------------------------------------------------------------------------------------
// Speicher für viel benutzte Eigenschaften
// ----------------------------------------------------------------------------------------------------
module Materials =

    let GRAFIC_ECO_PATH =
        "C:/Users/Lugi2/source/F#/Framework/EcoGrafics/ExampleSurfaces/bin/x64/Debug/net471/texture/"

    let ECO_PATH =
        System
            .Reflection
            .Assembly
            .GetExecutingAssembly()
            .GetName()

    let DIFFUSE_LIGHT = Color.LightYellow.ToColor4()

    // ----------------------------------------------------------------------------------------------------
    // FRESNEL
    // ----------------------------------------------------------------------------------------------------
    // WATER   0.02, 0.02, 0.02
    // GLASS   0.08  0.08  0.08
    // PLASTIC 0.05  0.05  0.05
    // GOLD    1.00  0.71  0.29
    // SILVER  0.95  0.93  0.88
    // COPPER  0.95  0.64  0.54

    // ----------------------------------------------------------------------------------------------------
    // MATERIAL
    // ----------------------------------------------------------------------------------------------------
    let MAT (name, diffuseAlbedo, fresnelR0, roughness) =
        new Material(
            name = name,
            diffuseAlbedo = diffuseAlbedo,
            fresnelR0 = fresnelR0,
            roughness = roughness,
            matTransform=Matrix.Identity
        )

    let MAT_GRASS =
        MAT("grass", new Vector4(0.2f, 0.6f, 0.2f, 1.0f), new Vector3(0.01f), 0.125f)

    let MAT_WATER =
        MAT("water", new Vector4(0.0f, 0.2f, 0.6f, 1.0f), new Vector3(0.1f), 0.0f)

    let MAT_BRICKS = MAT("bricks0", Vector4.One, Vector3(0.1f), 0.3f)

    let MAT_TILE =
        MAT("tile0", new Vector4(0.9f, 0.9f, 0.9f, 1.0f), Vector3(0.2f), 0.1f)

    let MAT_MIRROR =
        MAT("mirror", Vector4(0.0f, 0.0f, 0.0f, 1.0f), Vector3(0.98f, 0.97f, 0.95f), 0.1f)

    let MAT_SKY = MAT("sky", Vector4.One, new Vector3(0.1f), 1.0f)

    let MAT_SKULL =
        MAT("skull", new Vector4(0.8f, 0.8f, 0.8f, 1.0f), Vector3(0.2f), 0.2f)

    let MAT_WOOD = MAT("wood ", Color.White.ToVector4(), Vector3(0.05f), 0.2f)

    let MAT_WIREFENCE = MAT("wirefence", new Vector4(1.0f), Vector3(0.02f), 0.2f)

    let MAT_STONE = MAT("stone", Color.White.ToVector4(), Vector3(0.1f), 0.3f)

    let MAT_GRAY =
        MAT("gray", new Vector4(0.7f, 0.7f, 0.7f, 1.0f), Vector3(0.04f), 0.0f)

    let MAT_HIGHLIGHT =
        MAT("highlight", Vector4(1.0f, 1.0f, 0.0f, 0.6f), Vector3(0.06f), 0.0f)

    let MAT_CRATE = MAT("crate", Color.White.ToVector4(), Vector3(0.05f), 0.2f)

    let MAT_ICE = MAT("ice", Color.White.ToVector4(), Vector3(0.1f), 0.0f)

    let MAT_TREESPRITES = MAT("treeSprites", new Vector4(1.0f), Vector3(0.01f), 0.125f)

    let MAT_CHECKERTILE =
        MAT("checkertile", Color.White.ToVector4(), Vector3(0.07f), 0.3f)

    let MAT_ICEMIRROR =
        MAT("icemirror", new Vector4(1.0f, 1.0f, 1.0f, 0.3f), Vector3(0.1f), 0.5f)

    let MAT_SHADOW =
        MAT("shadow", new Vector4(0.0f, 0.0f, 0.0f, 0.5f), Vector3(0.001f), 0.0f) 

    let MAT_COPPER =
        MAT("copper", Vector4(0.95f, 0.64f, 0.54f, 0.5f), Vector3(0.001f), 0.0f) 
