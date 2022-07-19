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
open System
open System.Runtime.InteropServices

open DirectX.D3DUtilities

open Structures

// ----------------------------------------------------------------------------------------------------
// Strukturen aus ShaderRenderingCookbook  
// ----------------------------------------------------------------------------------------------------
module Interface =
    
    let frameLength = D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
    let matLength   = D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
    let itemLength  = D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()

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

    let shaderObject(_world, _view, _proj, _eyePos) = 
        let _invView        = Matrix.Invert(_view)
        let _invProj        = Matrix.Invert(_proj) 
        let _viewProj       = _view * _proj
        let _invViewProj    = Matrix.Invert(_viewProj) 
 
        Transpose (
            new ObjectConstants(
                World = _world,
                View = Matrix.Transpose(_view),
                InvView = Matrix.Transpose(_invView), 
                Proj = Matrix.Transpose(_proj) ,
                InvProj = Matrix.Transpose(_invProj) ,
                ViewProj = Matrix.Transpose(_viewProj) ,
                InvViewProj = Matrix.Transpose(_invViewProj), 
                WorldViewProjection= _world * _viewProj,
                WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(_world)),
                ViewProjection = _viewProj,
                EyePosW = _eyePos  
            )
        )

    let shaderFrame(tessellationFactor, lightColor, lightDir, cameraPosition) =    
        new FrameConstants(
            TessellationFactor = tessellationFactor, 
            Light = DirectionalLight(lightColor, lightDir),
            CameraPosition = cameraPosition    
        ) 

    let shaderLight(color:Color, lightDir:Vector3) =
        new DirectionalLight(color.ToColor4(), new Vector3(lightDir.X, lightDir.Y, lightDir.Z))

