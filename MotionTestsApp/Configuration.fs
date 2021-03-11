namespace MotionTests
//
//  Configuration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Windows.Forms

open log4net

open SharpDX

open ApplicationBase.MoveableObject
open ApplicationBase.ShaderConfiguration
open ApplicationBase.WindowLayout
open ApplicationBase.ScenarioSupport

open Shader.ShaderSupport 

open Simulation.SimulationSystem

open WindowControl

/// <summary>
/// Window und Menues
/// SimulationSystem
/// </summary>
module Configuration =

    let logger = LogManager.GetLogger("Configuration")

    /// <summary>
    ///  Window konfigurieren
    /// </summary>
    let ConfigureMenue () =  
        logger.Info("Configuration.Menue")
        // Menue Simulation
        let simulationSubmenue =  
            let simulationMenuItem = new ToolStripMenuItem("&Simulation")
            let startMenuItem = new ToolStripMenuItem("&Start Scenario")
            let nextMenuItem = new ToolStripMenuItem("&Next Scenario")
            let toggleMenuItem = new ToolStripMenuItem("&Toggle Motion")
            simulationMenuItem.DropDownItems.Add(startMenuItem)|>ignore
            simulationMenuItem.DropDownItems.Add(nextMenuItem)|>ignore
            simulationMenuItem.DropDownItems.Add(toggleMenuItem)|>ignore
            startMenuItem.Click.Add(fun _   -> startActiveScenario())
            nextMenuItem.Click.Add(fun _    -> startNextScenario())
            toggleMenuItem.Click.Add(fun _  -> MySimulation.Instance.toggleWorkflows ())
            simulationMenuItem
        
        // Menue View Erweiterung 
        // Toggle Umgebung
        let toggleUmgebungMenuItem = new ToolStripMenuItem("&Umgebung toggle")
        viewSubmenueStandard.DropDownItems.Add(toggleUmgebungMenuItem)|>ignore
        toggleUmgebungMenuItem.Click.Add(fun _ -> MySimulation.Instance.toggleUmgebungen ())

        // Menue Main
        let mainMenue = 
            let mainMenu = new  MenuStrip() 
            mainMenu.Items.Add(fileSubmenueStandard)|>ignore 
            mainMenu.Items.Add(settingMenueStandard)|>ignore
            mainMenu.Items.Add(viewSubmenueStandard)|>ignore 
            mainMenu.Items.Add(simulationSubmenue)|>ignore
            mainMenu

        mainWindow.MainMenuStrip <- mainMenue
        mainWindow.Controls.Add(mainWindow.MainMenuStrip)      

    /// <summary>
    ///  SimulationSystem konfigurieren
    /// </summary>

    let ConfigureSystem() =  
        logger.Info("Configuration.System")

        // Simulation System
        MySimulation.CreateInstance([pipelineConfigBasic; pipelineConfigTesselateQuad; pipelineConfigTesselateTri])
        MySimulation.Instance.ConfigurePipeline(ShaderClass.PhongPSType, RasterType.Wired, BlendType.Opaque)
        MySimulation.Instance.LoadTextureFiles("EcoGrafics", "ExampleApp", "textures") 
        MySimulation.Instance.TessellationFactor <- 1.0f