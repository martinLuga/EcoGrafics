namespace ShaderGameProgramming
//
//  Interface.fs
//
//  Created by Martin Luga on 17.07.22.
//  Port of Luna, Frank D. Introduction To 3D Game Programming With Direct X 12 
//

open SharpDX 

open Structures

// ----------------------------------------------------------------------------------------------------
// Material  -  Diffuse Lighting
//
// The diffuse albedo specifies the amount of incoming light that the surface reflects
//
// The Fresnel equations mathematically describe the percentage of incoming light that is reflected, 0 ≤ RF ≤ 1.

// ----------------------------------------------------------------------------------------------------
module Interface =

    let shaderMaterial(material:Material, hasTexture:bool)  = 
        new MaterialConstants(
            material.DiffuseAlbedo,         // diffuseAlbedo  
            material.FresnelR0,             // fresnelR0 
            material.Roughness,             // roughness  
            material.MatTransform           // matTransform
        ) 