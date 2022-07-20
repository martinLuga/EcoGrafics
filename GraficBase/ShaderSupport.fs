namespace GraficBase
//
//  Camera.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open SharpDX.Direct3D12

open DirectX.Assets

open Base
open ShaderSupport
open ModelSupport
open Framework

// ----------------------------------------------------------------------------------------------------
//  Shader specifics
//  Shader comes from: 1.Part  2.ShaderCache
// ----------------------------------------------------------------------------------------------------
module ShaderSupport =

    // Shader comes from: 1.Part  2.ShaderCache
    let currentVertexShader (part: Part) =
        match part.Shaders.VertexShaderDesc with
        | { ShaderDescription.Use = ShaderUsage.ToBeFilledIn } ->
            ShaderCache.GetShader(ShaderType.Vertex, part.Shape.TopologyType, part.Shape.Topology)
        | { ShaderDescription.Use = ShaderUsage.Required } -> part.Shaders.VertexShaderDesc
        | { ShaderDescription.Use = ShaderUsage.NotRequired } -> raiseException ("VertexShader muss gesetzt sein")
        | _ -> raiseException ("ShaderUsage not set")

    let currentPixelShader (part: Part) =
        match part.Shaders.PixelShaderDesc with
        | { ShaderDescription.Use = ShaderUsage.ToBeFilledIn } ->
            ShaderCache.GetShader(ShaderType.Pixel, part.Shape.TopologyType, part.Shape.Topology)
        | { ShaderDescription.Use = ShaderUsage.NotRequired } -> ShaderDescription.CreateNotRequired(ShaderType.Pixel)
        | { ShaderDescription.Use = ShaderUsage.Required } -> part.Shaders.PixelShaderDesc
        | _ -> raiseException ("ShaderUsage not set")

    let currentDomainShader (part: Part) =
        match part.Shaders.DomainShaderDesc with
        | { ShaderDescription.Use = ShaderUsage.ToBeFilledIn } ->
            ShaderCache.GetShader(ShaderType.Domain, part.Shape.TopologyType, part.Shape.Topology)
        | { ShaderDescription.Use = ShaderUsage.Required } -> part.Shaders.DomainShaderDesc
        | { ShaderDescription.Use = ShaderUsage.NotRequired } -> ShaderDescription.CreateNotRequired(ShaderType.Domain)
        | _ -> raiseException ("ShaderUsage not set")

    let currentHullShader (part: Part) =
        match part.Shaders.HullShaderDesc with
        | { ShaderDescription.Use = ShaderUsage.ToBeFilledIn } ->
            ShaderCache.GetShader(ShaderType.Hull, part.Shape.TopologyType, part.Shape.Topology)
        | { ShaderDescription.Use = ShaderUsage.Required } -> part.Shaders.HullShaderDesc
        | { ShaderDescription.Use = ShaderUsage.NotRequired } -> ShaderDescription.CreateNotRequired(ShaderType.Hull)
        | _ -> raiseException ("ShaderUsage not set")

    // RootSignature 
    let currentRootSignatureDesc(part:Part, rootSignatureDesc:RootSignatureDescription) =
        if isRootSignatureDescEmpty(part.Shaders.VertexShaderDesc.RootSignature) then
            let fromCache = ShaderCache.GetShader(ShaderType.Vertex, part.Shape.TopologyType, part.Shape.Topology)
            if fromCache = null || (fromCache.Use=ShaderUsage.ToBeFilledIn) then
                rootSignatureDesc
            else
                fromCache.RootSignature
        else 
            part.Shaders.VertexShaderDesc.RootSignature     
