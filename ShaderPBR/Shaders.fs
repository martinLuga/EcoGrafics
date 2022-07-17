namespace ShaderPBR
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
// Shaders
// ----------------------------------------------------------------------------------------------------
module Shaders =

    let vertexShaderDesc = 
        ShaderDescription(ShaderType.Vertex,    "shader",  "PbrVS","main","vs_5_1", ShaderUsage.Required, rootSignatureDesc)

    let pixelShaderDesc = 
        ShaderDescription(ShaderType.Pixel,     "shader",  "PbrPS","main","ps_5_1", ShaderUsage.Required, rootSignatureDesc)

    // ----------------------------------------------------------------------------------------------------
    // Die Defaultshaders werden benögt, wenn die Shaders nicht direkt gesetzt werden können
    // Z.B bei Wavefront-Objekten (diese enthalten viele Parts mit unterschiedlichen Topologien)
    // ----------------------------------------------------------------------------------------------------
    let InitDefaultShaders() =
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Triangle, PrimitiveTopology.TriangleList, pixelShaderDesc)
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Triangle, PrimitiveTopology.TriangleList, vertexShaderDesc)

        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Line, PrimitiveTopology.LineList, vertexShaderDesc);
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Line, PrimitiveTopology.LineList, pixelShaderDesc)