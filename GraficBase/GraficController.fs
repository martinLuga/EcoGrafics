namespace GraficBase
//
//  ExampleApp.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
// 

open System
open System.Collections.Generic
open System.Runtime.InteropServices

open log4net

open SharpDX
open SharpDX.Windows
open SharpDX.DXGI 
open SharpDX.Direct3D12 

open Base
open ModelSupport
open Framework
open PrintSupport
open LoggingSupport
open ObjectBase 
open ShaderSupport
open GameTimer
open MaterialsAndTextures

open DirectX.Assets

open GPUModel.MyGPU

open CameraControl 
open Camera
open GraficWindow
open ShaderPackage

//open ShaderGameProgramming
open ShaderRenderingCookbook
open Interface
//open Pipeline 
//open Structures

// ----------------------------------------------------------------------------------------------------
// Application using shaders from DirectX Cookbook  
//
// Singleton
//  gpu
//  window
//  renderLoop
// ----------------------------------------------------------------------------------------------------
module GraficController =
 
    // ----------------------------------------------------------------------------------------------------
    // Steuerung der Grafic, Versorgung der Gpu, Entgegennehmen der Abbildungsinformationen
    // ----------------------------------------------------------------------------------------------------
    
    exception ObjectNotFoundException of string

    let DEFAULT_LIGHT_DIR   = Vector3(15.0f, -15.0f, 10.0f) 
    let DEFAULT_LIGHT_COLOR = Color.White 

    let logger = LogManager.GetLogger("GraficController")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger) 
    let logError = Error(logger)

    type ControllerStatus = | New | Prepared | Running | Idle | Terminated

    [<AllowNullLiteral>]
    type MyController(graficWindow: MyWindow) =
        static let mutable instance:MyController = null
        
        let mutable graficWindow = graficWindow
        let mutable myGpu = new MasterGPU()
        
        let mutable aspectRatio = 1.0f
        let mutable timer = new GameTimer()
        
        let mutable defaultInputLayoutDesc:InputLayoutDescription = null
        let mutable defaultRootSignatureDesc:RootSignatureDescription = null
        let mutable defaultVertexShaderDesc : ShaderDescription = null
        let mutable defaultPixelShaderDesc : ShaderDescription = null
        let mutable defaultDomainShaderDesc : ShaderDescription = null
        let mutable defaultHullShaderDesc : ShaderDescription = null
        let mutable defaultRasterizerDesc = RasterizerDescription.Default()
        let mutable defaultBlendDesc = BlendDescription.Default()
        let mutable defaultSampleDesc = SampleDescription()
        let mutable defaultTopologyType = PrimitiveTopologyType.Triangle

        let mutable objects = Dictionary<string, BaseObject>()        
        let mutable lightDir = Vector3.Zero
        let mutable lightColor:Color4  = Color4.White
        let mutable status = ControllerStatus.New
        let mutable idle = false
        let mutable rasterizationFactor = 8.0f
        let mutable tessellationFactor = 8.0f
        let mutable rasterizerDesc = RasterizerDescription.Default()
        let mutable blendDesc = BlendDescription.Default()
        let mutable startCameraPosition = Vector3.Zero
        let mutable startCameraTarget = Vector3.Zero
        let mutable materials:Dictionary<int,Material> = new Dictionary<int,Material>()

        // Shader comes from: 1.Part  2.ShaderCache
        let currentVertexShader(part:Part)  = 
            match part.Shaders.VertexShaderDesc with
            | {ShaderDescription.Use=ShaderUsage.ToBeFilledIn} ->               
                ShaderCache.GetShader(ShaderType.Vertex,part.Shape.TopologyType, part.Shape.Topology)
            | {ShaderDescription.Use=ShaderUsage.Required} -> 
                part.Shaders.VertexShaderDesc
            | {ShaderDescription.Use=ShaderUsage.NotRequired} ->
                raiseException("VertexShader muss gesetzt sein") 
            |_ -> defaultVertexShaderDesc            

        let currentPixelShader(part:Part)   =
            match part.Shaders.PixelShaderDesc with 
            | { ShaderDescription.Use=ShaderUsage.ToBeFilledIn} ->
                ShaderCache.GetShader(ShaderType.Pixel,part.Shape.TopologyType, part.Shape.Topology)
            | {ShaderDescription.Use=ShaderUsage.NotRequired} ->
                raiseException("VertexShader muss gesetzt sein")
            | {ShaderDescription.Use=ShaderUsage.Required} -> 
                part.Shaders.PixelShaderDesc                
            |_ -> defaultPixelShaderDesc  

        let currentDomainShader(part:Part)  = 
            match part.Shaders.DomainShaderDesc with 
            | { ShaderDescription.Use=ShaderUsage.ToBeFilledIn} ->
                ShaderCache.GetShader(ShaderType.Domain, part.Shape.TopologyType, part.Shape.Topology)
            | {ShaderDescription.Use=ShaderUsage.Required} -> 
                part.Shaders.DomainShaderDesc 
            | {ShaderDescription.Use=ShaderUsage.NotRequired} ->
                defaultDomainShaderDesc 
            |_ -> raiseException("DomainShader invalid use") 

        let currentHullShader(part:Part)  =
            match part.Shaders.HullShaderDesc with
            | { ShaderDescription.Use=ShaderUsage.ToBeFilledIn} ->
                ShaderCache.GetShader(ShaderType.Hull, part.Shape.TopologyType, part.Shape.Topology)
            | {ShaderDescription.Use=ShaderUsage.Required} -> 
                part.Shaders.HullShaderDesc 
            | {ShaderDescription.Use=ShaderUsage.NotRequired} ->
                 defaultHullShaderDesc 
            |_ -> raiseException("DomainShader invalid use") 

        // RootSignature 
        let currentRootSignatureDesc(part:Part, rootSignatureDesc:RootSignatureDescription) =
            if isRootSignatureDescEmpty(part.Shaders.VertexShaderDesc.RootSignature) then
                let fromCache = ShaderCache.GetShader(ShaderType.Vertex, part.Shape.TopologyType, part.Shape.Topology)
                if fromCache = null || (fromCache.Use=ShaderUsage.ToBeFilledIn) then
                    rootSignatureDesc
                else
                    fromCache.RootSignature
            else 
                part.Shaders.VertexShaderDesc.RootSignature        

        // ----------------------------------------------------------------------------------------------------
        // Instance
        // ----------------------------------------------------------------------------------------------------
        static member Instance  
            with get() = instance
            and set(value) = instance <- value

        static member CreateInstance(graficWindow: MyWindow, shaderPackage:IShaderPackage) =
            MyController.Instance <- MyController(graficWindow)
            graficWindow.Renderer <- MyController.Instance.GPU
            graficWindow.Renderer.Initialize(graficWindow)
            instance.Configure(shaderPackage)

        // ----------------------------------------------------------------------------------------------------
        //  Configuration
        // ----------------------------------------------------------------------------------------------------
        member this.Configure(shaderPackage:IShaderPackage) =
            
            // default values
            defaultInputLayoutDesc      <- shaderPackage.InputLayoutDesc 
            defaultRootSignatureDesc    <- shaderPackage.RootSignatureDesc
            defaultVertexShaderDesc     <- shaderPackage.VertexShaderDesc 
            defaultPixelShaderDesc      <- shaderPackage.PixelShaderDesc 

            defaultDomainShaderDesc     <- ShaderDescription.CreateNotRequired(ShaderType.Domain)
            defaultHullShaderDesc       <- ShaderDescription.CreateNotRequired(ShaderType.Hull)
            defaultSampleDesc           <- SampleDescription(1, 0)
            defaultRasterizerDesc       <- RasterizerDescription.Default()
            defaultBlendDesc            <- BlendDescription.Default()
            defaultTopologyType         <- PrimitiveTopologyType.Triangle

            myGpu.FrameLength <- shaderPackage.FrameLength
            myGpu.MatLength   <- shaderPackage.MatLength
            myGpu.ItemLength  <- shaderPackage.ItemLength

            myGpu.InstallPipelineProvider(
                defaultInputLayoutDesc ,      
                defaultRootSignatureDesc  , 
                defaultVertexShaderDesc ,
                defaultPixelShaderDesc ,  
                defaultDomainShaderDesc ,
                defaultHullShaderDesc  ,
                defaultSampleDesc  ,      
                defaultBlendDesc ,   
                defaultRasterizerDesc , 
                defaultTopologyType ,                
                new ShaderDefineMacros([])
            )

        member this.ConfigureWorld(origin:Vector3, halfLength:float32, makeGround, makeAxes) =
            let axes = makeAxes(halfLength) 
            if axes <> null then
                this.AddObject(axes)
            let ground = makeGround(origin, halfLength)
            if ground <> null then
                this.AddObject(ground) 

        member this.ConfigureCamera(position:Vector3, target:Vector3) = 
            CameraController.Instance.ConfigureCamera( position, target, graficWindow.AspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL, DEFAULT_ROT_STRENGTH, DEFAULT_ZOOM_STRENGTH)  

        member this.ConfigureCamera(position:Vector3, target:Vector3,  _rot_strength, _zoom_strength) = 
            CameraController.Instance.ConfigureCamera( position, target, graficWindow.AspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL, _rot_strength, _zoom_strength)  

        member this.InitDefaultCamera() =
            this.ConfigureCamera(DEFAULT_CAMERA_POS, DEFAULT_TARGET_POS)

        member this.initDefaultLight() =
            this.initLight(DEFAULT_LIGHT_DIR, DEFAULT_LIGHT_COLOR)

        member this.ConfigVision(newCameraPosition:Vector3, lightDirection:Vector3, lightColor) =
            CameraController.Instance.ConfigureCamera(newCameraPosition, startCameraTarget, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL, DEFAULT_ROT_STRENGTH, DEFAULT_ZOOM_STRENGTH) 
            this.initLight(lightDirection, lightColor) 

        member this.initLight(dir:Vector3, color: Color) = 
            lightDir <-  dir 
            lightColor <- color.ToColor4() 

        // ----------------------------------------------------------------------------------------------------
        // Member
        // ----------------------------------------------------------------------------------------------------
        member this.MyGpu  
            with get () = myGpu

        member this.AspectRatio  
            with get () = graficWindow.AspectRatio

        member this.Timer
            with get() = timer

        member this.GPU
            with get() = myGpu

        member this.IsRunning
            with get() = status
            and set(value) = status <- value

        member this.Idle
            with get() = idle
            and set(value) = idle <- value

        member this.Status
            with get() = status
            and set(value) = status <- value

        member this.Objects
            with get() = objects

        // ----------------------------------------------------------------------------------------------------
        // Material
        // ----------------------------------------------------------------------------------------------------
        member this.ClearMaterials() =
           materials.Clear()

        member this.addMaterial(material:Material) =
            if not (materials.ContainsKey(material.IDX)) then
                materials.Add(material.IDX, material)  

        member this.getMaterial(idx) =
            let success = materials.ContainsKey (idx )
            if success then 
                materials.Item(idx) 
            else null

        // ----------------------------------------------------------------------------------------------------
        // Objects im Controller verwalten
        // ----------------------------------------------------------------------------------------------------
        member this.AddObject(displayable: BaseObject) =
            if objects.ContainsKey(displayable.Name) then
                raise (ObjectDuplicateException(displayable.Name))
            else
                objects.Add(displayable.Name, displayable)
                logDebug("Add Object " + displayable.Name + " Pos " + formatVector3(displayable.Position))

        member this.AddObjects(objects:BaseObject list) =
            for object in objects do                   
                this.AddObject(object)
        
        abstract member RefreshObject:BaseObject->Unit
        default this.RefreshObject(object:BaseObject) =
            ()

        member this.GetObject(name) =
            objects.Item(name)

        member this.RemoveObject(name) =
            objects.Remove(name)

        member this.ClearObjects() = 
            objects.Clear() 

        member this.AnzahlParts(displayables:BaseObject list) =
            if displayables.IsEmpty then
                0
            else
                displayables
                |> List.map (fun disp -> disp.Display.Parts.Length)
                |> List.reduce (fun len1 len2 -> len1 + len2)

        member this.ObjectParts = 
            objects.Values
                |> Seq.collect(fun obj -> obj.Display.Parts |> List.toSeq) 

        member this.ObjectMaterials = 
            this.ObjectParts                    
                |> Seq.map(fun part -> part.Material)
                |> Seq.distinctBy(fun mat -> mat.Name)
                |> Seq.toList

        member this.ObjectTextures = 
            this.ObjectParts                   
                |> Seq.map(fun part -> part.Texture)
                |> Seq.distinctBy(fun text -> text.Name)
                |> Seq.toList

        // ----------------------------------------------------------------------------------------------------
        // Controller verwalten
        // ----------------------------------------------------------------------------------------------------
        member this.Reset() = 
            this.ClearObjects()
            this.ClearMaterials()
            myGpu.ResetAllMeshes()  

        member this.Prepare() =
            this.SetIdle()
            myGpu.ResetTextures()
            myGpu.StartInstall()

            this.InstallMaterials(DefaultMaterials)
            this.InstallMaterials(this.ObjectMaterials)
            
            this.InstallTextures(DefaultTextures)
            this.InstallTextures(this.ObjectTextures)

            myGpu.PrepareInstall(this.AnzahlParts(objects.Values |>Seq.toList), Material.MAT_COUNT) 

            for object in objects.Values do  
                for part in object.Display.Parts do                  
                    this.InstallPart(part)
            
            myGpu.FinalizeMeshCache() 
            myGpu.ExecuteInstall()
            this.Start()

        member this.InstallObjects(objects:BaseObject list) =
            this.AddObjects(objects)
            this.Prepare()

        member this.InstallMaterials(materials:Material list) =
            for material in materials do
                this.addMaterial(material)

        member this.InstallTextures(textures:Texture list) =
            for texture in textures do
                if texture.notEmpty then
                    myGpu.InstallTexture(texture)
        
        member this.InstallPart(part: Part) =
            if  myGpu.hasMesh(part.Shape.Name)  then
                ()
            else
                let meshData = part.Shape.CreateVertexData(part.Visibility)
                myGpu.InstallMesh(part.Shape.Name, meshData.Vertices, meshData.Indices, part.Shape.Topology)            

        // ---------------------------------------------------------------------------------------------------- 
        // Alle Meshes erneut schreiben
        // ---------------------------------------------------------------------------------------------------- 
        member this.RefreshShapes() = 
            myGpu.StartInstall()
            myGpu.resetMeshCache()
            for object in objects.Values do  
                for part in object.Display.Parts do 
                    let meshData = part.Shape.CreateVertexData(part.Visibility)
                    myGpu.InstallMesh(part.Shape.Name, meshData.Vertices, meshData.Indices, part.Shape.Topology) 
                    logInfo("Refresh Mesh for " + part.Shape.Name)
            myGpu.FinalizeMeshCache() 
            myGpu.ExecuteInstall()

        // ----------------------------------------------------------------------------------------------------
        // Toggle Displayable Properties
        // ----------------------------------------------------------------------------------------------------
        member this.ToggleRasterizerState() = 
            if rasterizerDesc = rasterWiredDescription then
                rasterizerDesc <- rasterSolidDescription
            else
                rasterizerDesc  <- rasterWiredDescription
            myGpu.RasterizerDesc <- rasterizerDesc

        member this.TessellationFactor
            with get() = tessellationFactor
            and set(value) = tessellationFactor <- value
        
        member this.RasterizationFactor
            with get() = rasterizationFactor
            and set(value) = rasterizationFactor <- value

        member this.RasterizerDesc
            with get() = rasterizerDesc
            and set(value) = rasterizerDesc <- value
        
        member this.BlendDesc
            with get() = blendDesc
            and set(value) = blendDesc <- value
            
        member this.StartCameraPosition
                 with get() = startCameraPosition
                 and set(value) = startCameraPosition <- value
             
        member this.StartCameraTarget
            with get() = startCameraTarget
            and set(value) = startCameraTarget <- value

        // ----------------------------------------------------------------------------------------------------
        //  GPU Steuerung
        // ----------------------------------------------------------------------------------------------------
        member this.Stop() =
            logInfo("Stop")
            status <- ControllerStatus.Terminated

        member this.Start() =
            logInfo("Start")
            status <- ControllerStatus.Running
            idle <- false

        member this.SetIdle() =
            logInfo("Idle")
            this.Idle <- true

        member this.isRunning() =
            status = ControllerStatus.Running

        member this.notIdle() =
            this.Idle = false

        member this.isIdle() =
            this.Idle = true

        abstract member Run:unit->Unit
        default this.Run() =
            logInfo("Run") 
            myGpu.Begin()
            status <- ControllerStatus.Running
            this.Timer.Reset()
            // Windows Render-Loop
            let loop = new RenderLoop(graficWindow)
            // Depth-Z sort for Transparency
            let sorted = objects.Values|> Seq.sortBy(fun disp -> - (Vector3.Distance(disp.Position, Camera.Instance.EyePosition)))  
            while loop.NextFrame() && this.isRunning() do
                if this.notIdle() then
                    logInfo("Step")
                    this.Timer.Tick()
                    myGpu.StartUpdate()
                    this.updatePerFrame()
                    myGpu.StartDraw()    
                    let mutable PartIdx = 0    
                    for object in sorted do 
                        for part in object.Display.Parts do  
                            this.updatePerPart(PartIdx, object, part)                           // Position wird pro Part gesetzt 
                            this.updatePerMaterial(part.Material, part.hasTexture())            // Material wird pro Part gesetzt
                            this.drawPerPart(PartIdx, part)
                            PartIdx <- PartIdx + 1
                    myGpu.EndDraw()
            logInfo("Terminated") 

        // ----------------------------------------------------------------------------------------------------
        // Update GPU
        // ----------------------------------------------------------------------------------------------------
        member this.updatePerFrame() =
            let frameConst = shaderFrame(tessellationFactor, lightColor, lightDir, Camera.Instance.EyePosition )
            myGpu.UpdateFrame(ref frameConst)
             
        member this.updatePerMaterial(material:Material, hasTexture:bool) = 
            let newMaterial = shaderMaterial(material, hasTexture)
            myGpu.UpdateMaterial(material.IDX, ref newMaterial)
        
        // Update 
        // Objekt-Eigenschaften
        member this.updatePerPart(idx:int, displayable:BaseObject, part:Part) = 
            logDebug("Update part " + idx.ToString() + " " + part.Shape.Name)

            let mutable scleP = Vector3.One
            let mutable rotP  = Quaternion.Identity
            let mutable tranP = Vector3.One
            displayable.LocalTransform().Decompose(&scleP, &rotP, &tranP) |> ignore
            
            let mutable sclePt = Vector3.One
            let mutable rotPt  = Quaternion.Identity
            let mutable tranPt = Vector3.One
            part.Transform.Decompose(&sclePt, &rotPt, &tranPt) |> ignore

            let _world          = displayable.World * part.Transform 
            let _view           = Camera.Instance.View
            let _proj           = Camera.Instance.Proj            
            let _eyePos         = Camera.Instance.EyePosition

            let perObject = shaderObject(_world, _view, _proj, _eyePos)

            myGpu.UpdateObject(idx, ref perObject)

        // ----------------------------------------------------------------------------------------------------
        // Draw
        // ----------------------------------------------------------------------------------------------------
        member this.drawPerPart(idx, part:Part) =  
            logDebug("Draw part " + idx.ToString() + " " + part.Shape.Name)
 
            if part.Shape.Animated then
                part.Shape.Update(timer)
                let mesh = part.Shape.CreateVertexData(part.Visibility)
                let vertices = mesh.Vertices  
                myGpu.ReplaceMesh(part.Shape.Name, vertices)  

            myGpu.UpdatePipeline(
                defaultInputLayoutDesc,
                currentRootSignatureDesc(part, defaultRootSignatureDesc),
                currentVertexShader(part),
                currentPixelShader(part),
                currentDomainShader(part),
                currentHullShader(part),
                new SampleDescription(1, 0),
                part.Shape.TopologyType,
                part.Shape.Topology,
                this.RasterizerDesc,
                blendDescriptionFromVisibility(part.Visibility),
                part.TextureIsCube()
            )

            myGpu.DrawPerObject(idx, part.Shape.Name, part.Shape.Topology, part.Material.IDX, part.Texture)

        override this.ToString() =
            "GraficController-" + graficWindow.ToString()