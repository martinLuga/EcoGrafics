namespace MotionTests
//
//  Control.fs
//
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Windows.Forms
open System.Threading
open System.Threading.Tasks

open SharpDX

open log4net

open ApplicationBase.WindowControl
open ApplicationBase.WindowLayout
open ApplicationBase.MoveableObject
open ApplicationBase.GraficSystem  

open Base.Logging

open Simulation
open Simulation.SimulationSystem
open Simulation.WeltModul
open Simulation.ScenarioSupport

open WindowLayout

// ----------------------------------------------------------------------------------------------------
// Steuerung
// Shader-Auswahl
// Move Rotate
// ----------------------------------------------------------------------------------------------------
module Control = 

    let logger = LogManager.GetLogger("app.Control")
    let logInfo  = Info(logger)
    let logDebug = Debug(logger)
    let mutable cancelAll = new CancellationTokenSource()

    let mutable moveFactor = 0.0003f  
    let mutable moveUp = 0.0f   
    let mutable moveLeft = 0.0f  
    let mutable antNumber = 0
    let mutable predNumber = 0

    let TEST_SPEED = 0.3f
    let SPEED_FACTOR = 10.0f
        
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
        restartScenario() 

    let hideUmgebung() =
        Welt.Instance.HideUmgebungen()
        MySystem.Instance.InstallObjects() 

    let addScenarioKeyMovements() =
        graficWindow.KeyDown.Add(fun e -> if e.KeyCode = Keys.N  then execNextScenario())  

        graficWindow.KeyDown.Add(fun e -> if e.KeyCode = Keys.D0 then execScenario(0)) 
        graficWindow.KeyDown.Add(fun e -> if e.KeyCode = Keys.D1 then execScenario(1))  
        graficWindow.KeyDown.Add(fun e -> if e.KeyCode = Keys.D2 then execScenario(2)) 

        graficWindow.KeyDown.Add(fun e -> if e.KeyCode = Keys.D3 then execScenario(3))    
        graficWindow.KeyDown.Add(fun e -> if e.KeyCode = Keys.D4 then execScenario(4)) 
        graficWindow.KeyDown.Add(fun e -> if e.KeyCode = Keys.D5 then execScenario(5))

        graficWindow.KeyDown.Add(fun e -> if e.KeyCode = Keys.D6 then execScenario(6))  

    // ----------------------------------------------------------------------------------------------------    
    // Menues
    // ----------------------------------------------------------------------------------------------------  
    // Menue Simulation
    let simulationSubmenue =  
        let simulationMenuItem = new ToolStripMenuItem("&Simulation")
        let stopWorkflowsMenuItem = new ToolStripMenuItem("&Stop Workflows")
        let startWorkflowsMenuItem = new ToolStripMenuItem("&Start Workflows")
        let scenarioMenuItem = new ToolStripMenuItem("&Next Scenario")
        let restartMenuItem = new ToolStripMenuItem("&Restart Scenario")
        let toggleWorkflowsMenuItem = new ToolStripMenuItem("&Toggle Workflows")
        simulationMenuItem.DropDownItems.Add(scenarioMenuItem)|>ignore
        simulationMenuItem.DropDownItems.Add(stopWorkflowsMenuItem)|>ignore
        simulationMenuItem.DropDownItems.Add(startWorkflowsMenuItem)|>ignore
        simulationMenuItem.DropDownItems.Add(restartMenuItem)|>ignore
        simulationMenuItem.DropDownItems.Add(toggleWorkflowsMenuItem)|>ignore
        stopWorkflowsMenuItem.Click.Add(fun _ -> MySimulation.Instance.stopWorkflows(); writeToMessageWindow("Workflows stopped"))
        startWorkflowsMenuItem.Click.Add(fun _ -> MySimulation.Instance.startWorkflows (); writeToMessageWindow("Workflows started"))
        scenarioMenuItem.Click.Add(fun _ -> execNextScenario())
        restartMenuItem.Click.Add(fun _ ->  restartScenario())
        toggleWorkflowsMenuItem.Click.Add(fun _ -> MySimulation.Instance.toggleWorkflows ())
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

    // ----------------------------------------------------------------------------------------------------    
    // Init  Menue, Key-Function, Scenario
    // ---------------------------------------------------------------------------------------------------- 
    let Init () =
        mainWindow.MainMenuStrip <- setupMenue
        mainWindow.Controls.Add(mainWindow.MainMenuStrip) 
        addScenarioKeyMovements()
        addStandardKeyMovements(graficWindow)   
        addStandardMouseMovements(graficWindow)   
        OBJECT_VELOCITY <- 0.3f 
        adjustSpeedBar(OBJECT_VELOCITY)

    let Start() = 
        logInfo("Start")
        startScenario(0)
        MySimulation.Instance.Start()
        writeToMessageWindow("Application started") 