namespace ApplicationBase
//
//  ScenarioSupport.fs
//
//  Created by Martin Luga on 10.09.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic

open SharpDX

open Geometry.GeometricModel

/// <summary>
/// Scenario convenience funcctions
/// </summary>
module ScenarioSupport =

    let mutable iActiveScenario = 0

    let mutable scenarios:Map<int,(unit->unit)> = Map[]
    let mutable scenariosForName = new Dictionary<string, unit->unit>()

    let AddScenario(idx, name,  scenario:(unit->unit)) =
        scenarios <- scenarios.Add(idx, scenario)  
        scenariosForName.Add(name, scenario) 

    let scenarioNamed(name) =
        scenariosForName.Item(name) 

    let scenariosNames() =  
        scenariosForName.Keys

    let activeScenario() =
        scenarios.Item(iActiveScenario)

    let execActiveScenario () =
        scenarios.Item(iActiveScenario) ()

    let execScenarioNamed (name:string) =
        scenariosForName.Item(name)()        
    
    let execScenario (nr:int) =
        iActiveScenario <- nr
        execActiveScenario()

    let execNextScenario () =
        iActiveScenario <- iActiveScenario + 1
        if iActiveScenario >= scenarios.Count then 
            iActiveScenario <- 0
        scenarios.[iActiveScenario]()

    let startScenario(nr:int) =
        execScenario(nr)

    let startNextScenario() =
        execNextScenario()

/// <summary>
/// Common declares for scenarios
/// </summary>
module TestScenariosCommon =
    
    let downDirection  = Vector3.UnitY * -1.0f
    let backDirection  = Vector3.UnitZ *  1.0f
    let rightDirection = Vector3.UnitX *  1.0f
    let leftDirection  = Vector3.UnitX * -1.0f
    
    // ----------------------------------------------------------------------------------------------------
    // GEOMETRY
    // ----------------------------------------------------------------------------------------------------
    let CORPUS (aContour) = 
        Corpus(
            name="CORPUS",
            center= Vector3(1.5f, 0.0f, -3.0f),
            contour=aContour,
            height=5.0f,
            colorBottom=Color.White,
            colorTop=Color.White,
            colorSide=Color.White
        )        
    let MINI_CUBE = 
        Würfel(
            "SMALLCUBE", 
            0.5f,
            Color.Red,          // Front
            Color.Green,        // Right
            Color.Blue,         // Back  
            Color.Cyan,         // Left
            Color.Yellow,       // Top        
            Color.Orange        // Bottom            
        )
    let BIG_CUBE = 
        Würfel(
            "BIGCUBE", 
            3.0f,
            Color.Red,          // Front
            Color.Green,        // Right
            Color.Blue,         // Back  
            Color.Cyan,         // Left
            Color.Yellow,       // Top        
            Color.Orange        // Bottom            
        ) 
    let SMALL_CUBE = 
        Würfel(
            "SMALLCUBE", 
            2.0f,
            Color.Red,          // Front
            Color.Green,        // Right
            Color.Blue,         // Back  
            Color.Cyan,         // Left
            Color.Yellow,       // Top        
            Color.Orange        // Bottom            
        ) 
        
    // Im Uhrteigersinn unten
    let CONTOUR_PLATE =
        [|Vector3( 0.0f, 0.0f, -5.0f);
            Vector3( 1.0f, 0.0f, -5.0f);
            Vector3( 2.0f, 0.0f, -5.0f);
            Vector3( 3.0f, 0.0f, -5.0f);

            Vector3( 4.0f, 0.0f, -4.0f);
            Vector3( 4.0f, 0.0f, -3.0f);
            Vector3( 4.0f, 0.0f, -2.0f);
            Vector3( 3.0f, 0.0f, -1.0f);

            Vector3( 2.0f, 0.0f, -1.0f);
            Vector3( 1.0f, 0.0f, -1.0f);
            Vector3( 0.0f, 0.0f, -1.0f);
            Vector3(-1.0f, 0.0f, -2.0f);

            Vector3(-1.0f, 0.0f, -3.0f);
            Vector3(-1.0f, 0.0f, -4.0f);
            Vector3( 0.0f, 0.0f, -5.0f) 
            |] 