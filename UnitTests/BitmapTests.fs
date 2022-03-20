namespace ecografics
//
//  Grafic.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net
open NUnit.Framework
open SharpDX.Direct3D

open Base.LoggingSupport 
open Base.MeshObjects
open Base.VertexDefs
open Base.MaterialsAndTextures

open Base.ModelSupport 
open Base.ShaderSupport 

open DirectX.BitmapSupport
 
open Initializations
open ExampleShaders

module BitmapTests =
     
    // ----------------------------------------------------------------------------------------------------
    // Test des BitmapSupport
    // ----------------------------------------------------------------------------------------------------

    let DEVICE_RTX3090 = new Device(null, FeatureLevel.Level_11_0)

    [<TestFixture>]
    type CubeMaps() = 

        [<DefaultValue>] val mutable logger : ILog
        [<DefaultValue>] val mutable filename : string         
        [<DefaultValue>] val mutable  bitmapManager:BitmapManager

        [<OneTimeSetUp>]
        member this.setUp() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            let getLogger(name:string) = LogManager.GetLogger(name)
            this.logger <- getLogger("GlbBuilder")
            this.bitmapManager <- BitmapManager(DEVICE_RTX3090)

        member this.initFiles(directory:string) =
            this.filename <- directory  
            logger.Debug(" ")

        [<TestCase("Yokohama")>]
        member this.Initialize(directoryName) = 
            this.bitmapManager.InitFromArray("C:\\temp\\obj\\" + directoryName)