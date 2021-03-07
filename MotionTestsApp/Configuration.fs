namespace MotionTests
//
//  Configuration.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System

open log4net

open SharpDX

open ApplicationBase.MoveableObject
open ApplicationBase.GraficSystem
open ApplicationBase.WindowControl
open ApplicationBase.ShaderConfiguration 

open Simulation.SimulationObject
open Simulation.SimulationSystem

// ----------------------------------------------------------------------------------------------------
// Einstellungen für die Simulation
// Begrenzungen des Object space
// DirectX Einstellungen
// ----------------------------------------------------------------------------------------------------
module Configuration =

    let logger = LogManager.GetLogger("Configuration")

    let simulationLightDir = new Vector3(1.0f, -1.0f, -1.0f) 

    let collideHill2Position    = Vector3( 12.0f, 15.0f,  -4.0f) 
    let collideGroundPosition   = Vector3(-10.0f, 10.0f, -12.0f) 
    let collideFood1Position    = Vector3(-12.0f, 12.0f,  -5.0f) 
    let collideFood2Position    = Vector3(2.0f, 12.0f,  -5.0f)
    let collideFood1FromLeft    = Vector3(-20.0f,  2.0f,  -5.0f)
    let dropInPosition          = Vector3(  0.0f, 10.0f,   0.0f)

    let downDirection       = Vector3.UnitY * -1.0f
    let backDirection       = Vector3.UnitZ *  1.0f
    let forwardDirection    = Vector3.UnitZ * -1.0f
    let rightDirection      = Vector3.UnitX *  1.0f
    let leftDirection       = Vector3.UnitX * -1.0f

    // ----------------------------------------------------------------------------------------------------
    //  Richtungsänderungen bei zufälliger Bewegung
    // ----------------------------------------------------------------------------------------------------
    let updateDirectionRandom(random:Random) (dir:Vector3) =
        let x = (random.Next(-10,10) |> float32 )  / 100.0f
        let y = (random.Next(-10,10) |> float32  ) / 100.0f
        let z = (random.Next(-10,10) |> float32  ) / 100.0f  
        let dir = new Vector3(x, y, z)
        dir.Normalize()
        dir          

    let updateDirectionRandom2(random:Random) (dir:Vector3) =
        let deviation = 5  
        let x = dir.X + (random.Next(-deviation,deviation) |> float32 )  / 10.0f
        let y = dir.Y + (random.Next(-deviation,deviation) |> float32  ) / 10.0f
        let z = dir.Z + (random.Next(-deviation,deviation) |> float32  ) / 10.0f  
        let result = new Vector3(x, y, z)
        result.Normalize()
        result 

    let updateDirectionRandomOnGround(random:Random) (dir:Vector3) =
        let x = (random.Next(-10,10) |> float32 )  / 100.0f
        let y = dir.Y
        let z = (random.Next(-10,10) |> float32  ) / 100.0f  
        let dir = new Vector3(x, y, z)
        dir.Normalize()
        logger.Debug("NEW DIR=" + dir.ToString())
        dir  

    // ----------------------------------------------------------------------------------------------------
    // GraphicSystem initialisieren
    // ----------------------------------------------------------------------------------------------------
    let Configure () =  

        logger.Info("Application.Configure")

        Moveable.RandomInterval <- 8L 
        Moveable.RandomDirectionFunc <- updateDirectionRandomOnGround
        Simulateable.LookAroundInterval <- 150L  

        clock.Start()

        // Simulation System
        MySimulation.CreateInstance([pipelineConfigBasic; pipelineConfigTesselateQuad; pipelineConfigTesselateTri ])
        MySimulation.Instance.initialize()
        MySimulation.Instance.LoadTextureFiles("EcoGrafics", "ExampleApp", "textures")   

        tessellationFactor <- 1.0f