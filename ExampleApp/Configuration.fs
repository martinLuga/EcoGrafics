namespace ExampleApp
//
//  Configuration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Windows.Forms

open SharpDX

open log4net

open ApplicationBase.GraficSystem  
open ApplicationBase.WindowLayout
open ApplicationBase.WindowControl
open ApplicationBase.ShaderConfiguration
open ApplicationBase.ScenarioSupport

open Shader.ShaderSupport 

/// <summary>
/// Konfiguration der Anwendung
/// </summary>
module Configuration = 

    let logger = LogManager.GetLogger("Configurations")

    /// <summary>
    ///  Menue konfigurieren
    /// </summary>
    let ConfigureMenue () =  
        logger.Info("Configuration.Menue")

        /// <summary>    
        /// Geometry Menue 
        /// </summary> 
        let geometryMenue = 
            let geometryMenuItem = new ToolStripMenuItem("&Geometry")
            let sphereMenuItem = new ToolStripMenuItem("&Sphere")
            let cubeMenuItem = new ToolStripMenuItem("&Cube")
            let adobeMenuItem = new ToolStripMenuItem("&Adobe")
            let pyramidMenuItem = new ToolStripMenuItem("&Pyramid")
            let cylinderMenuItem = new ToolStripMenuItem("&Cylinder")
            let skullMenuItem = new ToolStripMenuItem("&Skull")
            let carMenuItem = new ToolStripMenuItem("&Car")
            let atomBondMenuItem = new ToolStripMenuItem("&Atom Bond")
            let atomBuilderMenuItem = new ToolStripMenuItem("&Atombuilder")
            let korpusMenuItem = new ToolStripMenuItem("&Korpus")
            let groundPlaneMenuItem = new ToolStripMenuItem("&Plane")
            let icosahedronMenuItem = new ToolStripMenuItem("&Icosahedron")
            let manyObjectsMenuItem = new ToolStripMenuItem("&Many Objects")
            let twoDMenuItem = new ToolStripMenuItem("&2D")

            geometryMenuItem.DropDownItems.Add(cubeMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(sphereMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(pyramidMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(adobeMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(cylinderMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(skullMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(carMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(atomBondMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(atomBuilderMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(korpusMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(groundPlaneMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(icosahedronMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(manyObjectsMenuItem)|>ignore
            geometryMenuItem.DropDownItems.Add(twoDMenuItem)|>ignore

            sphereMenuItem.Click.Add(fun _      -> execScenarioNamed("Sphere")) 
            cubeMenuItem.Click.Add(fun _        -> execScenarioNamed("Cube")) 
            pyramidMenuItem.Click.Add(fun _     -> execScenarioNamed("Adobe")) 
            adobeMenuItem.Click.Add(fun _       -> execScenarioNamed("Pyramid")) 
            cylinderMenuItem.Click.Add(fun _    -> execScenarioNamed("Cylinder")) 
            skullMenuItem.Click.Add(fun _       -> execScenarioNamed("SkullContour")) 
            carMenuItem.Click.Add(fun _         -> execScenarioNamed("CarContour")) 
            atomBondMenuItem.Click.Add(fun _    -> execScenarioNamed("AtomWithBond")) 
            atomBuilderMenuItem.Click.Add(fun _ -> execScenarioNamed("AtomBuilder")) 
            korpusMenuItem.Click.Add(fun _      -> execScenarioNamed("Korpus")) 
            groundPlaneMenuItem.Click.Add(fun _ -> execScenarioNamed("GroundPlane")) 
            icosahedronMenuItem.Click.Add(fun _ -> execScenarioNamed("Icosahedron")) 
            manyObjectsMenuItem.Click.Add(fun _ -> execScenarioNamed("ManyObjects")) 
            twoDMenuItem.Click.Add(fun _        -> execScenarioNamed("TwoD")) 
            geometryMenuItem

        /// <summary>    
        /// Setting Menue 
        /// </summary> 
        let settingMenue =
            let settMenuItem = settingMenueStandard
            settMenuItem.DropDownItems.Add(geometryMenue)|>ignore
            settMenuItem

        let mainMenue = 
            let mainMenu = new  MenuStrip() 
            mainMenu.Items.Add(fileSubmenueStandard)    |>ignore 
            mainMenu.Items.Add(viewSubmenueStandard)    |>ignore
            mainMenu.Items.Add(settingMenue)            |>ignore
            mainMenu

        mainWindow.MainMenuStrip <- mainMenue
        mainWindow.Controls.Add(mainWindow.MainMenuStrip) 
 

     /// <summary>
     ///  System konfigurieren
     /// </summary>
    let ConfigureSystem() = 
        logger.Info("Configuration.System")

        // Window
        MySystem.CreateInstance([pipelineConfigBasic; pipelineConfigTesselateQuad; pipelineConfigTesselateTri ]) 
        MySystem.Instance.ConfigurePipeline(ShaderClass.PhongPSType, RasterType.Solid, BlendType.Opaque)      
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