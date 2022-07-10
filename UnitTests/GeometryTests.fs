namespace ecografics
//
//  Grafic.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open NUnit.Framework

open SharpDX

open Base.LoggingSupport 
open Base.QuaderSupport

open Geometry.GeometricModel3D

module GeometrySupport = 

    let getLogger(name:string) = LogManager.GetLogger(name)
     
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // Alles zu Plane
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    [<TestFixture>]
    type PlaneSupport() = 

        [<DefaultValue>] val mutable logger     : ILog
        [<DefaultValue>] val mutable laenge     : float32
        [<DefaultValue>] val mutable hoehe      : float32
        [<DefaultValue>] val mutable breite     : float32
        [<DefaultValue>] val mutable p1         : Vector3
        [<DefaultValue>] val mutable p2         : Vector3
        [<DefaultValue>] val mutable p3         : Vector3
        [<DefaultValue>] val mutable p4         : Vector3
        [<DefaultValue>] val mutable p5         : Vector3
        [<DefaultValue>] val mutable p6         : Vector3
        [<DefaultValue>] val mutable p7         : Vector3
        [<DefaultValue>] val mutable p8         : Vector3
        [<DefaultValue>] val mutable position   : Vector3 

        [<OneTimeSetUp>]
        member this.setUp() =
            this.laenge <- 2.0f
            this.breite <- 3.0f
            this.hoehe  <- 4.0f
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            this.logger <- getLogger("Spatprodukt")
            this.logger.Debug("L = " + this.laenge.ToString() + "B = " + this.breite.ToString() + "H = " + this. hoehe.ToString() + "\n")
             
            this.p1 <- Vector3.Zero
            this.p2 <- new Vector3(this.p1.X, this.p1.Y + this.hoehe, this.p1.Z)
            this.p3 <- new Vector3(this.p1.X + this.laenge, this.p1.Y + this.hoehe, this.p1.Z)
            this.p4 <- new Vector3(this.p1.X + this.laenge, this.p1.Y, this.p1.Z)

            // Back im Uhrzeigersinn
            this.p5 <- new Vector3(this.p1.X + this.laenge, this.p1.Y, this.p1.Z + this.breite)
            this.p6 <- new Vector3(this.p1.X + this.laenge, this.p1.Y + this.hoehe, this.p1.Z + this.breite)
            this.p7 <- new Vector3(this.p1.X, this.p1.Y + this.hoehe, this.p1.Z + this.breite)
            this.p8 <- new Vector3(this.p1.X, this.p1.Y, this.p1.Z + this.breite)

        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        // Test des Spatprodukts an Punkten und Ebenen
        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
                
        [<Test>]
        member this.LiegtInFlaeche() =             
            let front =  {P1 = this.p1; P2 = this.p2; P3 = this.p3; P4 = this.p4}
            this.position <- Vector3(1.0f, 1.0f, 0.0f)
            let prob = snd (propableComplanar(this.position, front))
            Assert.AreEqual(0.0f, prob)
        
        [<Test>]
        member this.LiegtNichtInFlaeche() =             
            let front =  {P1 = this.p1; P2 = this.p2; P3 = this.p3; P4 = this.p4}
            this.position <- Vector3(1.0f, 1.0f, 0.5f)
            let prob = snd (propableComplanar(this.position, front))
            Assert.AreNotEqual(0.0f, prob)
 
     // ----------------------------------------------------------------------------------------------------
     // ----------------------------------------------------------------------------------------------------
     // Alles zu quader
     // ----------------------------------------------------------------------------------------------------
     // ----------------------------------------------------------------------------------------------------
     [<TestFixture>]
     type QuaderSupport() = 

         [<DefaultValue>] val mutable logger     : ILog
         [<DefaultValue>] val mutable laenge     : float32
         [<DefaultValue>] val mutable hoehe      : float32
         [<DefaultValue>] val mutable breite     : float32
         [<DefaultValue>] val mutable quader1    : Quader 
         [<DefaultValue>] val mutable position   : Vector3 

         [<OneTimeSetUp>]
         member this.setUp() =
             configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
             this.logger <- getLogger("Quader")

         [<Test>]
         member this.trifftVonVorn() =
             this.quader1 <- new Quader("ADOBE1", 3.0f, 4.0f, 4.0f, Color.Brown)
             this.position <- Vector3(1.0f, 1.0f, 0.0f)
             let plane = this.quader1.planeWithPoint(this.position)
             logPlane(plane, this.logger)
