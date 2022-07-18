namespace ShaderRenderingCookbook
//
//  Interface.fs
//
//  Created by Martin Luga on 16.03.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open Base.ModelSupport

open SharpDX
open SharpDX.Mathematics.Interop

open Structures

// ----------------------------------------------------------------------------------------------------
// Strukturen aus ShaderRenderingCookbook  
// ----------------------------------------------------------------------------------------------------
module Interface =

    let shaderMaterial(material:Material, hasTexture:bool)  = 
        new MaterialConstants( 
            Ambient = material.Ambient,
            Diffuse = material.Diffuse,
            Specular = material.Specular,
            SpecularPower = material.SpecularPower,
            Emissive = material.Emissive,
            HasTexture = RawBool(hasTexture), 
            UVTransform = Matrix.Identity
        ) 

