namespace Base
//
//  Logging.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open System
open System.IO
open System.Runtime.Serialization  

open FileSupport
open StringSupport

// ----------------------------------------------------------------------------
// Configuration
// ----------------------------------------------------------------------------
module Configuration = 

    [<AllowNullLiteral>] 
    [<DataContract>] 
    type Configuration() =
        [<field : DataMember>]
        let mutable projectName = ""
        [<field : DataMember>]
        let mutable filePath = ""
        [<field : DataMember>]
        let mutable counter = 0

        member this.FilePath
         with get() = filePath
            and set(value) = filePath <- value

        member this.ProjectName
         with get() = projectName
            and set(value) = projectName <- value

        member this.Counter
         with get() = counter
            and set(value) = counter <- value

    [<AllowNullLiteral>] 
    type Configurator() =
        static let mutable instance:Configurator = new Configurator() 
        let mutable configuration = new Configuration()
        static member Instance
            with get() = 
                if instance = null then
                    instance <- new Configurator()
                instance
            and set(value) = instance <- value

        member this.Configuration
            with get() = configuration
            and set(value) = configuration <- value

        member this.FromFile(filePath) =
            try  
                let xml = File.ReadAllText(filePath)
                this.Configuration <- deserializeXml(xml)
                this.Configuration 
 
            with :?  FileNotFoundException -> null

        member this.FromMapProjectFolderFile (mapName:String) (projectName:String) (folderName:String)  (fileName:string) = 
            try  
                let filePath = fileNameInMap mapName  projectName folderName fileName
                let xml = File.ReadAllText(filePath)
                this.Configuration <- deserializeXml(xml)
                this.Configuration 

            with :?  FileNotFoundException -> null