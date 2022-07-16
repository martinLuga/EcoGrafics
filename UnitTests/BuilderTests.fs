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
open Base.MeshObjects
open Base.VertexDefs 

open Base.ModelSupport 
 
open Builder 
 
open Initializations
open ExampleShaders

module BuilderTests =
     
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // Test der Record Funktionen
    // Einlesen von Textdateien auf Record-Basis
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------

    let LogLines(logfun, lines: string list) = 
        for line in lines do
            logfun(">> " + line )
        logfun("---"  )

    let LogLine(logfun, text, line: string, idx) = 
        logfun(text + "line(" + idx.ToString() + ")= " + line )
    
    let LogText(logfun, text ) = 
        logfun(text)
    
    let LogMesh(logfun, mesh:MeshData<Vertex>) = 
        logfun("" + mesh.Properties.ToString()) 

    let LogObjects(logfun, objects:string list list) =
        for object in objects do
            logfun(">> OBJCT << " + (object.Head.ToString()))
            LogLines(logfun, object)

    let LogSummary(logfun, objects:string list list) =
        logfun("Anzahl Objekte  " + objects.Length.ToString())
        for object in objects do
            logfun("---Objekte " + (object.Head.ToString()))

    [<TestFixture>]
    type GlbFormat() = 

        [<DefaultValue>] val mutable logger : ILog
        [<DefaultValue>] val mutable filename : string 

        [<OneTimeSetUp>]
        member this.setUp() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            let getLogger(name:string) = LogManager.GetLogger(name)
            this.logger <- getLogger("GlbBuilder")
            this.initFiles("C:\\temp\\gltf\\", "Megalodon.obj")

        member this.initFiles(directory, newFileName) =
            this.filename <- directory + newFileName
            logger.Debug(" ")

        member this.LogText(text ) = 
            logger.Debug(text)
        
        member this.LogMesh(mesh:MeshData<Vertex>) = 
            logger.Debug("" + mesh.ToString())

        member this.LogObjects(objects:string list list) =
            for object in objects do
                logger.Debug(">> OBJCT << " + (object.Head.ToString()))

    [<TestFixture>]
    type GlbBuilding() = 

        [<DefaultValue>] val mutable logger : ILog
        [<DefaultValue>] val mutable filename : string   

        [<OneTimeSetUp>]
        member this.setUp() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            let getLogger(name:string) = LogManager.GetLogger(name)
            this.logger <- getLogger("GlbBuilder")
            this.initFiles("C:\\temp\\gltf\\", "Megalodon.obj")

        member this.initFiles(directory, newFileName) =
            this.filename <- directory + newFileName
            logger.Debug(" ") 

        [<TestCase("Megalodon.glb")>]
        member this.Build(fileName) = 
            this.initFiles("C:\\temp\\gltf\\", fileName)  
            let parts = PBRBuilder.Build(
                "megalodon",
                "C:\\temp\\gltf\\Megalodon.glb",
                Vector3(0.0f, 0.0f, 0.0f),
                Matrix.Identity,
                Vector3.One,
                Visibility.Opaque,
                Augmentation.Hilite
            )
            Assert.NotNull(parts)