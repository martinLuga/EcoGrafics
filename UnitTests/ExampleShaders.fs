namespace ecografics
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

    let vertexShaderDesc = 
        ShaderDescription(ShaderType.Vertex,    "shaders",  "VS","VSMain","vs_5_1", ShaderUsage.Required, rootSignatureDesc)

    let vertexShaderSkyDesc = 
        ShaderDescription(ShaderType.Vertex,    "shaders",  "Box","VSMain","vs_5_1", ShaderUsage.Required, rootSignatureDesc)
    
    let pixelShaderSkyDesc = 
        ShaderDescription(ShaderType.Pixel,     "shaders",  "Box","PSMain","ps_5_1", ShaderUsage.Required, rootSignatureDesc)
    
    let vertexShaderTesselateDesc = 
        ShaderDescription(ShaderType.Vertex,    "shaders",  "VS","VSPassThruTessellate","vs_5_1", ShaderUsage.Required, rootSignatureDesc)

    let vertexShaderDefaultDesc = 
        ShaderDescription(ShaderType.Vertex,    "shaders",  "Default","VS","vs_5_1", ShaderUsage.Required, rootSignatureDesc)

    let pixelShaderDefaultDesc = 
        ShaderDescription(ShaderType.Pixel,     "shaders",  "Default","PS","ps_5_1", ShaderUsage.Required, rootSignatureDesc)
 
    let pixelShaderSimpleDesc = 
        ShaderDescription(ShaderType.Pixel,     "shaders",  "SimplePS","PSMain","ps_5_1", ShaderUsage.Required, rootSignatureDesc)

    let domainShaderQuadDesc = 
        ShaderDescription(ShaderType.Domain,    "shaders",  "TessellateQuad","DS_Quads","ds_5_1", ShaderUsage.Required, rootSignatureDesc)

    let hullShaderQuadDesc = 
        ShaderDescription(ShaderType.Hull,      "shaders",  "TessellateQuad","HS_QuadsInteger","hs_5_1", ShaderUsage.Required, rootSignatureDesc)

    let domainShaderTriDesc = 
        ShaderDescription(ShaderType.Domain,    "shaders",  "TessellateTri","DS_Triangles","ds_5_1", ShaderUsage.Required, rootSignatureDesc)

    let domainShaderTesselatePhongDesc = 
        ShaderDescription(ShaderType.Domain,    "shaders",  "TessellatePhong","DS_PhongTessellation","ds_5_1", ShaderUsage.Required, rootSignatureDesc)

    let hullShaderTriDesc = 
        ShaderDescription(ShaderType.Hull,      "shaders",  "TessellateTri","HS_TrianglesInteger","hs_5_1", ShaderUsage.Required, rootSignatureDesc)
 
    let pixelShaderPhongDesc = 
         ShaderDescription(ShaderType.Pixel,    "shaders",  "PhongPS","PSMain","ps_5_1", ShaderUsage.Required, rootSignatureDesc)
 
    let pixelShaderLambertDesc = 
        ShaderDescription(ShaderType.Pixel,     "shaders",  "DiffusePS","PSMain","ps_5_1", ShaderUsage.Required, rootSignatureDesc)

    let pixelShaderBlinnPhongDesc = 
        ShaderDescription(ShaderType.Pixel,     "shaders",  "BlinnPhongPS","PSMain","ps_5_1", ShaderUsage.Required, rootSignatureDesc)

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

