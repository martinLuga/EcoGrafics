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
open Base.MathSupport
open Base.GlobalDefs

open DirectX.GraficUtils

open Initializations

module CoordinatConversion = 

    let getLogger(name:string) = LogManager.GetLogger(name)
     
    // ----------------------------------------------------------------------------------------------------
    // Test der ToRadians Funktion
    // anhand ausgezeichneter Werte
    // ----------------------------------------------------------------------------------------------------
    [<TestFixture>]
    type Radians() = 

        [<DefaultValue>] val mutable logger : ILog
        [<DefaultValue>] val mutable cameraPosition : Vector3
        [<DefaultValue>] val mutable cameraTarget : Vector3
        [<DefaultValue>] val mutable position : Vector3
        [<DefaultValue>] val mutable radius : float32
        [<DefaultValue>] val mutable phi : float32
        [<DefaultValue>] val mutable theta : float32

        [<OneTimeSetUp>]
        member this.setUp() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            this.logger <- getLogger("Radians")
            this.cameraPosition <- Vector3( 5.0f, 0.0f, -10.0f)
            this.cameraTarget   <- Vector3.Zero            
            this.radius <-  Vector3.Distance (this.cameraPosition, this.cameraTarget)
            this.logger.Debug("CameraPosition is " + this.cameraPosition.ToString() + "\n")

        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        // Zusammenhang
        // Einheitskreis
        // Phi      : Winkel zwischen dem Punkt P und der y-Achse-Achse und y-Achse
        // Bereich  : 0 bis pi 
        // 
        // Theta    : Winkel in der x-z-Ebene
        // Bereich  : 0 bis 2 pi
        //
        // Achtung: Die Tests werden in alphabetischer Reihenfolge ausgeführt 
        // im Test-Explorer aber in umgekehrten Reihenfolge des Ablaufs angezeigt
        // ----------------------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        
        [<Test>]
        member this.Left() = 
            let point =  Vector3.Left * this.radius
            this.logger.Debug("Pos=" + point.ToString())
            let (phi, theta) = ToRadians(point, this.radius) 
            this.logger.Debug("phi=" + phi.ToString() + " theta=" + theta.ToString() +  " radius=" + this.radius.ToString())
            Assert.AreEqual (0.0f, theta)
            Assert.AreEqual (pihalbe, phi)

        [<Test>]
        member this.Right() =
            let point =  Vector3.Right * this.radius
            let (phi, theta) = ToRadians(point, this.radius) 
            this.logger.Debug("phi=" + phi.ToString() + " theta=" + theta.ToString() +  " radius=" + this.radius.ToString())
            Assert.AreEqual (pi,theta)
            Assert.AreEqual (pihalbe,phi)

        [<Test>]
        member this.Up() =    
            let point =  Vector3.Up * this.radius
            let (phi, theta) = ToRadians(point, this.radius) 
            this.logger.Debug("phi=" + phi.ToString() + " theta=" + theta.ToString() +  " radius=" + this.radius.ToString())
            Assert.AreEqual (0.0f, phi)
            // Theta kann hier nicht definiert sein

        [<Test>]
        member this.Down() =    
            let point =  Vector3.Down * this.radius
            let (phi, theta) = ToRadians(point, this.radius) 
            this.logger.Debug("phi=" + phi.ToString() + " theta=" + theta.ToString() +  " radius=" + this.radius.ToString())
            Assert.AreEqual (pi,phi)
            // Theta kann hier nicht definiert sein

        [<Test>]
        member this.Back() =  
            let point =  Vector3.ForwardLH * this.radius
            let (phi, theta) = ToRadians(point, this.radius) 
            this.logger.Debug("phi=" + phi.ToString() + " theta=" + theta.ToString() +  " radius=" + this.radius.ToString())
            Assert.AreEqual (pihalbe,theta)
            Assert.AreEqual (pihalbe,phi)

        [<Test>]
        member this.Front() =        
            let point =  Vector3.BackwardLH * this.radius
            let (phi, theta) = ToRadians(point, this.radius) 
            this.logger.Debug("phi=" + phi.ToString() + " theta=" + theta.ToString() +  " radius=" + this.radius.ToString())
            Assert.AreEqual (dreiPiHalbe,theta)
            Assert.AreEqual (pihalbe,phi)

    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // Test der ToCartesian Funktion
    // anhand ausgezeichneter Werte
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    [<TestFixture>]
    type Cartesian() = 

        [<DefaultValue>] val mutable logger : ILog
        [<DefaultValue>] val mutable cameraPosition : Vector3
        [<DefaultValue>] val mutable position : Vector3
        [<DefaultValue>] val mutable radius : float32
        [<DefaultValue>] val mutable phi : float32
        [<DefaultValue>] val mutable theta : float32

        [<OneTimeSetUp>]
        member this.setUp() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            this.logger <- getLogger("Cartesian")

        [<Test>]
        member this.Left() =
            this.phi    <- pihalbe
            this.theta  <- 0.0f
            this.radius <- 1.0f
            this.cameraPosition <- Vector3.Left
            this.logger.Debug("Phi=" + this.phi.ToString())
            this.logger.Debug("Theta=" + this.theta.ToString())
            this.logger.Debug("Radius=" + this.radius.ToString()) 
            this.position <- ToCartesian(this.phi, this.theta, this.radius)
            this.logger.Debug("Calculated position=" + this.position.ToString()) 
            Assert.IsTrue(approximatelySame(this.cameraPosition, this.position))

        [<Test>]
        member this.Right() =
            this.phi    <- pihalbe
            this.theta  <- pi
            this.radius <- 1.0f
            this.cameraPosition <- Vector3.Right
            this.logger.Debug("Phi=" + this.phi.ToString())
            this.logger.Debug("Theta=" + this.theta.ToString())
            this.logger.Debug("Radius=" + this.radius.ToString()) 
            this.position <- ToCartesian(this.phi, this.theta, this.radius)
            this.logger.Debug("Calculated position=" + this.position.ToString()) 
            Assert.IsTrue(approximatelySame(this.cameraPosition, this.position))

        [<Test>]
        member this.Up() =
            this.phi    <- 0.000001f
            this.theta  <- pihalbe
            this.radius <- 1.0f
            this.theta <- MathUtil.Clamp(this.theta, -MathUtil.TwoPi, MathUtil.TwoPi) 
            this.phi <- MathUtil.Clamp(this.phi, 0.1f, MathUtil.Pi - 0.1f)
            this.cameraPosition <- Vector3.Up
            this.logger.Debug("Phi=" + this.phi.ToString())
            this.logger.Debug("Theta=" + this.theta.ToString())
            this.logger.Debug("Radius=" + this.radius.ToString()) 
            this.position <- ToCartesian(this.phi, this.theta, this.radius)
            this.logger.Debug("Calculated position=" + this.position.ToString()) 
            Assert.IsTrue(approximatelySame(this.cameraPosition, this.position))

        [<Test>]
        member this.Down() =
            this.phi    <- pi
            this.theta  <- pihalbe        
            this.radius <- 1.0f
            this.theta <- MathUtil.Clamp(this.theta, -MathUtil.TwoPi, MathUtil.TwoPi) 
            this.phi <- MathUtil.Clamp(this.phi, 0.1f, MathUtil.Pi - 0.1f)
            this.cameraPosition <-  Vector3.Down
            this.logger.Debug("Phi=" + this.phi.ToString())
            this.logger.Debug("Theta=" + this.theta.ToString())
            this.logger.Debug("Radius=" + this.radius.ToString()) 
            this.position <- ToCartesian(this.phi, this.theta, this.radius)
            this.logger.Debug("Calculated position=" + this.position.ToString()) 
            Assert.IsTrue(approximatelySame(this.cameraPosition, this.position))

        [<Test>]
        member this.Forward() =
            this.phi    <- pihalbe
            this.theta  <- pihalbe        
            this.radius <- 1.0f
            this.theta <- MathUtil.Clamp(this.theta, -MathUtil.TwoPi, MathUtil.TwoPi) 
            this.phi <- MathUtil.Clamp(this.phi, 0.1f, MathUtil.Pi - 0.1f)
            this.cameraPosition <-  Vector3.ForwardLH
            this.logger.Debug("Phi=" + this.phi.ToString())
            this.logger.Debug("Theta=" + this.theta.ToString())
            this.logger.Debug("Radius=" + this.radius.ToString()) 
            this.position <- ToCartesian(this.phi, this.theta, this.radius)
            this.logger.Debug("Calculated position=" + this.position.ToString()) 
            Assert.IsTrue(approximatelySame(this.cameraPosition, this.position))

        [<Test>]
        member this.Backward() =
            this.phi    <- pihalbe
            this.theta  <- dreiPiHalbe        
            this.radius <- 1.0f
            this.theta <- MathUtil.Clamp(this.theta, -MathUtil.TwoPi, MathUtil.TwoPi) 
            this.phi <- MathUtil.Clamp(this.phi, 0.1f, MathUtil.Pi - 0.1f)
            this.cameraPosition <-  Vector3.BackwardLH
            this.logger.Debug("Phi=" + this.phi.ToString())
            this.logger.Debug("Theta=" + this.theta.ToString())
            this.logger.Debug("Radius=" + this.radius.ToString()) 
            this.position <- ToCartesian(this.phi, this.theta, this.radius)
            this.logger.Debug("Calculated position=" + this.position.ToString()) 
            Assert.IsTrue(approximatelySame(this.cameraPosition, this.position))

    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // Test der ToCartesian Funktion
    // anhand ausgezeichneter Werte
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    [<TestFixture>]
    type Camera() = 

        [<DefaultValue>] val mutable logger : ILog
        [<DefaultValue>] val mutable cameraPosition : Vector3
        [<DefaultValue>] val mutable cameraTarget : Vector3
        [<DefaultValue>] val mutable position : Vector3
        [<DefaultValue>] val mutable radius : float32
        [<DefaultValue>] val mutable phi : float32
        [<DefaultValue>] val mutable theta : float32

        [<OneTimeSetUp>]
        member this.setUp() =
            this.logger <- getLogger("Cartesian")
            this.cameraPosition <- Vector3( 5.0f, 0.0f, -10.0f)
            this.cameraTarget   <- Vector3.Zero            
            this.radius <-  Vector3.Distance (this.cameraPosition, this.cameraTarget)
            this.logger.Debug("CameraPosition is " + this.cameraPosition.ToString() + "\n")
        
        [<Test>]
        // Muss zuerst laufen: Die konfigurierte Position in Radians
        member this.V1() = 
            this.logger.Debug("Position=" + this.cameraPosition.ToString()) 
            this.radius <- Vector3.Distance (this.cameraPosition, this.cameraTarget)
            let (phi, theta) = ToRadians(this.cameraPosition, this.radius)  
            this.phi <- phi
            this.theta <- theta
            this.logger.Debug("Phi=" + phi.ToString())
            this.logger.Debug("Theta=" + theta.ToString())
            this.logger.Debug("Radius=" + this.radius.ToString())

        [<Test>]
        member this.V2() =
            this.logger.Debug("Position=" + this.cameraPosition.ToString()) 
            this.radius <-  Vector3.Distance (this.cameraPosition, this.cameraTarget)
            this.position <- ToCartesian(this.phi, this.theta, this.radius) 
            this.logger.Debug("Calculated position=" + this.position.ToString()) 
            Assert.IsTrue(approximatelySame(this.cameraPosition, this.position))