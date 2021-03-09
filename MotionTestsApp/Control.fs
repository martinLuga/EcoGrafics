namespace MotionTests
//
//  Control.fs
//
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Windows.Forms

open log4net

open ApplicationBase.WindowLayout
open ApplicationBase.MoveableObject
open ApplicationBase.DisplayableObject 

open Base.Logging

open Simulation.SimulationSystem
open Simulation.ScenarioSupport

open WindowLayout
open WindowControl

/// <summary>
// Steuerung
// Shader-Auswahl
// Move Rotate
/// </summary>
module Control = 

    let logger = LogManager.GetLogger("app.Control")
    let logInfo  = Info(logger)
    let logDebug = Debug(logger)

    let mutable moveFactor = 0.0003f  
    let mutable moveUp = 0.0f   
    let mutable moveLeft = 0.0f  

    let TEST_SPEED = 0.3f
    let SPEED_FACTOR = 10.0f

    let mutable displayables:Displayable list = [] 
        
    let adjustSpeedBar(speed) = 
        let tickValue = (int)(speed * SPEED_FACTOR)
        logInfo("adjustSpeedBar") 
        logInfo("Object speed :" + speed.ToString())
        logInfo("Trackbar ticks      :" + tickValue.ToString())  
        objectSpeedTrackBar.Value <- tickValue
     
    let changeObjectSpeed(sender:int) =
        OBJECT_VELOCITY <- float32 sender / SPEED_FACTOR 
        adjustSpeedBar(OBJECT_VELOCITY)

        logInfo("Change object speed to " + OBJECT_VELOCITY.ToString())

    let addScenarioKeyMovements() =
        graficWindow.KeyDown.Add(fun e -> if e.KeyCode = Keys.N  then execNextScenario())  

    /// <summary>    
    /// Menues
    /// </summary> 
    
    // Menue Simulation
    let simulationSubmenue =  
        let simulationMenuItem = new ToolStripMenuItem("&Simulation")
        let nextMenuItem = new ToolStripMenuItem("&Next Scenario")
        let restartMenuItem = new ToolStripMenuItem("&Restart Scenario")
        let startWorkflowsMenuItem = new ToolStripMenuItem("&Start Motion")
        let toggleWorkflowsMenuItem = new ToolStripMenuItem("&Toggle Motion")
        simulationMenuItem.DropDownItems.Add(nextMenuItem)|>ignore
        simulationMenuItem.DropDownItems.Add(restartMenuItem)|>ignore
        simulationMenuItem.DropDownItems.Add(toggleWorkflowsMenuItem)|>ignore
        simulationMenuItem.DropDownItems.Add(startWorkflowsMenuItem)|>ignore
        nextMenuItem.Click.Add(fun _ -> startNextScenario())
        restartMenuItem.Click.Add(fun _ -> execActiveScenario ())
        toggleWorkflowsMenuItem.Click.Add(fun _ -> MySimulation.Instance.toggleWorkflows ())
        startWorkflowsMenuItem.Click.Add(fun _ -> MySimulation.Instance.startWorkflows ())
        simulationMenuItem

    // Menue View
    let toggleUmgebungMenuItem = new ToolStripMenuItem("&Umgebung toggle")
    viewSubmenueStandard.DropDownItems.Add(toggleUmgebungMenuItem)|>ignore
    toggleUmgebungMenuItem.Click.Add(fun _ -> MySimulation.Instance.toggleUmgebungen ())

    let setupMenue = 
        let mainMenu = new  MenuStrip() 
        mainMenu.Items.Add(fileSubmenueStandard)|>ignore 
        mainMenu.Items.Add(settingMenueStandard)|>ignore
        mainMenu.Items.Add(viewSubmenueStandard)|>ignore 
        mainMenu.Items.Add(simulationSubmenue)|>ignore
        mainMenu

    objectSpeedTrackBar.Scroll.Add(fun args -> changeObjectSpeed objectSpeedTrackBar.Value)

    /// <summary>    
    /// Init  Menue, Key-Function, Scenario
    /// </summary> 
    let Init () = 
        mainWindow.MainMenuStrip <- setupMenue
        mainWindow.Controls.Add(mainWindow.MainMenuStrip) 
        addScenarioKeyMovements()
        addStandardKeyMovements(graficWindow)   
        addStandardMouseMovements(graficWindow)   
        OBJECT_VELOCITY <- 0.1f 
        adjustSpeedBar(OBJECT_VELOCITY)

    let Start() = 
        logInfo("Start")
        execScenario(2) 
        MySimulation.Instance.Start()
        writeToMessageWindow("Application started") 