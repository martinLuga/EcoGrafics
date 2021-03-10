namespace FrameworkTests
//
//  Configuration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX
open SharpDX.Direct3D12
open SharpDX.DXGI

open log4net

open ApplicationBase.GraficSystem  
open ApplicationBase.WindowLayout
open ApplicationBase.WindowControl
open ApplicationBase.ShaderConfiguration

open GPUModel.MyPipelineConfiguration

open Shader.ShaderSupport

open DirectX.Assets

// ----------------------------------------------------------------------------------------------------
// Konfiguration für die Unit-Tests
// ----------------------------------------------------------------------------------------------------
module Configuration = 

    let logger = LogManager.GetLogger("Configurations")

    let Configure() = 
        
        let pipelineConfigTest = 
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

        MySystem.CreateInstance([pipelineConfigTest])         
        MySystem.Instance.LoadTextureFiles("EcoGrafics", "ExampleApp", "textures")     

        // Camera and light
        initLight (new Vector3( -15.0f,  -15.0f,  10.0f), Color.White)     // Nach links hinten nach unten

        // Camera new
        initCamera(
            Vector3( 0.0f, 5.0f, -15.0f),   // Camera position
            Vector3.Zero,                   // Camera target
            aspectRatio,                    // Aspect ratio
            MathUtil.TwoPi / 200.0f,        // Scrollamount horizontal
            MathUtil.TwoPi / 200.0f)        // Scrollamount vertical