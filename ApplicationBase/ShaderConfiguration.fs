namespace ApplicationBase
//
//  ShaderConfiguration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 

open SharpDX.DXGI
open SharpDX.Direct3D12

open DirectX.Assets

open Shader.ShaderSupport
open Shader.ShaderCompile
open GPUModel.MyPipelineConfiguration

// ----------------------------------------------------------------------------------------------------
// Shader
// ----------------------------------------------------------------------------------------------------
module ShaderConfiguration =

    let vertexShaderDesc = 
        ShaderDescription(ShaderClass.SimpleVSType, "ExampleApp","shaders","VS","VSMain","vs_5_0")

    let vertexShaderTesselateDesc = 
        ShaderDescription(ShaderClass.TesselatedVSType, "ExampleApp","shaders","VS","VSPassThruTessellate","vs_5_0")
 
    let pixelShaderSimpleDesc = 
        ShaderDescription(ShaderClass.SimplePSType, "ExampleApp","shaders","SimplePS","PSMain","ps_5_0")

    let domainShaderQuadDesc = 
        ShaderDescription(ShaderClass.QuadDSType, "ExampleApp","shaders","TessellateQuad","DS_Quads","ds_5_0")

    let hullShaderQuadDesc = 
        ShaderDescription(ShaderClass.QuadHSType, "ExampleApp","shaders","TessellateQuad","HS_QuadsInteger","hs_5_0")

    let domainShaderTriDesc = 
        ShaderDescription(ShaderClass.TriDSType, "ExampleApp","shaders","TessellateTri","DS_Triangles","ds_5_0")

    let hullShaderTriDesc = 
        ShaderDescription(ShaderClass.TriHSType, "ExampleApp","shaders","TessellateTri","HS_TrianglesInteger","hs_5_0")
 
    let pixelShaderPhongDesc = 
         ShaderDescription(ShaderClass.PhongPSType, "ExampleApp","shaders", "PhongPS","PSMain","ps_5_0")
 
    let pixelShaderLambertDesc = 
        ShaderDescription(ShaderClass.LambertPSType, "ExampleApp","shaders","DiffusePS","PSMain","ps_5_0")

    let pixelShaderBlinnPhongDesc = 
        ShaderDescription(ShaderClass.BlinnPhongPSType, "ExampleApp","shaders","BlinnPhongPS","PSMain","ps_5_0")

    let rasterWiredDescription =
        RasterizerDescription(RasterType.Wired, rasterizerStateWired)
    let rasterSolidDescription =
        RasterizerDescription(RasterType.Solid, rasterizerStateSolid)
    let AllRasterDescriptions =
        [rasterSolidDescription; rasterWiredDescription]
    let rasterizerDescFromType(rasterType:RasterType)=
        AllRasterDescriptions |> List.find (fun rastr -> rastr.Type = rasterType)

    let blendOpaqueDescription =
        BlendDescription(BlendType.Opaque, blendStateOpaque)
    let blendTransparentDescription =
        BlendDescription(BlendType.Transparent, blendStateTransparent)
    let AllBlendDescriptions =
        [blendOpaqueDescription; blendTransparentDescription]
    let blendDescFromType(blendType:BlendType)=
        AllBlendDescriptions |> List.find (fun blend -> blend.Type = blendType)

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
            primitiveTopologyType=PrimitiveTopologyType.Triangle,
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
            primitiveTopologyType=PrimitiveTopologyType.Patch,
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
            primitiveTopologyType=PrimitiveTopologyType.Patch,
            blendStateDesc=blendOpaqueDescription,
            rasterizerStateDesc=rasterSolidDescription
        )