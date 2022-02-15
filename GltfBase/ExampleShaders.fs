namespace GltfBase
//
//  ShaderConfiguration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
// 

open Base.ShaderSupport
open GraficBase.Cache

open DirectX.Pipeline

open SharpDX.Direct3D 
open SharpDX.Direct3D12

// ----------------------------------------------------------------------------------------------------
// Example Shader
// ----------------------------------------------------------------------------------------------------
module ExampleShaders =
    
    let vertexShaderSkyDesc = 
        ShaderDescription(ShaderType.Vertex,    "shader",  "Box","VSMain","vs_5_1", rootSignatureDesc, ShaderUsage.Required)

    let vertexShaderPBRDesc = 
        ShaderDescription(ShaderType.Vertex,    "shader",  "PbrVS","main","vs_5_1", rootSignatureDesc, ShaderUsage.Required)
    
    let pixelShaderSkyDesc = 
        ShaderDescription(ShaderType.Pixel,     "shader",  "Box","PSMain","ps_5_1", rootSignatureDesc, ShaderUsage.Required)

    let pixelShaderPBRDesc = 
        ShaderDescription(ShaderType.Pixel,     "shader",  "PbrPS","main","ps_5_1", rootSignatureDesc, ShaderUsage.Required)
    

    // ----------------------------------------------------------------------------------------------------
    // Die Defaultshaders werden benögt, wenn die Shaders nicht direkt gesetzt werden können
    // Z.B bei Wavefront-Objekten (diese enthalten viele Parts mit unterschiedlichen Topologien)
    // ----------------------------------------------------------------------------------------------------
    let InitDefaultShaders() =
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Triangle, PrimitiveTopology.TriangleList, vertexShaderPBRDesc)
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Triangle, PrimitiveTopology.TriangleList, pixelShaderPBRDesc)

        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Line, PrimitiveTopology.LineList, vertexShaderSkyDesc);
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Line, PrimitiveTopology.LineList, pixelShaderSkyDesc)