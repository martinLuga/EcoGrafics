namespace MotionTests
//
//  TestScenariosMotion.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open SharpDX

open Base.Logging

open ApplicationBase.DisplayableObject
open ApplicationBase.MoveableObject
open ApplicationBase.WindowControl
open ApplicationBase.WindowLayout

open Geometry.GeometricModel
open Geometry.ObjectConvenience
open Geometry.GeometryUtils

open Simulation.ScenarioSupport
open Simulation.WeltModul
open Simulation.WeltObjects
open Simulation.TestScenariosCommon
open Simulation.SimulationSystem

open Control

// ----------------------------------------------------------------------------------------------------
// TestsScenarios für die Bewegung
// 1. Objekte prallen frontal von der Wand ab
// 2. Zwei Objekte treffen aufeinander 
// 2. Zwei Objekte prallen im Winkel von der Wand ab
// ----------------------------------------------------------------------------------------------------
module Scenarios =

    let printWeltDaten(weltDaten:WeltDaten) =
        let points = toPoints(weltDaten.ursprung, weltDaten.laenge, weltDaten.malX, weltDaten.malY, weltDaten.malZ)
        let text = "Welt:" + points.ToString()
        writeToMessageWindow(text)  

    let printScenario(scenarioName) =
        logInfo("Start Scenario: " + scenarioName) 
        mainWindow.Text <- "Scenario:" + scenarioName

    let hilited (displayable:Displayable) =
        displayable.createHilite()  

    type Texture = Geometry.GeometricModel.Texture

    let logger = LogManager.GetLogger("Scenarios.Motion")
    let logInfo  = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    // Kollisionstest mit unbeweglichem Objekt
    // Kugeln, die sich zwischen zwei Wänden bewegen
    // Kugeln bewegen sich zwischen den beiden Wänden hin und her
    // ----------------------------------------------------------------------------------------------------
    let scenarioCollisionMitWand() =  
        printScenario("CollisionMitWand")
        let weltDaten = new WeltDaten(Vector3(-20.0f, 0.0f, -20.0f), 10.0f, 4, 2, 4)        
        printWeltDaten(weltDaten)

        MySimulation.Instance.InitializeForWorldData(weltDaten)
        MySimulation.Instance.SetCameraPos(Vector3(-5.0f, 20.0f, -50.0f))
        MySimulation.Instance.SetLightPos (new Vector3( 25.0f,  -25.0f,  10.0f))    

        // ----------------------------------------------------------------------------------------------------
        // Zwei parallele vertikale Wände
        // ----------------------------------------------------------------------------------------------------
        let wallLinks = 
            new Landscape(
             name="wallLinks",
             geometry=new Quader(name="Wall", laenge=2.0f, hoehe=8.0f, breite=4.0f, color=Color.Transparent), 
             surface=SURFACE_WALL(Color.DarkRed),
             position=Vector3(-20.0f, 0.0f, -2.0f),
             color=Color.IndianRed
             ) 

        let wallRechts = 
            new Landscape(
             name="wallRechts",
             geometry=new Quader(name="Wall", laenge=2.0f, hoehe=8.0f, breite=4.0f, color=Color.Transparent),            
             surface=SURFACE_WALL(Color.DarkRed),
             position=Vector3(-4.0f, 0.0f, -2.0f),
             color=Color.IndianRed
             )  

        let sphere1 = 
            new Moveable( 
                name="sphere1",
                geometry=new Kugel("Sphere1", 1.0f, Color.Transparent),  
                surface=SURFACE_KUGEL(Color.DarkRed),
                position=Vector3(-15.0f, 4.0f, 0.0f),
                direction=Vector3.Right,                   
                velocity=OBJECT_VELOCITY,
                color=Color.DarkRed,
                moveRandom=false
            )

        // ----------------------------------------------------------------------------------------------------
        // Zwei parallele horizontale Wände
        // ----------------------------------------------------------------------------------------------------
        let wallOben = 
            new Landscape(
                name="wallOben",
                geometry=new Quader(name="Wall2", laenge=8.0f, hoehe=2.0f, breite=4.0f, color=Color.Transparent), 
                surface=SURFACE_WALL(Color.Orange),
                position=Vector3(0.0f,  16.0f, -2.0f),
                color=Color.Orange
            )      

        let wallUnten = 
            new Landscape(
                name="wallUnten",
                geometry=new Quader(name="Wall2", laenge=8.0f, hoehe=2.0f, breite=4.0f, color=Color.Transparent),
                surface=SURFACE_WALL(Color.Orange),
                position=Vector3(0.0f, 0.0f, -2.0f),
                color=Color.Orange
            )           
        
        let sphere2 = 
            new Moveable( 
             name="sphere2",
             geometry=new Kugel("Sphere2", 1.0f, Color.Transparent),  
             surface=SURFACE_KUGEL(Color.Orange),
             position=Vector3(4.0f, 4.0f, 0.0f),
             direction=Vector3.UnitY,                    
             velocity=OBJECT_VELOCITY,
             color=Color.DarkOrange,
             moveRandom=false
             )

        // ----------------------------------------------------------------------------------------------------        
        // Zwei parallele horizontale Wände Vorne / hinten
        // ----------------------------------------------------------------------------------------------------
        let wallVorne = 
            new Landscape(
                name="wallVorne",
                geometry=new Quader(name="Wall3", laenge=4.0f, hoehe=8.0f, breite=2.0f, color=Color.Transparent),
                surface=SURFACE_WALL(Color.DarkGreen),
                position=Vector3(12.0f, 0.0f, -8.0f),
                color=Color.LightGreen
            ) 

        let wallHinten = 
            new Landscape(
                name="wallHinten",
                geometry=new Quader(name="Wall3", laenge=4.0f, hoehe=8.0f, breite=2.0f, color=Color.Transparent), 
                surface=SURFACE_WALL(Color.DarkGreen),
                position=Vector3(12.0f, 0.0f, 8.0f),
                color=Color.DarkGreen
            )        

        let sphere3 = 
            new Moveable( 
                name="sphere3",
                geometry=new Kugel("Sphere3", 1.0f, Color.Transparent),  
                surface=SURFACE_KUGEL(Color.DarkSeaGreen),
                position=Vector3(14.0f, 4.0f, 0.0f),
                direction= (Vector3.UnitZ),             
                velocity=OBJECT_VELOCITY,
                color=Color.DarkSeaGreen,
                moveRandom=false
            )

        logInfo(">>>>>> Wall Hinten =" + wallHinten.ToString())

        displayables <- [wallRechts:>Displayable; wallLinks:>Displayable; wallVorne:>Displayable; wallHinten:>Displayable; wallOben:>Displayable; wallUnten:>Displayable;sphere1:>Displayable;sphere2:>Displayable;sphere3:>Displayable]
        MySimulation.Instance.AddObjects(displayables)        
        MySimulation.Instance.UnhideUmgebungen()

    /// <summary>
    /// Einfallswinkel = Ausfallswinkel
    /// Zwei Kugeln gegen eine Wand
    /// Sphere - Quader
    /// </summary>
    let scenarioEinfallswinkelGleichAusfallswinkel() = 
        printScenario("EinfallswinkelGleichAusfallswinkel")

        let weltDaten = new WeltDaten(Vector3(-20.0f, -10.0f, -20.0f), 10.0f, 4, 3, 4)        
        printWeltDaten(weltDaten)

        MySimulation.Instance.InitializeForWorldData(weltDaten)
        MySimulation.Instance.SetCameraPos(Vector3(0.0f, 15.0f, -70.0f))
        MySimulation.Instance.SetLightPos (new Vector3(  25.0f,  -25.0f,  10.0f))

        // ----------------------------------------------------------------------------------------------------
        // Zwei Kugeln und dazwischen eine Wand  
        // ---------------------------------------------------------------------------------------------------- 
        MySimulation.Instance.ClearObjects()  
        let sphere1 = 
            new Moveable( 
             name="sphere1",
             geometry=new Kugel("Sphere1", 1.0f, Color.Transparent),
             surface=SURFACE_KUGEL(Color.DarkCyan),
             color=Color.DarkCyan,
             position=Vector3(-5.0f, 3.0f, 0.0f),
             direction=new Vector3(1.0f,1.0f,0.0f), 
             velocity=OBJECT_VELOCITY,
             moveRandom=false
             )

        let sphere2 = 
            new Moveable( 
                name="sphere2",
                geometry=new Kugel("Sphere2", 1.0f, Color.Transparent), 
                surface=SURFACE_KUGEL(Color.DarkRed),
                position=Vector3(5.0f, 3.0f, 0.0f),
                direction=new Vector3(-1.0f,1.0f,0.0f), 
                velocity=OBJECT_VELOCITY,
                color=Color.DarkRed,
                moveRandom=false
             )

        let wall = 
            new Landscape(
                name="Wall",
                geometry=new Quader("Wall", 1.0f, Welt.Instance.YMAX - Welt.Instance.YMIN, 20.0f, Color.Transparent),        
                surface=SURFACE_WALL(Color.Orange),
                position=Vector3(0.0f, Welt.Instance.GroundHeight+0.5f, -10.0f),
                color=Color.Transparent
             ) 
             
        displayables <- [wall:>Displayable; sphere1:>Displayable; sphere2:>Displayable]
        MySimulation.Instance.AddObjects(displayables)

    /// <summary>
    /// Kollision Kugel an einem Korpus
    /// Zwei Objekte. die sich aufeinander zu bewegen und reflektiert werden
    /// </summary>
    let scenarioCollisionKugelUndKorpus() = 
        printScenario("KollisionKugelUndKorpus")

        let weltDaten = new WeltDaten(Vector3(-20.0f, -10.0f, -20.0f), 10.0f, 4, 2, 4)        
        printWeltDaten(weltDaten)

        MySimulation.Instance.InitializeForWorldData(weltDaten)
        MySimulation.Instance.SetCameraPos(Vector3(-5.0f, 15.0f, -50.0f))
        MySimulation.Instance.SetLightPos (new Vector3( 15.0f,  -15.0f,  10.0f)) 

        // ----------------------------------------------------------------------------------------------------
        // Eine Kugel und ein Korpus
        // ---------------------------------------------------------------------------------------------------- 
        let radius = 1.0f
        let sphere1 = 
           new Moveable( 
               name="sphere1",
               geometry=new Kugel("Sphere1", radius, Color.Transparent),   
               surface=SURFACE_KUGEL(Color.OrangeRed),
               color=Color.Yellow, 
               position=Vector3(-8.0f, Welt.Instance.GroundHeight + radius + 0.1f, 0.0f),
               direction=Vector3(1.0f, 0.0f, 1.0f ),   
               velocity=OBJECT_VELOCITY,     
               moveRandom=false
           ) 

        let plate1 = 
            new Landscape(
                name="plate1",
                geometry=CORPUS(CONTOUR_PLATE),
                surface=new Surface(MAT_DARKSLATEGRAY),
                color=Color.Black,
                position=Vector3(2.0f, Welt.Instance.GroundHeight+0.5f, 2.0f)
            ) 

        displayables <- [sphere1:>Displayable; plate1:>Displayable]
        MySimulation.Instance.AddObjects(displayables)

    /// <summary>
    /// Initialize
    /// </summary>
    let Initialize() =
        AddScenario(0, "CollisionMitWand", scenarioCollisionMitWand)
        AddScenario(1, "EinfallswinkelGleichAusfallswinkel", scenarioEinfallswinkelGleichAusfallswinkel)
        AddScenario(2, "CollisionKugelUndKorpus", scenarioCollisionKugelUndKorpus)