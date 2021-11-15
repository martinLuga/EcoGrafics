namespace Shader
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

    let loadCompiled(fileInfo: string*string*string*string*string) =  
        if not PRECOMPILED then
            raise (ShaderError("Not using precompiled shaders " ))
        let (app, dir, file, entry, profile) = fileInfo 

        let fileName = fileNameInProject app dir file + "_" + entry + ".cso"

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

    let storeCompiled(bytecode:D3DCompiler.ShaderBytecode, fileInfo: string*string*string*string*string) =  
        let (app, dir, file, entry, profile) = fileInfo 
        let fileName = fileNameInProject app dir file + "_" + entry + ".cso"
        let mutable str:FileStream = null
        try   
            str <- new FileStream(fileName, FileMode.Create)
            bytecode.Save(str)
            str.Close()
            str.Dispose()
            logger.Debug("Stored shader bytecode to : " + fileName)
        with :? IOException -> logger.Warn("Cannot store precomiled shader named: " + fileName)

    let shaderFromFile (fileInfo: string*string*string*string*string) =
        try   
            byteCode <- loadCompiled(fileInfo)
        with :? ShaderError  -> 
            let (proj, dir, file, entry, profile) = fileInfo
            let fileName = fileNameInProject proj dir file + ".hlsl" 
            logger.Warn("Compiling shader named: " + fileName + "_" + entry)
            let dirName = dirNameInMap proj dir  
            let includeHandler = new IncludeFX(dirName)
            let compResult = ShaderBytecode.CompileFromFile(fileName, entry, profile, ShaderFlags.OptimizationLevel3, EffectFlags.None, null, includeHandler) 
            if compResult.Bytecode <> null then
                byteCode <- compResult.Bytecode
                storeCompiled(byteCode, fileInfo)
            else
                logger.Error("Compile-Error : " + compResult.Message)
                raise (ShaderError(compResult.Message))
        ShaderBytecode(byteCode.Data)