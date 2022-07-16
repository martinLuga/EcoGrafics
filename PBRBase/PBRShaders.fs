namespace PBRBase
//
//  ShaderConfiguration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
// 

open Base.ShaderSupport 

open ShaderPBR.Pipeline

// ----------------------------------------------------------------------------------------------------
// Shader used in PBR
// ----------------------------------------------------------------------------------------------------
module Shaders =

    let vertexShaderDesc = 
        ShaderDescription(ShaderType.Vertex,    "shader",  "VS","VSMain","vs_5_1", ShaderUsage.Required, rootSignatureDesc)
  
    let vertexShaderPBRDesc = 
        ShaderDescription(ShaderType.Vertex,    "shader",  "PbrVS","main","vs_5_1", ShaderUsage.Required, rootSignatureDesc)
 
    let pixelShaderPBRDesc = 
        ShaderDescription(ShaderType.Pixel,     "shader",  "PbrPS","main","ps_5_1", ShaderUsage.Required, rootSignatureDesc)
    
    let vertexShaderTesselateDesc = 
        ShaderDescription(ShaderType.Vertex,    "shader",  "VS","VSPassThruTessellate","vs_5_1", ShaderUsage.Required, rootSignatureDesc)

    let vertexShaderDefaultDesc = 
        ShaderDescription(ShaderType.Vertex,    "shader",  "Default","VS","vs_5_1", ShaderUsage.Required, rootSignatureDesc)

    let pixelShaderDefaultDesc = 
        ShaderDescription(ShaderType.Pixel,     "shader",  "Default","PS","ps_5_1", ShaderUsage.Required, rootSignatureDesc)
 
    let pixelShaderSimpleDesc = 
        ShaderDescription(ShaderType.Pixel,     "shader",  "SimplePS","PSMain","ps_5_1", ShaderUsage.Required, rootSignatureDesc)

    let domainShaderQuadDesc = 
        ShaderDescription(ShaderType.Domain,    "shader",  "TessellateQuad","DS_Quads","ds_5_1", ShaderUsage.Required, rootSignatureDesc)

    let hullShaderQuadDesc = 
        ShaderDescription(ShaderType.Hull,      "shader",  "TessellateQuad","HS_QuadsInteger","hs_5_1", ShaderUsage.Required, rootSignatureDesc)

    let domainShaderTriDesc = 
        ShaderDescription(ShaderType.Domain,    "shader",  "TessellateTri","DS_Triangles","ds_5_1", ShaderUsage.Required, rootSignatureDesc)

    let domainShaderTesselatePhongDesc = 
        ShaderDescription(ShaderType.Domain,    "shader",  "TessellatePhong","DS_PhongTessellation","ds_5_1", ShaderUsage.Required, rootSignatureDesc)

    let hullShaderTriDesc = 
        ShaderDescription(ShaderType.Hull,      "shader",  "TessellateTri","HS_TrianglesInteger","hs_5_1", ShaderUsage.Required, rootSignatureDesc)
 
    let pixelShaderPhongDesc = 
         ShaderDescription(ShaderType.Pixel,    "shader",  "PhongPS","PSMain","ps_5_1", ShaderUsage.Required, rootSignatureDesc)
 
    let pixelShaderLambertDesc = 
        ShaderDescription(ShaderType.Pixel,     "shader",  "DiffusePS","PSMain","ps_5_1", ShaderUsage.Required, rootSignatureDesc)

    let pixelShaderBlinnPhongDesc = 
        ShaderDescription(ShaderType.Pixel,     "shader",  "BlinnPhongPS","PSMain","ps_5_1", ShaderUsage.Required, rootSignatureDesc)