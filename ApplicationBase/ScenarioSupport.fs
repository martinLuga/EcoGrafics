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
open Base.ObjectBase
open Geometry.GeometricModel
open log4net
open SharpDX
open System.Collections.Generic
open GraficBase.GraficController

// ----------------------------------------------------------------------------------------------------
// Ein Scenario stellt eine graphische Ausgangssituation her
// Die anzeigbaren Objekte werden erstellt und der jeweilige GraficController damit versorgt
// Realisiert ist ein Scenario aals einr Function
// Hier sind Functions zum Erstellen, Finden und Ausführen vorhanden
// ----------------------------------------------------------------------------------------------------
module ScenarioSupport =

    let logger = LogManager.GetLogger("Scenario")
    let logDebug = Debug(logger)

    let mutable iActiveScenario  = -1
    let mutable iScenarioObjects = 2

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
//  Tesselated objects test
// ----------------------------------------------------------------------------------------------------   
module GroundPlaneSurfaces = 

    let PART_FRONT =
        new Part(
            name = "FRONT",
            shape =
                Fläche.InXYPlane(
                    name = "FRONT",
                    ursprung = Vector3.Zero,
                    seitenlaenge = 10.0f,
                    normal = Vector3.BackwardLH,
                    color = Color.Transparent
                ),
            material = MAT_DARKSLATEGRAY,
            visibility = Visibility.Transparent
        )

    let PART_GROUND (origin, extent) =
        new Part(
            name = "GROUND",
            shape =
                  Fläche.InXZPlane(
                      name = "GROUND",
                      ursprung = origin,
                      seitenlaenge = extent,
                      normal = Vector3.Up,
                      color = Color.Transparent
                  ) ,
            material = MAT_GROUND,
            texture = TEXT_GROUND
        )

    let PART_RIGHT =
        new Part(
            name = "RIGHT",
            shape =
                  Fläche.InYZPlane(
                      name = "RIGHT",
                      ursprung = Vector3.Zero,
                      seitenlaenge = 10.0f,
                      normal = Vector3.Right,
                      color = Color.Transparent
                  ) ,
            material = MAT_BLUE
        ) 

// ----------------------------------------------------------------------------------------------------
//  Einige grafische Definitionen, die in Scenarios benötigt werden
// ----------------------------------------------------------------------------------------------------
module TestScenariosCommon = 

    open GroundPlaneSurfaces

    let WITH_AXES = true
    let WITHOUT_AXES = false

    let WITH_GROUND = true
    let WITHOUT_GROUND = false

    let WORLDORIGIN = Vector3.Zero
    let GROUND_LEVEL = WORLDORIGIN.Y
    let WORLD_HALF_LENGTH = 100.0f
    
    let downDirection  = Vector3.UnitY * -1.0f
    let backDirection  = Vector3.UnitZ *  1.0f
    let rightDirection = Vector3.UnitX *  1.0f
    let leftDirection  = Vector3.UnitX * -1.0f
    
    // ----------------------------------------------------------------------------------------------------
    //  AXES
    // ----------------------------------------------------------------------------------------------------
    let xAxis(from, too) =
        new Part(
            name = "XAxis",
            shape =
                Linie(
                    name = "XAxis",
                    von = Vector3(from, 0.5f, 0.0f),
                    bis = Vector3(too, 0.5f, 0.0f),
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

    let createAXES(from, too) =
        new BaseObject(
            name = "Achsen",
            display = 
                new Display(
                    parts = [
                        xAxis(from, too); yAxis(from, too); zAxis(from, too)
                    ]
                ),
            position = Vector3.Zero
        )

    let DEFAULT_AXES(extent) =
        createAXES(-extent/2.0f, extent/2.0f) 

    // ----------------------------------------------------------------------------------------------------
    //  SHAPE
    // ----------------------------------------------------------------------------------------------------
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
    
    // ----------------------------------------------------------------------------------------------------
    //  GROUND
    // ----------------------------------------------------------------------------------------------------
    let GROUND_HEIGHT = 0.0f

    let NO_GROUND(center:Vector3, extent) =
        let result:BaseObject = null
        result

    let createGround(center:Vector3, extent) =
        let origin = 
            Vector3(
                center.X - extent/2.0f,
                center.Y - GROUND_HEIGHT,
                center.Z - extent/2.0f
            )
        new BaseObject(
            name = "GROUND",
            display =  
                new Display(
                    parts = [PART_GROUND (Vector3.Zero, extent)]
                ),
            position = origin
        )

    let DEFAULT_GROUND(center:Vector3, halfLenth) =
        let origin = 
            Vector3(
                center.X - halfLenth/2.0f,
                center.Y - GROUND_HEIGHT,
                center.Z - halfLenth/2.0f
            )
        new BaseObject(
            name = "GROUND",
            display =  
                new Display(
                    parts = [
                        new Part(  
                            name = "GROUND",
                            shape=Quader( 
                                name="GROUND", 
                                laenge=halfLenth,
                                breite=halfLenth,
                                hoehe= GROUND_HEIGHT,
                                color=Color.Black
                            ),
                            material=MAT_DARKSLATEGRAY,
                            texture=TEXT_EMPTY
                        )
                    ]
                ),
            position = origin
        )