namespace MotionTests
//
//  Program.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.

open Base.Logging

open ApplicationBase.GraficSystem  

open ApplicationBase.WindowControl

// ----------------------------------------------------------------------------------------------------
// SimulationApp
// ----------------------------------------------------------------------------------------------------
// Steuerung
// ----------------------------------------------------------------------------------------------------
module Program =

    configureLoggingInMap "EcoGrafics" "MotionTestsApp" "resource" "log4net.config"

    [<EntryPoint>]
    
    let main argv = 
        WindowLayout.Setup("ANT SIMULATION")
        Configuration.Configure()
        Control.Init() 
        displayWindows()
        Control.Start()   
        0     
