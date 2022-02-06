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
open Base.MaterialsAndTextures

open Base.ModelSupport 
open Base.ShaderSupport 

open Builder.GlbFormat
open Builder.Glb
 
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
            this.initFiles("C:\\temp\\obj\\", "Megalodon.obj")

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

        [<TestCase("Megalodon.glb")>]
        member this.ReadFromFile(fileName) = 
            this.initFiles("C:\\temp\\obj\\", fileName)  
            let container = getGlbContainer (this.filename)  
            Assert.NotNull(container)

        [<TestCase("Megalodon.glb")>]
        member this.ReadMeshes(fileName) = 
            this.initFiles("C:\\temp\\obj\\", fileName)  
            let meshes = GetMeshes (this.filename)  
            Assert.NotNull(meshes)

        [<TestCase("Megalodon.glb")>]
        member this.ReadNodes(fileName) = 
            this.initFiles("C:\\temp\\obj\\", fileName)  
            let nodes = GetNodes (this.filename)  
            Assert.NotNull(nodes)

    [<TestFixture>]
    type GlbBuilding() = 

        [<DefaultValue>] val mutable logger : ILog
        [<DefaultValue>] val mutable filename : string  
        [<DefaultValue>] val mutable builder : GlbBuilder

        [<OneTimeSetUp>]
        member this.setUp() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            let getLogger(name:string) = LogManager.GetLogger(name)
            this.logger <- getLogger("GlbBuilder")
            this.initFiles("C:\\temp\\obj\\", "Megalodon.obj")

        member this.initFiles(directory, newFileName) =
            this.filename <- directory + newFileName
            logger.Debug(" ") 

        [<TestCase("Megalodon.glb")>]
        member this.Build(fileName) = 
            this.initFiles("C:\\temp\\obj\\", fileName) 
            this.builder <- new GlbBuilder("Megalodon", this.filename) 
            let parts =             
                this.builder.Build(
                   MAT_BEIGE,
                   TEXT_HILL, 
                   Vector3(0.7f,0.7f,0.7f), 
                   Visibility.Opaque,
                   Augmentation.None,
                   Quality.Medium,
                   new ShaderConfiguration(
                       vertexShaderDesc=vertexShaderTesselateDesc,
                       pixelShaderDesc=pixelShaderPhongDesc,
                       domainShaderDesc=ShaderDescription.CreateToBeFilledIn(ShaderType.Domain),
                       hullShaderDesc=ShaderDescription.CreateToBeFilledIn(ShaderType.Hull)
                   )
                )
            Assert.NotNull(this.builder.Parts)
            Assert.IsNotEmpty(this.builder.Parts)