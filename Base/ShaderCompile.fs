namespace Base
//
//  ShaderCompile.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open SharpDX
open SharpDX.D3DCompiler
open SharpDX.Direct3D12

open System
open System.IO

open Base.FileSupport
open Base.ShaderSupport

// ----------------------------------------------------------------------------------------------------
// SHADER  lesen und compilieren
// ----------------------------------------------------------------------------------------------------  
module ShaderCompile = 
    
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

    let logger = LogManager.GetLogger("ShaderCompile")
    let mutable PRECOMPILED = true
    let mutable byteCode:D3DCompiler.ShaderBytecode = null
    exception ShaderError of string

    type IncludeFX (dirName:string) = 
        let mutable includeDirectory = dirName
        interface Include with
            member this.Shadow
                with get () = this :> IDisposable
                and set v = ()
            member this.Dispose(): unit = ()

            member this.Close (stream:Stream) =
                stream.Close()
                stream.Dispose()  

            member this.Open (typ, fileName, parentStream)  =
                new FileStream(includeDirectory +  "\\" + fileName, FileMode.Open) :> Stream

    let loadCompiled(desc: ShaderDescription) =  
        if not PRECOMPILED then
            raise (ShaderError("Not using precompiled shaders " ))
        let fileName = fileNameHere desc.Directory desc.File + "_" + desc.Entry + ".cso"
        let mutable str:FileStream = null 
        let mutable result:D3DCompiler.ShaderBytecode = null
        try   
            str <- new FileStream(fileName, FileMode.Open)
            result <- ShaderBytecode.Load(str)
            str.Close()
            str.Dispose()
            logger.Debug("Using precompiled shader named: " + fileName)
        with :? FileNotFoundException -> 
            logger.Debug("Try compile : " + fileName)
            raise (ShaderError("No precompiled shader named: " + fileName))
        result

    let storeCompiled(bytecode:D3DCompiler.ShaderBytecode, desc: ShaderDescription) = 
        let fileName = fileNameHere desc.Directory desc.File + "_" + desc.Entry + ".cso"
        let mutable str:FileStream = null
        try   
            str <- new FileStream(fileName, FileMode.Create)
            bytecode.Save(str)
            str.Close()
            str.Dispose()
            logger.Debug("Stored shader bytecode to : " + fileName)
        with :? IOException -> logger.Warn("Cannot store precomiled shader named: " + fileName)

    let shaderFromFile (desc: ShaderDescription) =
        try   
            byteCode <- loadCompiled(desc)
        with :? ShaderError  -> 
            let fileName = fileNameHere desc.Directory desc.File + ".hlsl" 
            logger.Warn("Compiling shader named: " + fileName + "_" + desc.Entry)
            let dirName = dirNameHere desc.Directory  
            let includeHandler = new IncludeFX(dirName)
            let compResult = ShaderBytecode.CompileFromFile(fileName, desc.Entry, desc.Mode, ShaderFlags.OptimizationLevel3, EffectFlags.None, null, includeHandler) 
            if compResult.Bytecode <> null then
                byteCode <- compResult.Bytecode
                storeCompiled(byteCode, desc)
            else
                logger.Error("Compile-Error : " + compResult.Message)
                raise (ShaderError(compResult.Message))
        ShaderBytecode(byteCode.Data)

    let shaderFromString (str: string, entry, profile) =
        let compResult = ShaderBytecode.Compile(str, entry, profile, ShaderFlags.OptimizationLevel3, EffectFlags.None) 
        if compResult.Bytecode <> null then
            byteCode <- compResult.Bytecode
        else
            logger.Error("Compile-Error : " + compResult.Message)
            raise (ShaderError(compResult.Message))
        ShaderBytecode(byteCode.Data)

    let shaderFromStringAndFile (shParms: string, dir:string, file:string, entry, profile) =
        let fileName = fileNameHere dir file + ".hlsl" 
        let fs = new FileStream(fileName, FileMode.Open, FileAccess.Read)  :> Stream 
        
        // read the whole content to a string.
        let mutable code = ""
        using (new StreamReader(fs))(fun r ->
            code <-r.ReadToEnd() 
        )
        shaderFromString (shParms + code, entry, profile)