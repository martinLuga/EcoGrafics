namespace ecografics
//
//  ShaderConfiguration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
// 

open Base.ShaderSupport
open Shader.ShaderCompile

// ----------------------------------------------------------------------------------------------------
// Example Shader
// ----------------------------------------------------------------------------------------------------
module ExampleShaders =

    let vertexShaderDesc = 
        ShaderDescription(ShaderClass.SimpleVSType, "shaders","VS","VSMain","vs_5_0")

    let vertexShaderTesselateDesc = 
        ShaderDescription(ShaderClass.TesselatedVSType, "shaders","VS","VSPassThruTessellate","vs_5_0")

    let pixelShaderSimpleDesc = 
        ShaderDescription(ShaderClass.SimplePSType, "shaders","SimplePS","PSMain","ps_5_0")

    let domainShaderQuadDesc = 
        ShaderDescription(ShaderClass.QuadDSType, "shaders","TessellateQuad","DS_Quads","ds_5_0")

    let hullShaderQuadDesc = 
        ShaderDescription(ShaderClass.QuadHSType, "shaders","TessellateQuad","HS_QuadsInteger","hs_5_0")

    let domainShaderTriDesc = 
        ShaderDescription(ShaderClass.TriDSType, "shaders","TessellateTri","DS_Triangles","ds_5_0")

    let hullShaderTriDesc = 
        ShaderDescription(ShaderClass.TriHSType, "shaders","TessellateTri","HS_TrianglesInteger","hs_5_0")
 
    let pixelShaderPhongDesc = 
         ShaderDescription(ShaderClass.PhongPSType, "shaders", "PhongPS","PSMain","ps_5_0")
 
    let pixelShaderLambertDesc = 
        ShaderDescription(ShaderClass.LambertPSType, "shaders","DiffusePS","PSMain","ps_5_0")

    let pixelShaderBlinnPhongDesc = 
        ShaderDescription(ShaderClass.BlinnPhongPSType, "shaders","BlinnPhongPS","PSMain","ps_5_0")

    // ----------------------------------------------------------------------------------------------------
    // Access Shaders
    // ----------------------------------------------------------------------------------------------------
    let AllShaderDescriptions =
        [vertexShaderDesc; vertexShaderTesselateDesc; pixelShaderSimpleDesc;   domainShaderQuadDesc;  hullShaderQuadDesc; 
        domainShaderTriDesc;  hullShaderTriDesc; pixelShaderPhongDesc; pixelShaderLambertDesc; pixelShaderBlinnPhongDesc]

    let ShaderDescForType(klass:ShaderClass) =
        AllShaderDescriptions |> List.find (fun desc -> desc.Klass = klass)

    let AllShaders =
        AllShaderDescriptions |> List.map(fun desc ->  (desc.Klass, shaderFromFile(desc)))

    let ShaderWithClass(klasse:ShaderClass) =
        let shader = AllShaders |> List.find (fun shader -> (fst shader) = klasse)
        shader