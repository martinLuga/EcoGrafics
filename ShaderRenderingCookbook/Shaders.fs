namespace ShaderRenderingCookbook
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
// Shader
// ----------------------------------------------------------------------------------------------------
module Shaders =

    let vertexShaderDesc = 
        ShaderDescription(ShaderType.Vertex,    "shader",  "VS","VSMain","vs_5_1", ShaderUsage.Required, rootSignatureDesc)

    let vertexShaderSkyDesc = 
        ShaderDescription(ShaderType.Vertex,    "shader",  "Box","VSMain","vs_5_1", ShaderUsage.Required, rootSignatureDesc)

    let vertexShaderPBRDesc = 
        ShaderDescription(ShaderType.Vertex,    "shader",  "PbrVS","main","vs_5_1", ShaderUsage.Required, rootSignatureDesc)
    
    let pixelShaderSkyDesc = 
        ShaderDescription(ShaderType.Pixel,     "shader",  "Box","PSMain","ps_5_1", ShaderUsage.Required, rootSignatureDesc)

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

    // ----------------------------------------------------------------------------------------------------
    // Die Defaultshaders werden benögt, wenn die Shaders nicht direkt gesetzt werden können
    // Z.B bei Wavefront-Objekten (diese enthalten viele Parts mit unterschiedlichen Topologien)
    // ----------------------------------------------------------------------------------------------------
    let InitDefaultShaders() =
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Triangle, PrimitiveTopology.TriangleList, pixelShaderBlinnPhongDesc)
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Triangle, PrimitiveTopology.TriangleList, vertexShaderDesc)

        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Line, PrimitiveTopology.LineList, vertexShaderDesc);
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Line, PrimitiveTopology.LineList, pixelShaderBlinnPhongDesc)

        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Patch, PrimitiveTopology.PatchListWith4ControlPoints, vertexShaderTesselateDesc);
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Patch, PrimitiveTopology.PatchListWith4ControlPoints, pixelShaderPhongDesc);
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Patch, PrimitiveTopology.PatchListWith4ControlPoints, domainShaderQuadDesc)
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Patch, PrimitiveTopology.PatchListWith4ControlPoints, hullShaderQuadDesc)

        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Patch, PrimitiveTopology.PatchListWith3ControlPoints, vertexShaderTesselateDesc);
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Patch, PrimitiveTopology.PatchListWith3ControlPoints, pixelShaderPhongDesc);
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Patch, PrimitiveTopology.PatchListWith3ControlPoints, domainShaderTriDesc)
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Patch, PrimitiveTopology.PatchListWith3ControlPoints, hullShaderTriDesc)

        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Patch, PrimitiveTopology.PatchListWith6ControlPoints, vertexShaderTesselateDesc)
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Patch, PrimitiveTopology.PatchListWith6ControlPoints, pixelShaderPhongDesc);
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Patch, PrimitiveTopology.PatchListWith6ControlPoints, domainShaderTriDesc)
        ShaderCache.AddShaderFromDesc(PrimitiveTopologyType.Patch, PrimitiveTopology.PatchListWith6ControlPoints, hullShaderTriDesc)