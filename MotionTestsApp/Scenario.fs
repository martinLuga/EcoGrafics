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
open ApplicationBase.WindowLayout
open ApplicationBase.WindowControl
open ApplicationBase.ScenarioSupport 
open ApplicationBase.TestScenariosCommon

open Geometry.GeometricModel
open Geometry.GeometricElements
open Geometry.ObjectConvenience
open Geometry.GeometricModel2D

open Simulation.SimulationSystem

// ----------------------------------------------------------------------------------------------------
// TestsScenarios für die Bewegung
// 1. Objekte prallen frontal von der Wand ab
// 2. Zwei Objekte treffen aufeinander 
// 2. Zwei Objekte prallen im Winkel von der Wand ab
// ----------------------------------------------------------------------------------------------------
module Scenario =

    /// <summary> 
    /// Text-Nachrichten Ausgabe
    /// </summary> 
    let writeObjectReport(objekt:Displayable) =
        writeToOutputWindow("Objekt  : "   + objekt.Name) 
        writeToOutputWindow("Geometry: "   + objekt.Geometry.ToString())
        writeToOutputWindow("Position: "   + objekt.Position.ToString())
        writeToOutputWindow("Center  : "   + objekt.Center.ToString())
        writeToOutputWindow("Bounds  : "   + objekt.Boundaries.ToString())
        newLineOutputWindow()

    let writeReportObjects(displayables:Displayable list) =
        clearOutputWindow()
        for disp in displayables do
            writeObjectReport(disp)

    let printScenario(scenarioName) =
        logInfo("Start Scenario: " + scenarioName) 
        mainWindow.Text <- "Scenario:" + scenarioName

    let hilited (displayable:Displayable) =
        displayable.createHilite()  

    type Texture = Geometry.GeometricElements.Texture

    let logger = LogManager.GetLogger("Scenarios.Motion")
    let logInfo  = Info(logger)

    let xAxis = 
        Immoveable(
            name="xAxis",
            geometry= Line(
                name="XAxis", 
                von=Vector3(-10.0f, 0.0f, 0.0f),
                bis=Vector3( 10.0f, 0.0f, 0.0f),
                color=Color.White
            ),
            surface=Surface(
                MAT_WHITE
            ),
            color=Color.White,
            position=Vector3(0.0f, 0.0f, 0.0f)
        ) 
    
    let yAxis = 
        Immoveable(
            name="yAxis",
            geometry= Line(
                name="YAxis", 
                von=Vector3(0.0f, -10.0f, 0.0f),
                bis=Vector3(0.0f,  10.0f, 0.0f),
                color=Color.White
            ),
            surface=Surface(
                MAT_WHITE
            ),
            color=Color.White,
            position=Vector3(0.0f, 0.0f, 0.0f)
        ) 
    let zAxis = 
        Immoveable(
            name="zAxis",
            geometry= Line(
                name="ZAxis", 
                von=Vector3(0.0f, 0.0f, -10.0f),
                bis=Vector3(0.0f, 0.0f,  10.0f),
                color=Color.White
            ),
            surface=Surface(
                MAT_WHITE 
            ),
            color=Color.White,
            position=Vector3(0.0f, 0.0f, 0.0f)
        ) 
    let AXES =
        [xAxis:>Displayable;yAxis:>Displayable;zAxis:>Displayable]

    // ----------------------------------------------------------------------------------------------------
    // Kollisionstest mit unbeweglichem Objekt
    // Kugeln, die sich zwischen zwei Wänden bewegen
    // Kugeln bewegen sich zwischen den beiden Wänden hin und her
    // ----------------------------------------------------------------------------------------------------
    let CollisionMitWand() =  
        printScenario("CollisionMitWand") 

        MySimulation.Instance.Reset()
        MySimulation.Instance.ConfigureWorld(Vector3(-20.0f, 0.0f, -20.0f), 10.0f, 4, 2, 4)
        MySimulation.Instance.ConfigVision(
            cameraPosition=Vector3(-5.0f,  20.0f, -50.0f),
            lightDirection=Vector3(25.0f, -25.0f,  10.0f)
        )    

        // ----------------------------------------------------------------------------------------------------
        // Zwei parallele vertikale Wände
        // ----------------------------------------------------------------------------------------------------
        let wallLinks = 
            new Immoveable(
             name="wallLinks",
             geometry=new Quader(name="Wall", laenge=2.0f, hoehe=8.0f, breite=4.0f, color=Color.Transparent), 
             surface=SURFACE_WALL(Color.DarkRed),
             position=Vector3(-20.0f, 0.0f, -2.0f),
             color=Color.IndianRed
             ) 

        let wallRechts = 
            new Immoveable(
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
            new Immoveable(
                name="wallOben",
                geometry=new Quader(name="Wall2", laenge=8.0f, hoehe=2.0f, breite=4.0f, color=Color.Transparent), 
                surface=SURFACE_WALL(Color.Orange),
                position=Vector3(0.0f,  16.0f, -2.0f),
                color=Color.Orange
            )      

        let wallUnten = 
            new Immoveable(
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
            new Immoveable(
                name="wallVorne",
                geometry=new Quader(name="Wall3", laenge=4.0f, hoehe=8.0f, breite=2.0f, color=Color.Transparent),
                surface=SURFACE_WALL(Color.DarkGreen),
                position=Vector3(12.0f, 0.0f, -8.0f),
                color=Color.LightGreen
            ) 

        let wallHinten = 
            new Immoveable(
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

        let displayables = [wallRechts:>Displayable; wallLinks:>Displayable; wallVorne:>Displayable; wallHinten:>Displayable; wallUnten:>Displayable;sphere1:>Displayable;sphere2:>Displayable;sphere3:>Displayable]
        writeReportObjects(displayables)
        MySimulation.Instance.AddObjects(displayables)

    /// <summary>
    /// Einfallswinkel = Ausfallswinkel
    /// Zwei Kugeln gegen eine Wand
    /// Sphere - Quader
    /// </summary>
    let EinfallswinkelGleichAusfallswinkel() = 
        printScenario("EinfallswinkelGleichAusfallswinkel")

        MySimulation.Instance.Reset()
        MySimulation.Instance.ConfigureWorld(Vector3(-20.0f, -10.0f, -20.0f), 10.0f, 4, 3, 4)
        MySimulation.Instance.ConfigVision(
            new Vector3( 0.0f, 15.0f, -70.0f),
            new Vector3( 25.0f,  -25.0f,  10.0f)
        )

        // ----------------------------------------------------------------------------------------------------
        // Zwei Kugeln und dazwischen eine Wand  
        // ---------------------------------------------------------------------------------------------------- 
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
            new Immoveable(
                name="Wall",
                geometry=new Quader("Wall", 1.0f, MySimulation.Instance.WeltDecke - MySimulation.Instance.GroundLevel, 20.0f, Color.Transparent),       
                surface=SURFACE_WALL(Color.Orange),
                position=Vector3(0.0f, MySimulation.Instance.GroundLevel+0.5f, -10.0f),
                color=Color.Transparent
            ) 
             
        let displayables = [wall:>Displayable; sphere1:>Displayable; sphere2:>Displayable] 
        writeReportObjects(displayables)
        MySimulation.Instance.AddObjects(displayables)

    /// <summary>
    /// Kollision Kugel an einem Korpus
    /// Zwei Objekte. die sich aufeinander zu bewegen und reflektiert werden
    /// </summary>
    let CollisionKugelUndKorpus() = 
        printScenario("KollisionKugelUndKorpus")

        MySimulation.Instance.Reset()
        MySimulation.Instance.ConfigureWorld(Vector3(-20.0f, 0.0f, -20.0f), 10.0f, 4, 2, 4)
        MySimulation.Instance.ConfigVision(
            new Vector3( -5.0f, 20.0f, -50.0f),
            new Vector3( 25.0f,  -25.0f,  10.0f)
        )   

        // ----------------------------------------------------------------------------------------------------
        // Eine Kugel und ein Korpus
        // ---------------------------------------------------------------------------------------------------- 
        let radius = 1.0f
        let sphere = 
            Moveable( 
                name="sphere",
                geometry=new Kugel("Sphere", radius, Color.Transparent),   
                surface=SURFACE_KUGEL(Color.OrangeRed),
                color=Color.Yellow, 
                position=Vector3(-8.0f, MySimulation.Instance.GroundLevel + 0.1f, 0.0f),
                direction=Vector3(1.0f, 0.0f, 1.0f ),   
                velocity=OBJECT_VELOCITY,     
                moveRandom=false
            ) 

        let plate1 = 
            Immoveable(
                name="plate1",
                geometry=CORPUS(CONTOUR_PLATE),
                surface=new Surface(MAT_DARKGOLDENROD),
                color=Color.Black,
                position=Vector3(2.0f, 0.0f, 2.0f)
            ) 

        // Ein Polyeder
        let polyeder = 
            new Immoveable(
                name="polyeder",
                geometry=new Polyeder(
                    name="Icosahedron2", 
                    center= Vector3(0.0f, 0.0f, -2.0f),
                    radius=3.0f,
                    color=Color.DarkBlue,
                    tessFactor=4.0f                  
                    ),
                surface=new Surface(MAT_DARKGOLDENROD),
                color=Color.Black,
                position=Vector3(0.0f, MySimulation.Instance.GroundLevel + 0.1f, 0.0f)
            ) 
        let displayables = [sphere:>Displayable; polyeder:>Displayable] 
        writeReportObjects(displayables)
        MySimulation.Instance.AddObjects(displayables)

    /// <summary>
    /// VerschiedeneGeometrien
    /// TODO Probleme mit tesseierten Objekten
    /// Beisst sich mit den Worl-Limits
    /// </summary>
    let VerschiedeneGeometrien() = 
        printScenario("VerschiedeneGeometrien")

        MySimulation.Instance.Reset()
        MySimulation.Instance.ConfigureWorld(Vector3(-20.0f, 0.0f, -20.0f), 10.0f, 4, 2, 4)
        MySimulation.Instance.ConfigVision(
            new Vector3(-5.0f, 20.0f, -30.0f),
            new Vector3( 5.0f,  -25.0f,  10.0f)
        )   

        // ----------------------------------------------------------------------------------------------------
        // Ein Korpus
        // ----------------------------------------------------------------------------------------------------
        /// Im Uhrteigersinn unten
        let CONTOUR =
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

        let CORPUS = 
            new Corpus(
                name="CORPUS",
                contour=CONTOUR,
                height=2.0f,
                colorBottom=Color.White,
                colorTop=Color.White,
                colorSide=Color.White
            )
        let corpus = 
            new Immoveable(
                name="corpus",
                geometry=CORPUS,
                surface=new Surface(MAT_DARKGOLDENROD),
                color=Color.Black,
                position=Vector3(-10.0f, MySimulation.Instance.GroundLevel + 0.1f, 0.0f)
            ) 

        // Ein Polyeder
        let polyeder = 
            new Immoveable(
                name="polyeder",
                geometry=new Polyeder(
                    name="Icosahedron2", 
                    center= Vector3(0.0f, 0.0f, -2.0f),
                    radius=3.0f,
                    color=Color.DarkBlue,
                    tessFactor=4.0f                  
                    ),
                surface=new Surface(MAT_DARKGOLDENROD),
                color=Color.Black,
                position=Vector3(0.0f, MySimulation.Instance.GroundLevel + 0.1f, 0.0f)
            ) 

        // Eine Fläche
        let plane = 
            new Immoveable(
                name="plane",
                geometry=new QuadPatch(
                    name="Plane", 
                    seitenLaenge=5.0f,
                    color=Color.LightPink,
                    tessFactor=12.0f
                ),
                surface=new Surface(                    
                    new Material( 
                        name="mat1",
                        ambient=Color4(0.2f),
                        diffuse=Color4.White,
                        specular=Color4.White,
                        specularPower=20.0f,
                        emissive=Color.DarkSlateGray.ToColor4()
                    ) 
                ),
                position=Vector3(10.0f, MySimulation.Instance.GroundLevel + 0.1f, 0.0f),
                color=Color.Gray
            ) 

        let displayables = [corpus:>Displayable; polyeder:>Displayable; plane:>Displayable]
        writeReportObjects(displayables)
        MySimulation.Instance.AddObjects(displayables)

    /// <summary>
    /// Initialize
    /// </summary>
    let CreateScenarios() =
        AddScenario(0, "CollisionMitWand", CollisionMitWand)
        AddScenario(1, "EinfallswinkelGleichAusfallswinkel", EinfallswinkelGleichAusfallswinkel)
        AddScenario(2, "CollisionKugelUndKorpus", CollisionKugelUndKorpus)
        AddScenario(3, "VerschiedeneGeometrien", VerschiedeneGeometrien)