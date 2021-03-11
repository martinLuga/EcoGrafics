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
open ApplicationBase.ScenarioSupport 

open Base.Logging

open Simulation.SimulationSystem

open WindowLayout
open WindowControl

/// <summary>
/// Steuerung der Anwendung
/// </summary>
module Control = 
    let logger = LogManager.GetLogger("app.Control")
    let logInfo  = Info(logger)
    let logDebug = Debug(logger)

    let TEST_SPEED = 0.3f
    let SPEED_FACTOR = 10.0f
       
    /// <summary>    
    /// Control SpeedBar
    /// </summary> 
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

    objectSpeedTrackBar.Scroll.Add(fun args -> changeObjectSpeed objectSpeedTrackBar.Value)

    /// <summary>    
    /// Init  Menue, Key-Function, Scenario
    /// </summary> 
    let Init () = 
        addScenarioKeyMovements()
        addStandardKeyMovements(graficWindow)   
        addStandardMouseMovements(graficWindow)   
        OBJECT_VELOCITY <- 0.1f 
        adjustSpeedBar(OBJECT_VELOCITY)

    /// <summary>    
    /// Start the application
    /// </summary> 
    let Start() = 
        logInfo("Start")
        clock.Start()
        iActiveScenario <- 1
        MySimulation.Instance.Start()
        writeToMessageWindow("Application started") 