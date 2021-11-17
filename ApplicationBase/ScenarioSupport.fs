﻿namespace ApplicationBase
//
//  ScenarioSupport.fs
//
//  Created by Martin Luga on 10.09.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open Base.LoggingSupport
open Base.MaterialsAndTextures
open Base.ModelSupport
open Geometry.GeometricModel
open log4net
open SharpDX
open System.Collections.Generic

// ----------------------------------------------------------------------------------------------------
// Ein Scenario stellt eine graphische Ausgangssituation her
// Die anzeigbaren Objekte werden erstellt und der jeweilige GraficController damit versorgt
// Realisiert ist ein Scenario aals einr Function
// Hier sind Functions zum Erstellen, Finden und Ausführen vorhanden
// ----------------------------------------------------------------------------------------------------
module ScenarioSupport =

    let logger = LogManager.GetLogger("Scenario")
    let logDebug = Debug(logger)

    let mutable iActiveScenario = -1

    let mutable scenarios:Dictionary<int,(unit->unit)> = new Dictionary<int,(unit->unit)>()
    let mutable scenarioNames:Dictionary<string, int> = Dictionary<string, int>()

    let AddScenario(idx, name,  scenario:(unit->unit)) =
        scenarios.Add(idx, scenario)  
        scenarioNames.Add(name, idx)  |> ignore

    let scenarioNamed(name) =
        let success  = scenarioNames.TryGetValue(name, &iActiveScenario)
        scenarios.Item(iActiveScenario) 

    let GetScenariosNames() =  
        scenarioNames.Keys |>Seq.toList

    let activeScenario() =
        scenarios.Item(iActiveScenario)

    let execActiveScenario () =
        if iActiveScenario >= 0 then 
            scenarios.Item(iActiveScenario) ()

    let execScenarioNamed (name:string) =
        scenarioNamed(name)()        
    
    let execScenario (nr:int) =
        iActiveScenario <- nr
        execActiveScenario()

    let execNextScenario () =
        iActiveScenario <- iActiveScenario + 1
        if iActiveScenario >= scenarios.Count then 
            iActiveScenario <- 0
        scenarios.[iActiveScenario]()

    let execPreviousScenario() =
        iActiveScenario <- iActiveScenario - 1
        if iActiveScenario < 0 then 
            iActiveScenario <- scenarios.Count - 1
        scenarios.[iActiveScenario]()

    let startScenario(nr:int) =
        execScenario(nr)

    let startNextScenario() =
        execNextScenario()

    let startActiveScenario() =      
        execActiveScenario() 

// ----------------------------------------------------------------------------------------------------
//  Einige grafische Definitionen, die in Scenarios benötigt werden
// ----------------------------------------------------------------------------------------------------
module TestScenariosCommon = 

    let WITH_AXES = true
    let WITHOUT_AXES = false

    let WITH_GROUND = true
    let WITHOUT_GROUND = false
    
    let downDirection  = Vector3.UnitY * -1.0f
    let backDirection  = Vector3.UnitZ *  1.0f
    let rightDirection = Vector3.UnitX *  1.0f
    let leftDirection  = Vector3.UnitX * -1.0f
    
    let axFrom = -50.0f
    let axTo = 50.0f

    let xAxis(from, too) =
        new Part(
            name = "XAxis",
            shape =
                Linie(
                    name = "XAxis",
                    von = Vector3(from, 0.0f, 0.0f),
                    bis = Vector3(too, 0.0f, 0.0f),
                    color = Color.White
                ),
            material = MAT_WHITE
        )

    let yAxis(from, too) =
        new Part(
            name = "YAxis",
            shape =
                Linie(
                    name = "YAxis",
                    von = Vector3(0.0f, from, 0.0f),
                    bis = Vector3(0.0f, too, 0.0f),
                    color = Color.White
                ),
            material = MAT_WHITE
        )

    let zAxis(from, too) =
        new Part(
            name = "ZAxis",
            shape =
                Linie(
                    name = "ZAxis",
                    von = Vector3(0.0f, 0.0f, from),
                    bis = Vector3(0.0f, 0.0f, too),
                    color = Color.White
                ),
            material = MAT_WHITE
        )

    let MATERIAL(name, color:Color) =  
            new Material( 
                name=name,
                ambient=Color4(0.2f),
                diffuse=Color4.White,
                specular=Color4.White,
                specularPower=20.0f,
                emissive=color.ToColor4()
            )

    let TEXTURE(name:string, texturName:string) =
        let textureName = (texturName.Split('.')).[0]
        new Texture (
            name=textureName,
            fileName=texturName,
            pathName=""
        )