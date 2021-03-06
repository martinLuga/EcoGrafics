namespace MotionTests
//
//  Program.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.

open Base.Logging

open ApplicationBase.WindowControl

/// <summary>
/// SimulationApp - Hauptprogramm
/// </summary>
module Program =

    configureLoggingInMap "EcoGrafics" "MotionTestsApp" "resource" "log4net.config"

    [<EntryPoint>]
    
    let main argv = 
        WindowLayout.Setup("Motion Tests")
        Configuration.ConfigureSystem()
        Configuration.ConfigureMenue()
        Scenario.CreateScenarios()
        Control.Init() 
        displayWindows()
        Control.Start()   
        0     
