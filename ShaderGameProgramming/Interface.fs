namespace ShaderGameProgramming
//
//  Interface.fs
//
//  Created by Martin Luga on 17.07.22.
//  Port of Luna, Frank D. Introduction To 3D Game Programming With Direct X 12 
//

open SharpDX 

open Base.ModelSupport

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
    
    let shaderObject(_world, _view, _proj, _eyePos) = 
        let _invView        = Matrix.Invert(_view)
        let _invProj        = Matrix.Invert(_proj) 
        let _viewProj       = _view * _proj
        let _invViewProj    = Matrix.Invert(_viewProj) 
        let obj =
            new ObjectConstants(
                gView = Matrix.Transpose(_view),
                gInvView = Matrix.Transpose(_invView), 
                gProj = Matrix.Transpose(_proj) ,
                gInvProj = Matrix.Transpose(_invProj) ,
                gViewProj = Matrix.Transpose(_viewProj) ,
                gInvViewProj = Matrix.Transpose(_invViewProj), 
                EyePosW = _eyePos  
            )
        obj