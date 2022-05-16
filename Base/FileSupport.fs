namespace Base
//
//  Framework.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open System.IO

open StringSupport

// ----------------------------------------------------------------------------
// File convenience functions
// ----------------------------------------------------------------------------
module FileSupport = 

    let fileNameHere path name = 
        let basePath = "."
        let sep = "\\" 
        Path.Combine(basePath, path + sep + name)

    let dirNameHere path = 
        let basePath = "."
        let sep = "\\" 
        Path.Combine(basePath, path)

    // ----------------------------------------------------------------------------
    // File im eigenen Projekt richtig adressieren
    // ----------------------------------------------------------------------------
    let fileNameInPlace path name = 
        let projectDirectory = __SOURCE_DIRECTORY__
        let basePath = Directory.CreateDirectory(projectDirectory)
        let sep = "\\" 
        match path with
        | null  -> 
            Path.Combine(basePath.FullName, name)
        | _ ->
            Path.Combine(basePath.FullName, path + sep + name)

    //
    // File im Projekt richtig adressieren
    // 
    let fileNameInProject project path name = 
        let sep = "\\" 
        let upDir = ".." 
        let thisDirectory = __SOURCE_DIRECTORY__
        let filePath = thisDirectory + sep + upDir
        let mapPath = Directory.CreateDirectory(filePath)

        let filePath = project + sep + path + sep + name
        Path.Combine(mapPath.FullName, filePath)

    let fileNameInMap map project directory fileName = 
        let sep = "\\" 
        let upDir = ".." 
        let doubleUp =  sep + upDir + sep + upDir  
        let thisDirectory = __SOURCE_DIRECTORY__
        thisDirectory + doubleUp + sep + map + sep + project + sep + directory + sep + fileName

    let dirNameInMap project path = 
        let sep = "\\" 
        let upDir = ".." 
        let thisDirectory = __SOURCE_DIRECTORY__
        let filePath = thisDirectory + sep + upDir
        let mapPath = Directory.CreateDirectory(filePath)

        let filePath = project + sep + path  
        Path.Combine(mapPath.FullName, filePath)

    let filesInPath filePath = 
        let directory = Directory.CreateDirectory(filePath)
        directory.GetFiles() 

    let directoriesInPath filePath = 
        let directory = Directory.CreateDirectory(filePath)
        directory.GetDirectories()

    let filesInPathExtension filePath extension = 
        filesInPath filePath  |> Array.where (fun f -> f.Extension = extension )

    let parentPath (filePath:string) =        
        let directory = Directory.CreateDirectory(filePath)
        let parent = directory.Parent
        parent.FullName


    // ----------------------------------------------------------------------------
    // File im Projekt richtig adressieren
    // ----------------------------------------------------------------------------
    let filesInDirectory project path = 
        let sep = "\\" 
        let upDir = ".." 
        let thisDirectory = __SOURCE_DIRECTORY__
        let filePath = thisDirectory + sep + upDir + sep + project + sep + path
        let directory = Directory.CreateDirectory(filePath)
        directory.GetFiles()    

    // ----------------------------------------------------------------------------
    // File im Projekt richtig adressieren
    // ----------------------------------------------------------------------------
    let filesInMap map project directory = 
        let sep = "\\" 
        let upDir = ".." 
        let doubleUp =  sep + upDir + sep + upDir  
        let thisDirectory = __SOURCE_DIRECTORY__
        let filePath = thisDirectory + doubleUp + sep + map + sep + project + sep + directory
        let directory = Directory.CreateDirectory(filePath)
        directory.GetFiles() 

    let filesInMapExtension map project directory extension = 
        filesInMap map project directory |> Array.where (fun f -> f.Extension = extension )

    // ----------------------------------------------------------------------------
    // Einträge im Directory
    // ----------------------------------------------------------------------------
    let getEntries directoryName = 
        let dirs  = directoriesInPath directoryName |> Array.map (fun x -> x :> FileSystemInfo)
        if dirs.Length > 0 then
            dirs
        else
            let files = filesInPath directoryName |> Array.map (fun x -> x:> FileSystemInfo)
            files 

    // ----------------------------------------------------------------------------
    // File zeilenweise lesen
    // ----------------------------------------------------------------------------
    let readLines (filePath:string) = seq {
        use sr = new StreamReader (filePath)
        while not sr.EndOfStream do
            yield sr.ReadLine ()
    }

    // ----------------------------------------------------------------------------
    // File zeilenweise lesen
    // ----------------------------------------------------------------------------
    let readLinesCSV (filePath:string) = seq {
        use sr = new StreamReader (filePath)
        while not sr.EndOfStream do
            yield getElementsSeparatedBy (sr.ReadLine (), ',')
    }

    // ----------------------------------------------------------------------------
    // Erste Zeile lesen
    // ----------------------------------------------------------------------------
    let readFirstLine (filePath:string) = seq {
        use sr = new StreamReader (filePath)
        yield sr.ReadLine ()
    }

    // ----------------------------------------------------------------------------
    // Erste Zeile lesen
    // ----------------------------------------------------------------------------
    let readFirstLineCSV (filePath:string) = 
        let sr = new StreamReader (filePath)
        getElementsSeparatedBy (sr.ReadLine (), ',') 

    // ----------------------------------------------------------------------------
    // Anzahl Zeilen in text-File
    // ----------------------------------------------------------------------------
    let anzLines (filePath:string) =  
        let mutable counter = 0
        let  sr = new StreamReader (filePath)
        while not sr.EndOfStream do
            sr.ReadLine () |> ignore
            counter <- counter + 1
        counter
 