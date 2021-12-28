namespace ecografics
//
//  Initializations.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 

open Base.MaterialsAndTextures
open Base.ModelSupport
open Base.ObjectBase
open Base.ShaderSupport
open DirectX.Assets
open ExampleShaders
open Geometry.GeometricModel
open GPUModel.MyPipelineConfiguration
open log4net
open SharpDX
open SharpDX.Direct3D
open SharpDX.DXGI 

module Initializations = 

    let logger = LogManager.GetLogger("Tests")
    
    let device = new Direct3D12.Device(null, FeatureLevel.Level_11_0) 

    let LogOutputDisplayModes(output:Output, format:Format) =      
        let modeList = output.GetDisplayModeList(format, DisplayModeEnumerationFlags.Interlaced)             
        for x in modeList do 
            let n = x.RefreshRate.Numerator  
            let d = x.RefreshRate.Denominator  
            let text =  "Width = " +  x.Width.ToString() + " " + "Height = " + x.Height.ToString() + " " + "Refresh = " +  n.ToString() + "/" + d.ToString() + "\n" 
            logger.Debug(text)

    let LogAdapterOutputs(adapter:Adapter) =
        for out in adapter.Outputs do 
            let desc = out.Description 
            let text = "***Output: " + desc.DeviceName  + "\n" 
            logger.Info(text)
            LogOutputDisplayModes(out, Format.B8G8R8A8_UNorm)      

    let getGraphicObjects() =
        let sphere1 =
            new BaseObject(
                name = "sphere1",
                display = new Display(
                    parts= [
                        new Part(
                            shape = Kugel("SPHERE", 1.0f, Color.DimGray), 
                                material = MAT_EARTH,
                                texture = TEXT_EARTH 
                            )
                    ]
                ),
                position = Vector3(-4.0f, 0.0f, 0.0f)
            )   

        let sphere2 =
            new BaseObject(
                name = "sphere2",
                display = new Display(
                    parts= [
                        new Part(
                            shape = Kugel("SPHERE", 1.0f, Color.DimGray), 
                                material = MAT_EARTH,
                                texture = TEXT_EARTH 
                            )
                    ]
                ),
                position = Vector3(-4.0f, 0.0f, 0.0f)
            )    

        [sphere1; sphere2] 

    let pipelineConfigBasic = 
        new MyPipelineConfiguration(
            configName="Basic",
            inputLayoutDesc=layoutCookBook,
            rootSignatureDescription=rootSignatureDescCookBook,
            vertexShaderDesc=vertexShaderDesc,
            pixelShaderDesc=pixelShaderPhongDesc,
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