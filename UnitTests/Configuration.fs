namespace ecografics
//
//  Configuration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX
open SharpDX.DXGI

open log4net

open GraficBase.GraficWindow
open GraficBase.GraficController 
open GraficBase.ShaderConfiguration

open GPUModel.MyPipelineConfiguration

open Shader.ShaderSupport

open DirectX.Assets

// ----------------------------------------------------------------------------------------------------
// Konfiguration für die Unit-Tests
// ----------------------------------------------------------------------------------------------------
module Configuration = 

    let logger = LogManager.GetLogger("Configurations")

    let  mutable myWindow = new MyWindow()

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
                topologyTypeDesc=topologyTriangleDescription, 
                blendStateDesc=blendOpaqueDescription,
                rasterizerStateDesc=rasterSolidDescription
            )

        MyController.CreateInstance( 
            "UnitTests",
            myWindow,
            [ pipelineConfigTest ],
            ShaderClass.PhongPSType,
            RasterType.Solid,
            BlendType.Opaque
        ) 
        
        MyController.Instance.initLight (new Vector3( -15.0f,  -15.0f,  10.0f), Color.White)  
        MyController.Instance.InitDefaultCamera()