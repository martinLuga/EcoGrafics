namespace ecografics
//
//  Grafic.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net
open NUnit.Framework

open Base.FileSupport
open Base.LoggingSupport 
open Base.MeshObjects
open Base.VertexDefs

open Builder.WavefrontFormat
open Base.RecordSupport
 
open Initializations

module RecordTests = 
     
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
    type RecordProcessing() = 

        [<DefaultValue>] val mutable logger : ILog
        [<DefaultValue>] val mutable filename : string 
        [<DefaultValue>] val mutable lines : string list   
        [<DefaultValue>] val mutable selectedLines: string list 
        [<DefaultValue>] val mutable allGroups:(string list) list 

        [<OneTimeSetUp>]
        member this.setUp() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            let getLogger(name:string) = LogManager.GetLogger(name)
            this.logger <- getLogger("Wavefront")
            this.initFiles("C:\\temp\\obj\\", "Handgun_obj.obj")

        member this.initFiles(directory, newFileName) =
            this.filename <- directory + newFileName
            this.lines <- (readLines (this.filename) |> Seq.toList)
            logger.Debug(newFileName + " with "  + this.lines.Length.ToString() + " lines ")
            logger.Debug(" ")

        member this.LogLines(lines: string list) = 
            for line in lines do
                logger.Debug(">> " + line )
            logger.Debug("---"  )

        member this.LogLine(text, line: string, idx) = 
            logger.Debug(text + "line(" + idx.ToString() + ")= " + line )
        
        member this.LogText(text ) = 
            logger.Debug(text)
        
        member this.LogMesh(mesh:MeshData<Vertex>) = 
            logger.Debug("" + mesh.ToString())

        member this.LogObjects(objects:string list list) =
            for object in objects do
                logger.Debug(">> OBJCT << " + (object.Head.ToString()))
                //this.LogLines(object)

        [<TestCase("Handgun_obj.obj")>]
        [<TestCase("FinalBaseMesh.obj")>]
        [<TestCase("Lowpoly_tree_sample.obj")>]
        [<TestCase("MiniCooper.obj")>]
        member this.FindStart(fileName) = 
            this.initFiles("C:\\temp\\obj\\", fileName)
            let line, position, found = findWith(this.lines, isStart)
            this.LogLine("Start found ", line, position)

        [<TestCase("Handgun_obj.obj")>]
        [<TestCase("FinalBaseMesh.obj")>]
        [<TestCase("Lowpoly_tree_sample.obj")>]
        [<TestCase("MiniCooper.obj")>]
        member this.FindUnequalComment(fileName) = 
            this.initFiles("C:\\temp\\obj\\", fileName)
            let line, position, found = findWith(this.lines, isNoComment)
            this.LogLine("No Comment found ", line, position)

        // ----------------------------------------------------------------------------------------------------
        //  Lesen bis zum Start
        //  Dann alle Vertex-Daten lesen
        // ----------------------------------------------------------------------------------------------------       
        [<TestCase("Handgun_obj.obj")>]
        [<TestCase("FinalBaseMesh.obj")>]
        [<TestCase("Lowpoly_tree_sample.obj")>]
        member this.SelectGroup(fileName) =
            let logDebug = logger.Debug
            let mutable group = []
            let mutable geometry : string list list = [ [] ]
            this.initFiles ("C:\\temp\\obj\\", fileName)

            // Look for first group record
            let firstHeader, firstHeaderPosition, firstObjectLines, remaining, notFound =
                findNextAndSplit (this.lines, isStart)
            // first records can be omittable=true)
            let result =
                Analyze(firstObjectLines, firstHeader, logDebug)

            if notFound then
                // No Group record found
                this.LogText("Keine Gruppierungen in Datei")
                if not (result=AnalyzeResult.Nothing) then
                    geometry <- geometry @ [ firstObjectLines ]
            else
                // Group record found
                // If a valid group exists , process
                if not (result=AnalyzeResult.Nothing) then
                    geometry <- geometry @ [ firstObjectLines ]

                // Look for second group record
                let secondHeader, secondPos, previousRecords, remaining, ende = findNextAndSplit (remaining, isStart)
                // previous group exists ?
                let result =
                    Analyze(previousRecords, secondHeader, logDebug)

                if not (result=AnalyzeResult.Nothing) then
                    geometry <- geometry @ [ previousRecords ]

                if ende then
                    this.LogText("End of File  ")
                else

                    // Look for third group record
                    let thirdHeader, thirdPos, previousRecords, remaining, ende = findNextAndSplit (remaining, isStart)

                    let result =
                        Analyze(previousRecords, thirdHeader, logDebug)

                    if not (result=AnalyzeResult.Nothing) then
                        geometry <- geometry @ [ previousRecords ]

        // ----------------------------------------------------------------------------------------------------
        //  Komplette Datei verarbeiten und in Gruppen zerlegen
        // ----------------------------------------------------------------------------------------------------  
        [<TestCase("Handgun_obj.obj")>]
        [<TestCase("FinalBaseMesh.obj")>]
        [<TestCase("Lowpoly_tree_sample.obj")>]
        [<TestCase("MiniCooper.obj")>]
        member this.SelectAllGroups(fileName) =
            let logDebug = logger.Debug
            let mutable geometry : string list list = [ [] ]
            let mutable atEnd = false
            this.initFiles ("C:\\temp\\obj\\", fileName)
            let mutable fileRemains = this.lines

            // Look for first group record
            while not atEnd do
                let startLine, startPos, previousRecords, remaining, innerEnd = findNextAndSplit (fileRemains, isStart)
                geometry <- geometry @ [ previousRecords ]
                let hasObjects = Analyze(previousRecords, startLine, logDebug) |> ignore
                fileRemains <- remaining
                atEnd <- innerEnd

    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // Test der Funktionen zum Lesen des Wavefront-Dateiformats 
    // Einlesen der Characterdaten als Records.
    // Gruppieren der Zeilen zu Objekten
    // Erstellen der Vertex-Daten
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    [<TestFixture>]
    type WavefrontObjects() = 

        [<DefaultValue>] val mutable logger : ILog
        [<DefaultValue>] val mutable filename : string 
        [<DefaultValue>] val mutable lines : string list   
        [<DefaultValue>] val mutable selectedLines: string list 
        [<DefaultValue>] val mutable allGroups:(string list) list 

        [<OneTimeSetUp>]
        member this.setUp() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            let getLogger(name:string) = LogManager.GetLogger(name)
            this.logger <- getLogger("Wavefront")

        member this.initFiles(newFileName) =
            this.filename <- newFileName 
            this.lines <- (readLines (this.filename) |> Seq.toList)
            this.logger.Debug("Working on example file " + this.filename + " with "  + this.lines.Length.ToString() + " lines ")

        member this.LogLines(lines: string list) = 
            for line in lines do
                logger.Debug(">> " + line )
            logger.Debug("---"  )

        member this.LogLine(text, line: string, idx) = 
            logger.Debug(text + "line(" + idx.ToString() + ")= " + line )
        
        member this.LogText(text ) = 
            logger.Debug(text)
        
        member this.LogMesh(mesh:MeshData<Vertex>) = 
            logger.Debug("" + mesh.ToString())

        member this.LogObjects(objects:string list list) =
            for object in objects do
                logger.Debug(">> OBJCT << " + (object.Head.ToString()))
                //this.LogLines(object)


        [<TestCase("Handgun_obj.mtl")>]
        [<TestCase("Lowpoly_tree_sample.mtl")>]
        member this.ReadMaterials(fileName) =
            let pathName = "C:\\temp\\obj\\" + fileName
            this.initFiles (pathName)
            //this.LogLines(this.lines)
            let subLists = splitAtType (this.lines, "newmtl")
            if not subLists.IsEmpty then
                for sublist in subLists  do
                    logger.Debug("Disp=" + sublist.ToString())