namespace Base
//
//  Logging.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open System
open System.IO
open System.Net 
open System.Net.Sockets

open System.Windows.Forms
 
open log4net
open log4net.Config

open FileSupport

// ----------------------------------------------------------------------------
// Logging Funktionen
// ----------------------------------------------------------------------------
module LoggingSupport = 

    let mutable P_DEBUG  = false
    let mutable P_INFO   = false
    let mutable P_WARN   = false
    let mutable P_ERROR  = false
    let mutable P_FATAL  = false

    // ----------------------------------------------------------------------------
    // log4net Konfiguration ausführen
    // ----------------------------------------------------------------------------
    let configureLogging (filePath:string) = 
        let sr = new StreamReader (filePath)
        let stream = sr.BaseStream
        Config.XmlConfigurator.Configure(stream)  
        |> ignore  
        
    let configureLoggingBasic () = 
        BasicConfigurator.Configure()
        |> ignore

    let configureLog4net (projectName:String) (folderName:String)  (fileName:string) =     
        let filePath = fileNameInProject projectName folderName fileName
        let sr = new StreamReader (filePath)
        let stream = sr.BaseStream
        Config.XmlConfigurator.Configure(stream)  
        |> ignore  

    let configureLoggingInMap (mapName:String) (projectName:String) (folderName:String)  (fileName:string) = 
        let filePath = fileNameInMap mapName  projectName folderName fileName
        let sr = new StreamReader (filePath)
        let stream = sr.BaseStream
        Config.XmlConfigurator.Configure(stream)  
        |> ignore  

    let doWithLogging aFunction aParm (logger:ILog) =
        logger.Info("--->" + aParm.ToString())
        let result = aFunction aParm
        logger.Info("<----" + result.ToString())
        result

    let outputWorkflow (window:RichTextBox) = async{
        let remoteEndPoint = new IPEndPoint(IPAddress.Any, 0)
        let udpClient = new UdpClient(10000)
        udpClient.Client.ReceiveTimeout <- 1000 
        while (true) do
            do! Async.Sleep 4
            if udpClient.Available > 0 then 
                let buffer = udpClient.Receive(ref remoteEndPoint)
                let outputString = System.Text.Encoding.ASCII.GetString(buffer)
                printfn "--> %O "outputString
                window.Text <- window.Text + outputString
    }

    let Debug (logger:ILog)  (message:string) = 
        if logger.IsDebugEnabled then 
            logger.Debug(message)

    let Info (logger:ILog)  (message:string) = 
        if logger.IsInfoEnabled then 
            logger.Info(message)
            
    let Warn (logger:ILog)  (message:string) = 
        if logger.IsWarnEnabled then 
            logger.Warn(message)

    let Error (logger:ILog)  (message:string) = 
        if logger.IsErrorEnabled then 
            logger.Error(message)

    let Fatal (logger:ILog)  (message:string) = 
        if logger.IsFatalEnabled then 
            logger.Fatal(message)

    let LogLines(lines: string list, logFunc) = 
        for line in lines do
            logFunc(">> " + line )
        logFunc("---"  )

    let LogLine(text, line: string, idx, logFunc) = 
        logFunc(text + "line(" + idx.ToString() + ")= " + line )
    
    let LogText(text, logFunc) = 
        logFunc(text)
    
    let LogObjects(objects:string list list, logFunc) =
        for object in objects do
            logFunc(">> OBJCT << " + (object.Head.ToString()))