namespace ShaderGameProgramming
//
//  ShaderConfiguration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
// 

open Base.ShaderSupport

open SharpDX.Direct3D 
open SharpDX.Direct3D12

open Pipeline

// ----------------------------------------------------------------------------------------------------
// Example Shader
// ----------------------------------------------------------------------------------------------------
module Shaders =

 
    let vertexShaderSkyDesc = 
        ShaderDescription(ShaderType.Vertex,    "shader",  "Box","VSMain","vs_5_1", ShaderUsage.Required, rootSignatureDesc)
    
    let pixelShaderSkyDesc = 
        ShaderDescription(ShaderType.Pixel,     "shader",  "Box","PSMain","ps_5_1", ShaderUsage.Required, rootSignatureDesc)

    let vertexShaderDefaultDesc = 
        ShaderDescription(ShaderType.Vertex,    "shader",  "Default","VS","vs_5_1", ShaderUsage.Required, rootSignatureDesc)

    let pixelShaderDefaultDesc = 
        ShaderDescription(ShaderType.Pixel,     "shader",  "Default","PS","ps_5_1", ShaderUsage.Required, rootSignatureDesc)
 
    // ----------------------------------------------------------------------------------------------------
    // Die Defaultshaders werden benögt, wenn die Shaders nicht direkt gesetzt werden können
    // Z.B bei Wavefront-Objekten (diese enthalten viele Parts mit unterschiedlichen Topologien)
    // ----------------------------------------------------------------------------------------------------
    let InitDefaultShaders() =
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Triangle, PrimitiveTopology.TriangleList, pixelShaderDefaultDesc)
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Triangle, PrimitiveTopology.TriangleList, vertexShaderDefaultDesc)