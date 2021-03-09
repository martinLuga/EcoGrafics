namespace ApplicationBase
//
//  ExampleApp.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 
open System

open log4net

open System.Windows.Forms
open System.Collections.Generic
open System.Diagnostics

open DirectX.D3DUtilities
open DirectX.Camera
open DirectX.MeshObjects
 

open SharpDX
open SharpDX.Windows
open SharpDX.Mathematics.Interop

open Base.Framework
open Base.FileSupport
open Base.Logging

open Geometry.GeometricModel

open DisplayableObject

open GPUModel.MyGPU
open GPUModel.MyGraphicWindow
open GPUModel.MyPipelineConfiguration

open Shader.FrameResources
open Shader.FrameResources.CookBook
open Shader.ShaderSupport
  
open ShaderConfiguration

// ----------------------------------------------------------------------------------------------------
// Application using shaders from DirectX Cookbook  
//
// Singleton
//  gpu
//  window
//  renderLoop
// ----------------------------------------------------------------------------------------------------
module GraficSystem = 

    let logger = LogManager.GetLogger("GraficSystem")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)

    type DirectionalLight = CookBook.DirectionalLight
    type FrameConstants = CookBook.FrameConstants
    type ObjectConstants = CookBook.ObjectConstants
    type MaterialConstants = CookBook.MaterialConstants

    type Texture =  DirectX.D3DUtilities.Texture

    type Material =  Geometry.GeometricModel.Material

    exception GraficSystemError of string

    let invalidValueError = GraficSystemError("Der übergebene Wert ist ungültig")
 
    // ----------------------------------------------------------------------------------------------------
    // System type 
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>] 
    type MySystem(graficWindow:MyWindow) =
        
        static let mutable instance = new MySystem()  // Singleton
       
        let mutable myGpu:MyGPU = MyGPU.Instance
        let mutable graficWindow=graficWindow 

        let mutable displayables = Dictionary<string,Displayable>() 
        let mutable materialIndices = new Dictionary<string, int>()
        let mutable geometries = new Dictionary<string, MeshData>()
        let mutable textureFiles = new Dictionary<string,string>()
        let mutable frameLight:DirectionalLight = DirectionalLight(Color4.White)
        let mutable lightDir = Vector4.Zero
        let mutable lastPipelineConfig = ""
        let mutable lastVisibility=Visibility.Opaque        
        let mutable currentPixelShaderDesc:ShaderDescription=null
        let mutable lastPixelShaderDesc:ShaderDescription=null
        let mutable isRunning:bool = false 
        let mutable tessellationFactor = 8.0f
        let mutable rasterizationFactor = 8.0f       
        let mutable rasterizerType = RasterType.Solid
        let mutable defaultRasterType:RasterType = RasterType.Undefinded
        let mutable blendType = BlendType.Opaque   
        let mutable defaultBlendType = BlendType.Undefinded 
        let mutable defaultPixelShader = ShaderClass.NotSet

        new() = new MySystem(MyWindow.Instance)

        interface IDisposable with 
            member this.Dispose() =  
                (myGpu:> IDisposable).Dispose()
                graficWindow.Dispose()

        static member Instance
            with get() = instance
            and set(value) = instance <- value

        static member CreateInstance(graficWindow:UserControl, defaultConfigurations: MyPipelineConfiguration list) =
            let win =  graficWindow:?> MyWindow
            MySystem.Instance <- new MySystem(win)
            MyGPU.Instance.Initialize(win)
            MyGPU.Instance.FrameLength <- D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
            MyGPU.Instance.MatLength   <- D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
            MyGPU.Instance.ItemLength  <- D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()
            MyGPU.Instance.SetPipelineConfigurations(defaultConfigurations)

        /// <summary>
        /// Initializer
        /// </summary>
        member this.initialize() =
            this.ClearObjects()  
            this.SetPixelShader(defaultPixelShader) 
            this.SetRasterizerState(defaultRasterType)
            this.SetBlendType(defaultBlendType) 

        /// <summary>
        /// Initializer
        /// </summary>
        member this.Configure(pixelShader, rasterType, blendType) =
            this.ClearObjects()  
            defaultPixelShader <- pixelShader 
            defaultRasterType  <- rasterType
            defaultBlendType   <- blendType 

        member this.IsRunning
            with get () = isRunning
            and  set(value) = isRunning <- value
            
        member this.FrameLight
            with get() = frameLight
            and set(value) = frameLight <- value

        member this.LightDir
            with get() = lightDir
            and set(value) = lightDir <- value

        member this.Displayables
            with get() = displayables
            and set(value) = displayables <- value

        member this.TessellationFactor
            with get() = tessellationFactor
            and set(value) = tessellationFactor <- value
        
        member this.RasterizationFactor
            with get() = rasterizationFactor
            and set(value) = rasterizationFactor <- value

        member this.RasterizerType
            with get() = rasterizerType
            and set(value) = rasterizerType <- value

            
        member this.DefaultRasterizerType
            with get() = defaultRasterType
            and set(value) = defaultRasterType <- value
            
        member this.BlendType
            with get() = blendType
            and set(value) = blendType <- value

        member this.SetPixelShader(shader: ShaderClass) =
            currentPixelShaderDesc <- ShaderDescForType(shader)
            myGpu.CurrentPixelShaderDesc <- currentPixelShaderDesc

        member this.AddObject(displayable:Displayable) = 
            this.addDisplayable(displayable)
            myGpu.RefreshGeometry(geometries)
            
        abstract member AddObjects: Displayable list -> unit
        default this.AddObjects(displayables:Displayable list) = 
            for displayable in displayables do
                this.addDisplayable(displayable)
            this.InstallObjects()

        member this.RefreshObject(displayable:Displayable) = 
            this.RefreshGeometry(displayable:Displayable) 

        member this.GetObject(name) = 
            displayables.Item(name)

        member this.ClearObjects() = 
            displayables.Clear() 
            materialIndices.Clear()
            geometries.Clear()

        member this.addDisplayable(displayable:Displayable) = 
            if displayables.ContainsKey(displayable.Name) then
                raise (ObjectDuplicateException(displayable.Name))
            else
                displayables.Add(displayable.Name, displayable)
                this.storeGeometry(displayable)
                this.rememberMaterial(displayable.Surface.Material) 

        member this.storeGeometry(displayable:Displayable) =
            if geometries.ContainsKey(displayable.Geometry.Name) then
                geometries.Replace(displayable.Geometry.Name, displayable.getVertexData())
            else
                geometries.Add(displayable.Geometry.Name, displayable.getVertexData())

        member this.RefreshGeometry(displayable:Displayable) =            
            if geometries.ContainsKey(displayable.Geometry.Name) then
                geometries.Replace(displayable.Geometry.Name, displayable.getVertexData())
            else
                geometries.Add(displayable.Geometry.Name, displayable.getVertexData())

        member this.rememberMaterial(material:Material) =
            if materialIndices.ContainsKey(material.Name) then
                ()
            else 
                let materialIndex = if materialIndices.Values.Count = 0 then 0 else materialIndices.Count
                materialIndices.Add(material.Name, materialIndex)

        member this.LoadTextureFiles(map, project, directory) =
            let files = filesInMap map project directory
            for file in files do 
                let textureName = (file.Name.Split('.')).[0]
                let textureFilename = file.FullName
                textureFiles.Add(textureName, textureFilename)

        member this.ToggleRasterizerState() = 
            if rasterizerType = RasterType.Solid then
                rasterizerType <- RasterType.Wired
            else
                rasterizerType <- RasterType.Solid
            myGpu.RasterizerDesc <- rasterizerDescFromType(rasterizerType)

        member this.ToggleBlendState() = 
            if blendType = BlendType.Opaque then
                blendType <- BlendType.Transparent
            else
                blendType <- BlendType.Opaque
            myGpu.BlendDesc <- blendDescFromType(blendType)

        member this.SetRasterizerState(rasterType:RasterType) = 
            rasterizerType <- rasterType
            myGpu.RasterizerDesc <- rasterizerDescFromType(rasterType)

        member this.SetBlendType(iblendType:BlendType) = 
            blendType <- iblendType
            myGpu.BlendDesc <- blendDescFromType(blendType)

        // 
        // Neue Objekte initialisieren
        // 
        member this.InitObjects(newDisplayables:Displayable list) = 
            logInfo("InitObjects")
            if newDisplayables.Length  = 0 then raise (GraficSystemError("Die Anzahl Displayables ist null"))            
            this.ClearObjects()            
            for displayable in newDisplayables do
                displayables.Replace(displayable.Name, displayable)
            this.InstallObjects()

        // 
        // Steuerung
        // 
        member this.Stop() =
            isRunning <- false

        member this.Start() =
            logInfo("Start")
            myGpu.Begin()
            isRunning <- true
            // Windows Render-Loop
            let loop = new RenderLoop(graficWindow)
            while loop.NextFrame() && isRunning do
                myGpu.StartUpdate()
                this.updatePerFrame() 
                myGpu.StartDraw()               
                let mutable idx = 0
                let sorted = displayables.Values|> Seq.sortBy(fun disp -> disp.isTransparent())
                for displayable in sorted do
                    this.updatePerObject(idx, displayable)
                    this.updatePerMaterial(displayable)
                    this.drawPerObject(idx, displayable)
                    idx <- idx + 1
                myGpu.EndDraw()
            logInfo("ExampleApp INFO: Loop END\n")

        // 
        // Alles dazugehörige installieren (Material,...) 
        // 
        member this.InstallObjects() = 
            logInfo("InstallObjects") 

            // All Tesselation-Modes, die in Geometries vorkommen
            // Daraus ergeben sich die benötigten shader
            let tesselationModes = displayables.Values |> List.ofSeq |> List.map (fun disp -> disp.Geometry.tesselationMode()) |> List.distinct 
            
            let pipelineConfig = this.configForTessMode(tesselationModes.Head) 
            if pipelineConfig <> lastPipelineConfig then
                myGpu.SetConfig(pipelineConfig)
            lastPipelineConfig <- pipelineConfig  

            // Materials
            let materials = displayables.Values |> List.ofSeq |> List.map (fun disp -> disp.Surface.Material) |> List.distinct 
            materials |> List.iter (fun mat -> this.rememberMaterial(mat))  
            
            // Geometries
            displayables.Values |> List.ofSeq |> List.iter (fun displayable -> this.storeGeometry(displayable))            
            myGpu.InstallObjects(displayables.Count, materials.Length, geometries, textureFiles)

        // 
        // Alle Geometrien neu erstellen
        // 
        member this.refreshGeometry() = 
            logInfo("refreshGeometry") 
            let disp = displayables.Values |> Seq.map (fun  displayable  -> displayable.Copy()) |> ResizeArray<Displayable>    // Recreate Displayables
            disp |> List.ofSeq |> List.iter (fun displayable -> this.storeGeometry(displayable))                        // Store in cache           
            myGpu.RefreshGeometry(geometries)

        // ----------------------------------------------------------------------------------------------------
        // Update GPU
        // ----------------------------------------------------------------------------------------------------
        member this.updatePerFrame() =
            let frameConst = 
                new FrameConstants(
                    TessellationFactor = tessellationFactor, 
                    Light = frameLight,
                    CameraPosition  = Camera.Instance.Position    
                )
            myGpu.UpdateFrame(ref frameConst)

        member this.updatePerObject(idx:int, displayable:Displayable) = 
            if displayable.Changed then
                this.RefreshObject(displayable)
                this.refreshGeometry()
                displayable.Changed <- false

            let viewProjectionMatrix = Camera.Instance.ViewProj
            let perObjectWorld = displayable.World * Camera.Instance.World
            let newObject = 
                new ObjectConstants(
                    World=perObjectWorld,
                    WorldInverseTranspose=Matrix.Transpose(Matrix.Invert(perObjectWorld)),
                    WorldViewProjection=perObjectWorld * viewProjectionMatrix,
                    ViewProjection=viewProjectionMatrix
                )
            let perObject = Transpose(newObject)
            myGpu.UpdateObject(idx, ref perObject)

        member this.updatePerMaterial(displayable:Displayable) = 
            let mat = displayable.Surface.Material
            let newMaterial = 
                new MaterialConstants( 
                    Ambient = mat.Ambient,
                    Diffuse = mat.Diffuse,
                    Specular = mat.Specular,
                    SpecularPower = mat.SpecularPower,
                    Emissive = mat.Emissive,
                    HasTexture = RawBool(displayable.hasTexture()), 
                    UVTransform = Matrix.Identity
                )
            let matIdx = materialIndices.Item(mat.Name)
            myGpu.UpdateMaterial(matIdx, ref newMaterial)

        member this.drawPerObject(idx, displayable) = 
            //logDebug("drawPerObject " + displayable.Name)
            let geometryName = displayable.Geometry.Name
            let topology = displayable.Geometry.Topology
            let matIdx = materialIndices.Item(displayable.Surface.Material.Name)
            let textureName = if displayable.Surface.Texture = null then "" else displayable.Surface.Texture.Name
            let tessMode = displayable.Geometry.tesselationMode()
            let visibility = displayable.Surface.Visibility
            let pipelineConfig = this.configForTessMode(tessMode) 
            let blendType = this.blendTypeFromVisibility(visibility)

            // pipelineState
            if (visibility <> lastVisibility) then
                myGpu.BlendDesc <-  blendDescFromType(blendType)
            lastVisibility <- visibility
            if (lastPipelineConfig <> pipelineConfig)  then
                myGpu.SetConfig(pipelineConfig)
            lastPipelineConfig <- pipelineConfig
            myGpu.CurrentPixelShaderDesc <- currentPixelShaderDesc
            lastPixelShaderDesc <- currentPixelShaderDesc
            
            myGpu.RasterizerDesc <- rasterizerDescFromType(rasterizerType)

            myGpu.DrawPerObject(idx, geometryName, topology, matIdx, textureName)

        member this.configForTessMode(tessMode) =
            match tessMode with 
            | TesselationMode.NONE    -> "Basic"
            | TesselationMode.TRI     -> "TesselatedTri"
            | TesselationMode.QUAD    -> "TesselatedQuad"
            | TesselationMode.BEZIER  -> "TesselatedQuad"

        member this.blendTypeFromVisibility(visibility:Visibility)=
            match visibility with 
            | Visibility.Opaque       -> BlendType.Opaque
            | Visibility.Transparent  -> BlendType.Transparent