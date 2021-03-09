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
open ApplicationBase.WindowControl
open ApplicationBase.ShaderConfiguration 
open Shader.ShaderSupport 

open Simulation.SimulationObject
open Simulation.SimulationSystem

/// <summary>
// Einstellungen für die Simulation
/// Begrenzungen des Object space
/// DirectX Einstellungen
/// </summary>
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

    /// <summary>
    //  GraphicSystem initialisieren
    /// </summary>
    let Configure () =  

        logger.Info("Application.Configure")

        Moveable.RandomInterval <- 8L 
        Simulateable.LookAroundInterval <- 150L  

        clock.Start()

        // Simulation System
        MySimulation.CreateInstance([pipelineConfigBasic; pipelineConfigTesselateQuad; pipelineConfigTesselateTri ])
        MySimulation.Instance.Configure(ShaderClass.PhongPSType, RasterType.Solid, BlendType.Opaque)
        MySimulation.Instance.LoadTextureFiles("EcoGrafics", "ExampleApp", "textures") 
        MySimulation.Instance.TessellationFactor <- 1.0f