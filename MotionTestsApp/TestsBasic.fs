namespace SimulationApp
//
//  TestsBasic.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System
open SharpDX
open log4net

open Base.Framework

open AntBehaviour.SimulationObjects

open ApplicationBase.GraficSystem

open Geometry.GeometricModel

open Simulation.WeltModul

// ----------------------------------------------------------------------------------------------------
// Tests 
// Anzeige der unbewegten Objekte
// ----------------------------------------------------------------------------------------------------
module TestsBasic =

    let logger = LogManager.GetLogger("Scenarios.Basic")

    let random = Random(0)

    let randPosInXZ() = 
        Vector3(
            random.Next(Welt.Instance.XMIN.ToInt(), Welt.Instance.XMAX.ToInt())|> float32,
            0.5f,  
            random.Next(Welt.Instance.ZMIN.ToInt(), Welt.Instance.ZMAX.ToInt())|> float32 
            )

    let randPosInXYZ() = 
        Vector3(
            random.Next(Welt.Instance.XMIN.ToInt() + 5 , Welt.Instance.XMAX.ToInt() - 5 )|> float32,
            random.Next(Welt.Instance.YMIN.ToInt() + 5 , Welt.Instance.YMAX.ToInt() - 5 )|> float32,
            random.Next(Welt.Instance.ZMIN.ToInt() + 5 , Welt.Instance.ZMAX.ToInt() - 5 )|> float32 
            )

    let anthillCenter = 
        new AnthillCenter(
            name="AnthillCenter",
            geometry=new Pyramide(
                name="AnthillPyramide",
                seitenLaenge=10.0f,
                hoehe=6.0f,
                colorFront=Color.Red,
                colorRight=Color.DarkRed,
                colorBack=Color.Red,
                colorLeft=Color.DarkRed,
                colorBasis=Color.LightPink
            ),
            color=Color.Red,
            position=Vector3(0.0f, -12.0f, 0.0f),
            energy=120.0f
            )

    // ----------------------------------------------------------------------------------------------------
    // Kompletter Ablauf
    // Erzeugen
    // In Bewegung setzen
    // Für eine Zeit anhalten
    // Bewegt sich wieder 
    // Stoppen
    // Init auf Ausgangspunkt
    // ----------------------------------------------------------------------------------------------------
    let testBasic = async {

        Welt.Instance.InitFromPoints(-20.0f, 20.0f, 0.0f, 20.0f, -20.0f, 20.0f, 5.0f)

        MySystem.Instance.ClearObjects()
 
        let anthillCenter = 
            new AnthillCenter(
             name="AnthillCenter",
             geometry=new Pyramide(
                name="AnthillPyramide",
                seitenLaenge=10.0f,
                hoehe=6.0f,
                colorFront=Color.Red,
                colorRight=Color.DarkRed,
                colorBack=Color.Red,
                colorLeft=Color.DarkRed,
                colorBasis=Color.LightPink
                ),
             color=Color.Red,
             position=Vector3(0.0f, -12.0f, 0.0f),
             energy=120.0f
             )

        let ant1 = 
            new Ant(
                "A1",
                Vector3(-4.0f, 0.0f, 0.0f),
                Vector3.UnitY,
                0.0f, Color.DarkCyan,
                false,
                20.0f,
                new Memory(anthillCenter, Welt.Instance.Ground)
            )
        MySystem.Instance.AddObject(ant1)
        
        let ant2 = 
            new Ant(
                "A2",
                Vector3( 0.0f, 0.0f, 0.0f),
                Vector3.UnitY,
                0.0f,
                Color.DarkCyan,
                false,
                20.0f,
                new Memory(anthillCenter, Welt.Instance.Ground)
            )
        MySystem.Instance.AddObject(ant2)
        
        let ant3 = 
            new Ant(
                "A3",
                Vector3( 4.0f, 0.0f, 0.0f),
                Vector3.UnitY,
                0.0f,
                Color.DarkCyan,
                false, 20.0f,
                new Memory(anthillCenter, Welt.Instance.Ground)
            )
        MySystem.Instance.AddObject(ant3)

        logger.Info("Test Basic started")
        do! Async.Sleep 1000

        logger.Info("Ant1 bewegen") 
        let ant1Start = ant1.Position
        ant1.MoveDirection Vector3.UnitY  0.1f 
        do! Async.Sleep 3000

        logger.Info("Ant stop")
        ant1.stop()  
        do! Async.Sleep 3000

        logger.Info("Initialize all Ants")
        do! Async.Sleep 1000

        logger.Info("Remove all Ants")
        MySystem.Instance.ClearObjects()

        logger.Info("Test Basic ended")
    }