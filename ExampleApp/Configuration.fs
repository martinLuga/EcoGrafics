namespace ExampleApp
//
//  Configuration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX

open log4net

open ApplicationBase.GraficSystem  
open ApplicationBase.WindowLayout
open ApplicationBase.WindowControl
open ApplicationBase.ShaderConfiguration

// ----------------------------------------------------------------------------------------------------
// Standard-Konfiguration für das GraphicSystem 
// ----------------------------------------------------------------------------------------------------
module Configuration = 

    let logger = LogManager.GetLogger("Configurations")

    let Configure() = 

        // Window
        MySystem.CreateInstance(graficWindow, [pipelineConfigBasic; pipelineConfigTesselateQuad; pipelineConfigTesselateTri ]) 
        
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