namespace ExampleApp
//
//  Control.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Windows.Forms

open log4net

open ApplicationBase.WindowControl
open ApplicationBase.WindowLayout
open ApplicationBase.GraficSystem 
open ApplicationBase.ScenarioSupport
open ApplicationBase.DisplayableObject

open DirectX.Assets

open GPUModel.MyGraphicWindow

open MoleculeDrawing.MoleculeDraw

open Shader.ShaderSupport

open Configuration

// ----------------------------------------------------------------------------------------------------
// Simple Steuerung der Elemente
// ----------------------------------------------------------------------------------------------------

type ZoomDir = | Nearer  | Farther 

module Control = 

    let logger = LogManager.GetLogger("Control")

    // ----------------------------------------------------------------------------------------------------    
    // Inits GUI, GraficSystem
    // ---------------------------------------------------------------------------------------------------- 
    let Init() =   
        InitTesselationFactor(4.0f)   
        addStandardKeyMovements(graficWindow)
        addScenarioKeyMovements(graficWindow)
        addStandardMouseMovements(graficWindow)
        mainWindow.Activated.Add(fun _ -> execScenarioNamed("Korpus"))

    let Start() = 
        logger.Info("\nStart")
        MySystem.Instance.Start()
        writeToMessageWindow("Application started")