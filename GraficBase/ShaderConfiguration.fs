namespace GraficBase
//
//  ShaderConfiguration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 

open SharpDX.DXGI

open DirectX.Assets

open Shader.ShaderSupport
open Shader.ShaderCompile
open GPUModel

open  MyPipelineConfiguration

// ----------------------------------------------------------------------------------------------------
// Shader
// ----------------------------------------------------------------------------------------------------
module ShaderConfiguration =

    let vertexShaderDesc = 
        ShaderDescription(ShaderClass.SimpleVSType, "Shader","shaders","VS","VSMain","vs_5_0")

    let vertexShaderTesselateDesc = 
        ShaderDescription(ShaderClass.TesselatedVSType, "Shader","shaders","VS","VSPassThruTessellate","vs_5_0")
 
    let pixelShaderSimpleDesc = 
        ShaderDescription(ShaderClass.SimplePSType, "Shader","shaders","SimplePS","PSMain","ps_5_0")

    let domainShaderQuadDesc = 
        ShaderDescription(ShaderClass.QuadDSType, "Shader","shaders","TessellateQuad","DS_Quads","ds_5_0")

    let hullShaderQuadDesc = 
        ShaderDescription(ShaderClass.QuadHSType, "Shader","shaders","TessellateQuad","HS_QuadsInteger","hs_5_0")

    let domainShaderTriDesc = 
        ShaderDescription(ShaderClass.TriDSType, "Shader","shaders","TessellateTri","DS_Triangles","ds_5_0")

    let hullShaderTriDesc = 
        ShaderDescription(ShaderClass.TriHSType, "Shader","shaders","TessellateTri","HS_TrianglesInteger","hs_5_0")
 
    let pixelShaderPhongDesc = 
         ShaderDescription(ShaderClass.PhongPSType, "Shader","shaders", "PhongPS","PSMain","ps_5_0")
 
    let pixelShaderLambertDesc = 
        ShaderDescription(ShaderClass.LambertPSType, "Shader","shaders","DiffusePS","PSMain","ps_5_0")

    let pixelShaderBlinnPhongDesc = 
        ShaderDescription(ShaderClass.BlinnPhongPSType, "Shader","shaders","BlinnPhongPS","PSMain","ps_5_0")

    let AllShaderDescriptions =
        [vertexShaderDesc; vertexShaderTesselateDesc; pixelShaderSimpleDesc;   domainShaderQuadDesc;  hullShaderQuadDesc; 
        domainShaderTriDesc;  hullShaderTriDesc; pixelShaderPhongDesc; pixelShaderLambertDesc; pixelShaderBlinnPhongDesc]

    let ShaderDescForType(klass:ShaderClass) =
        AllShaderDescriptions |> List.find (fun desc -> desc.Klass = klass)

    let AllShaders =
        AllShaderDescriptions |> List.map(fun desc ->  (desc.Klass, shaderFromFile(desc.asFileInfo)))

    let ShaderWithClass(klasse:ShaderClass) =
        let (klasse, shader) = AllShaders |> List.find (fun shader -> (fst shader) = klasse)
        shader

    let pipelineConfigBasic = 
        new MyPipelineConfiguration(
            configName="Basic",
            inputLayoutDesc=layoutCookBook,
            rootSignatureDescription=rootSignatureDescCookBook,
            vertexShaderDesc=vertexShaderDesc,
            pixelShaderDesc=pixelShaderSimpleDesc,
            domainShaderDesc=ShaderDescription(),
            hullShaderDesc=ShaderDescription(),
            sampleDesc = new SampleDescription(1, 0),
            topologyTypeDesc=topologyTriangleDescription, 
            blendStateDesc=blendOpaqueDescription,
            rasterizerStateDesc=rasterSolidDescription
        )

    let pipelineConfigTesselateQuad  = 
        new MyPipelineConfiguration(
            configName="TesselatedQuad",
            inputLayoutDesc=layoutCookBook,
            rootSignatureDescription=rootSignatureDescCookBook,
            vertexShaderDesc=vertexShaderTesselateDesc,
            pixelShaderDesc=pixelShaderSimpleDesc,
            domainShaderDesc=domainShaderQuadDesc,
            hullShaderDesc=hullShaderQuadDesc,
            sampleDesc = new SampleDescription(1, 0),
            topologyTypeDesc=topologyPatchDescription, 
            blendStateDesc=blendOpaqueDescription,
            rasterizerStateDesc=rasterSolidDescription
        )

    let pipelineConfigTesselateTri = 
        new MyPipelineConfiguration(
            "TesselatedTri",
            inputLayoutDesc=layoutCookBook,
            rootSignatureDescription=rootSignatureDescCookBook,
            vertexShaderDesc=vertexShaderTesselateDesc,
            pixelShaderDesc=pixelShaderSimpleDesc,
            domainShaderDesc=domainShaderTriDesc,
            hullShaderDesc=hullShaderTriDesc,
            sampleDesc=new SampleDescription(1, 0),
            topologyTypeDesc=topologyPatchDescription, 
            blendStateDesc=blendOpaqueDescription,
            rasterizerStateDesc=rasterSolidDescription
        )