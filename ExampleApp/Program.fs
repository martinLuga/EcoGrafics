namespace ExampleApp
//
//  Program.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved. 

open log4net

open Base.Logging
 
open ApplicationBase.WindowControl

/// <summary>
// /BasicApp
/// Test der Basisfunktionalitäten
/// Darstellung von Kugel Quader Zylinder
/// Ausführen von Bewegungen (Translation, Rotation)
/// </summary>
module Program =

    configureLoggingInMap "EcoGrafics" "ExampleApp" "resource" "log4net.config"

    let logger = LogManager.GetLogger("ExampleApp")

    [<EntryPoint>]
    
    let main argv = 
        logger.Info("Start Application")
        WindowLayout.Setup("DirectX Example Application")
        Configuration.ConfigureSystem()
        Configuration.ConfigureMenue()
        Scenario.CreateScenarios()
        Control.Init() 
        displayWindows()
        Control.Start()        
        0