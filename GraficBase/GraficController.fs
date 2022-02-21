﻿namespace GraficBase
//
//  ExampleApp.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
// 

open System.Collections.Generic

open log4net

open SharpDX
open SharpDX.Windows
open SharpDX.DXGI 
open SharpDX.Direct3D12
open SharpDX.Mathematics.Interop

open Base.ModelSupport
open Base.LoggingSupport
open Base.ObjectBase 
open Base.ShaderSupport
open Base.GameTimer

open DirectX.D3DUtilities
open DirectX.Pipeline

open GPUModel.MyGPU

open CameraControl 
open Camera
open Cache
open GraficWindow
open Structures

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
    // ----------------------------------------------------------------------------------------------------
    // Steuerung der Grafic, Versorgung der Gpu, Entgegennehmen der Abbildungsinformationen
    // ----------------------------------------------------------------------------------------------------
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
        
        let mutable aspectRatio = 1.0f
        
        let mutable defaultInputLayoutDesc:InputLayoutDescription = inputLayoutDescription
        let mutable defaultRootSignatureDesc:RootSignatureDescription = rootSignatureDesc
        let mutable defaultVertexShaderDesc : ShaderDescription = null
        let mutable defaultPixelShaderDesc : ShaderDescription = null
        let mutable defaultDomainShaderDesc : ShaderDescription = null
        let mutable defaultHullShaderDesc : ShaderDescription = null
        let mutable defaultRasterizerDesc = RasterizerDescription.Default()
        let mutable defaultBlendDesc = BlendDescription.Default()
        let mutable defaultSampleDesc = SampleDescription()
        let mutable defaultTopologyType = PrimitiveTopologyType.Triangle

        let mutable objects = Dictionary<string, BaseObject>()
        let mutable frameLight : DirectionalLight = DirectionalLight(Color4.White)
        let mutable graficWindow = graficWindow
        let mutable status = ControllerStatus.New
        let mutable lightDir = Vector4.Zero
        let mutable myGpu = new MasterGPU()
        let mutable rasterizationFactor = 8.0f
        let mutable tessellationFactor = 8.0f
        let mutable rasterizerDesc = RasterizerDescription.Default()
        let mutable blendDesc = BlendDescription.Default()
        let mutable startCameraPosition = Vector3.Zero
        let mutable startCameraTarget = Vector3.Zero
        let mutable worldMatrix = Matrix.Identity
        let mutable matNr = 0
        let mutable materials:Dictionary<int,Material> = new Dictionary<int,Material>()
        let mutable materialIndices = new Dictionary<string, int>()

        let currentVertexShader(part:Part)  = 
            match part.Shaders.VertexShaderDesc with
            | {ShaderDescription.Use=ShaderUsage.ToBeFilledIn} ->               
                ShaderCache.GetShader(ShaderType.Vertex,part.Shape.TopologyType, part.Shape.Topology)
            | {ShaderDescription.Use=ShaderUsage.Required} -> 
                part.Shaders.VertexShaderDesc
            | {ShaderDescription.Use=ShaderUsage.NotRequired} ->
                raise (System.Exception("VertexShader muss gesetzt sein"))
            |_ -> defaultVertexShaderDesc            

        let currentPixelShader(part:Part)   =
            match part.Shaders.PixelShaderDesc with 
            | { ShaderDescription.Use=ShaderUsage.ToBeFilledIn} ->
                ShaderCache.GetShader(ShaderType.Pixel,part.Shape.TopologyType, part.Shape.Topology)
            | {ShaderDescription.Use=ShaderUsage.NotRequired} ->
                raise (System.Exception("VertexShader muss gesetzt sein"))
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
            |_ -> raise (System.Exception("DomainShader invalid use"))

        let currentHullShader(part:Part)  =
            match part.Shaders.HullShaderDesc with
            | { ShaderDescription.Use=ShaderUsage.ToBeFilledIn} ->
                ShaderCache.GetShader(ShaderType.Hull, part.Shape.TopologyType, part.Shape.Topology)
            | {ShaderDescription.Use=ShaderUsage.Required} -> 
                part.Shaders.HullShaderDesc 
            | {ShaderDescription.Use=ShaderUsage.NotRequired} ->
                 defaultHullShaderDesc 
            |_ -> raise (System.Exception("DomainShader invalid use"))

        // RootSignature 
        let currentRootSignatureDesc(part:Part, defaultRootSignatureDesc:RootSignatureDescription) =
            if isRootSignatureDescEmpty(part.Shaders.VertexShaderDesc.RootSignature) then
                let fromCache = ShaderCache.GetShader(ShaderType.Vertex, part.Shape.TopologyType, part.Shape.Topology)
                if fromCache = null || (fromCache.Use=ShaderUsage.ToBeFilledIn) then
                    defaultRootSignatureDesc
                else
                    fromCache.RootSignature
            else 
                part.Shaders.VertexShaderDesc.RootSignature
        
        let mutable timer = new GameTimer()

        static member Instance  
            with get() = instance
            and set(value) = instance <- value

        // ----------------------------------------------------------------------------------------------------
        // Construct
        // ----------------------------------------------------------------------------------------------------
        static member CreateInstance
            (
                graficWindow: MyWindow,
                inputLayoutDescription:InputLayoutDescription,
                rootSignatureDesc:RootSignatureDescription,
                vertexShaderDesc:ShaderDescription,
                pixelShaderDepthDesc:ShaderDescription
            ) =
            MyController.Instance <- MyController(graficWindow)
            graficWindow.Renderer <- MyController.Instance.GPU

            instance.Configure(

                inputLayoutDescription,
                rootSignatureDesc,
                vertexShaderDesc,
                pixelShaderDepthDesc
            )

            instance

        // ----------------------------------------------------------------------------------------------------
        // Initialize (Default Configuration)
        // ----------------------------------------------------------------------------------------------------
        member this.Configure
            (
                _inputLayoutDescription,
                _rootSignatureDesc,
                _vertexShaderDescription,
                _pixelShaderDescription
            ) =
            defaultInputLayoutDesc <- inputLayoutDescription
            defaultRootSignatureDesc <- rootSignatureDesc
            defaultVertexShaderDesc <-
                ShaderDescription(
                    ShaderType.Vertex,
                    "shader",
                    "VS",
                    "VSMain",
                    "vs_5_1", 
                    ShaderUsage.Required,
                    rootSignatureDesc
                )

            defaultPixelShaderDesc <-
                ShaderDescription(
                    ShaderType.Pixel,
                    "shader",
                    "PhongPS",
                    "PSMain",
                    "ps_5_1",
                    ShaderUsage.Required,
                    rootSignatureDesc 
                )

            defaultDomainShaderDesc <- ShaderDescription.CreateNotRequired(ShaderType.Domain)
            defaultHullShaderDesc <- ShaderDescription.CreateNotRequired(ShaderType.Hull)
            defaultSampleDesc <- SampleDescription(1, 0)
            defaultRasterizerDesc <- RasterizerDescription.Default()
            defaultBlendDesc <- BlendDescription.Default()
            defaultTopologyType <- PrimitiveTopologyType.Triangle

            this.ConfigureGPU()

            myGpu.Initialize(graficWindow)

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
                defaultTopologyType     
            )

        abstract member ConfigureGPU:Unit -> Unit 
        default this.ConfigureGPU() =
            myGpu.FrameLength <- D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
            myGpu.MatLength   <- D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
            myGpu.ItemLength  <- D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()

        member this.ConfigureWorld(origin:Vector3, halfLength:float32, makeGround, makeAxes) =
            let axes = makeAxes(halfLength) 
            if axes <> null then
                this.AddObject(axes)
            let ground = makeGround(origin, halfLength * 2.0f)
            if ground <> null then
                this.AddObject(ground) 

        member this.ConfigureCamera(position:Vector3, target:Vector3) = 
            CameraController.Instance.ConfigureCamera( position, target, graficWindow.AspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL, DEFAULT_ROT_STRENGTH, DEFAULT_ZOOM_STRENGTH)  

        member this.InitDefaultCamera() =
            this.ConfigureCamera(DEFAULT_CAMERA_POS, DEFAULT_TARGET_POS)

        member this.initDefaultLight() =
            this.initLight(DEFAULT_LIGHT_DIR, DEFAULT_LIGHT_COLOR)

        member this.ConfigVision(newCameraPosition:Vector3, lightDirection:Vector3, lightColor) =
            CameraController.Instance.ConfigureCamera(newCameraPosition, startCameraTarget, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL, DEFAULT_ROT_STRENGTH, DEFAULT_ZOOM_STRENGTH) 
            this.initLight(lightDirection, lightColor) 

        member this.initLight(dir:Vector3, color: Color) = 
            lightDir <- Vector3.Transform(dir, worldMatrix)
            frameLight <- new DirectionalLight(color.ToColor4(), new Vector3(lightDir.X, lightDir.Y, lightDir.Z))

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

        member this.Objects
            with get() = objects

        // ----------------------------------------------------------------------------------------------------
        // Material
        // ----------------------------------------------------------------------------------------------------
        member this.ClearMaterials() = 
           matNr <- 0
           materialIndices.Clear()
           materials.Clear()

        member this.addMaterialCPU(material:Material) =
            if not (materialIndices.ContainsKey(material.Name)) then
                materials.Add(matNr, material)    
                materialIndices.Add(material.Name, matNr)
                matNr <- matNr + 1

        member this.getMaterial(name) =
            let mutable tempMatNr = 0
            let success = materialIndices.TryGetValue(name, &tempMatNr)
            if success then 
                materials.Item(tempMatNr) 
            else null

        member this.getMaterialConstants(name:string, hasTexture:bool) = 
            let material = this.getMaterial(name)
            if material = null then
                raise (ObjectNotFoundException("Invalid Materialname ")) 
            let mutable newMaterial = 
                new MaterialConstants( 
                    Ambient = material.Ambient,
                    Diffuse = material.Diffuse,
                    Specular = material.Specular,
                    SpecularPower = material.SpecularPower,
                    Emissive = material.Emissive,
                    HasTexture = RawBool(hasTexture), 
                    UVTransform = Matrix.Identity
                )
            let matIdx = materialIndices.Item(material.Name)
            matIdx, ref newMaterial

        // ----------------------------------------------------------------------------------------------------
        // Objects im Controller verwalten
        // ----------------------------------------------------------------------------------------------------
        member this.AddObject(displayable: BaseObject) =
            if objects.ContainsKey(displayable.Name) then
                raise (ObjectDuplicateException(displayable.Name))
            else
                objects.Add(displayable.Name, displayable)
                logDebug("Install Object " + displayable.Name + " at Position " + displayable.Position.ToString())

        member this.AddObjects(objects:BaseObject list) =
            for object in objects do                   
                this.AddObject(object)

        member this.RefreshObject(object:BaseObject) =
            this.Reset()
            this.AddObject(object)
            this.Prepare()

        member this.GetObject(name) =
            objects.Item(name)

        member this.ClearObjects() = 
            objects.Clear() 

        member this.AnzahlParts(displayables:BaseObject list) =
            if displayables.IsEmpty then
                0
            else
                displayables
                |> List.map (fun disp -> disp.Display.Parts.Length)
                |> List.reduce (fun len1 len2 -> len1 + len2)

        // ----------------------------------------------------------------------------------------------------
        // Controller verwalten
        // ----------------------------------------------------------------------------------------------------
        member this.Reset() = 
            this.ClearObjects()
            this.ClearMaterials()
            myGpu.ResetAllMeshes()  

        member this.Prepare() =
            this.SetIdle()
            myGpu.StartInstall()
            myGpu.PrepareInstall(this.AnzahlParts(objects.Values |>Seq.toList), this.AnzahlParts(objects.Values |>Seq.toList))

            for object in objects.Values do  
                for part in object.Display.Parts do                  
                    this.InstallPart(part)
            
            myGpu.FinalizeMeshCache() 
            myGpu.ExecuteInstall()
            this.Start()

        member this.InstallObjects(objects:BaseObject list) =
            this.AddObjects(objects)
            this.Prepare()
        
        member this.InstallPart(part: Part) =

            // Vertex Data
            if  myGpu.hasMesh(part.Shape.Name)  then
                ()
            else
                let meshData = part.Shape.CreateVertexData(part.Visibility)
                myGpu.InstallMesh(part.Shape.Name, meshData.Vertices, meshData.Indices, part.Shape.Topology)
            
            // Material Data
            if this.getMaterial(part.Material.Name) = null then
                this.addMaterialCPU(part.Material)
                logDebug("Install Material " + part.Material.Name)     
            
            // Texture Data
            if part.Texture <> null then
                myGpu.InstallTexture(part.Texture.Name, part.Texture.Path, part.Texture.IsCube) 

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
                    logDebug("Refresh Mesh for " + part.Shape.Name)
            //myGpu.FinalizeMeshCache() 
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

        member this.SetIdle() =
            logInfo("Idle")
            status <- ControllerStatus.Idle

        member this.isRunning() =
            status = ControllerStatus.Running

        member this.notIdle() =
            status <> ControllerStatus.Idle

        member this.isIdle() =
            status = ControllerStatus.Idle

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
            let frameConst = 
                new FrameConstants(
                    TessellationFactor = tessellationFactor, 
                    Light = frameLight,
                    CameraPosition  = Camera.Instance.EyePosition    
                )
            myGpu.UpdateFrame(ref frameConst)

        member this.updatePerMaterial(material:Material, hasTexture:bool) = 
            let newMaterial = 
                new MaterialConstants( 
                    Ambient = material.Ambient,
                    Diffuse = material.Diffuse,
                    Specular = material.Specular,
                    SpecularPower = material.SpecularPower,
                    Emissive = material.Emissive,
                    HasTexture = RawBool(hasTexture), 
                    UVTransform = Matrix.Identity
                )
            let matIdx = materialIndices.Item(material.Name) 
            myGpu.UpdateMaterial(matIdx, ref newMaterial)
        
        // Update 
        // Objekt-Eigenschaften
        member this.updatePerPart(idx:int, displayable:BaseObject, part:Part) = 
            logDebug("Update part " + idx.ToString() + " " + part.Shape.Name)

            let _world          = displayable.World
            let _view           = Camera.Instance.View
            let _proj           = Camera.Instance.Proj
            let _invView        = Matrix.Invert(_view)
            let _invProj        = Matrix.Invert(_proj) 
            let _viewProj       = _view * _proj
            let _invViewProj    = Matrix.Invert(_viewProj) 
            let _eyePos         = Camera.Instance.EyePosition
 
            let objConst =                
                new ObjectConstants(
                    World = _world,
                    View = Matrix.Transpose(_view),
                    InvView = Matrix.Transpose(_invView), 
                    Proj = Matrix.Transpose(_proj) ,
                    InvProj = Matrix.Transpose(_invProj) ,
                    ViewProj = Matrix.Transpose(_viewProj) ,
                    InvViewProj = Matrix.Transpose(_invViewProj), 
                    WorldViewProjection= _world * _viewProj,
                    WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(_world)),
                    ViewProjection = _viewProj,
                    EyePosW = _eyePos  
                )

            let perObject = Transpose(objConst)

            myGpu.UpdateObject(idx, ref perObject)

        member this.drawPerPart(idx, part:Part) =  
            logDebug("Draw part " + idx.ToString() + " " + part.Shape.Name)
            let matIdx = materialIndices.Item(part.Material.Name)

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
                blendDescriptionFromVisibility(part.Visibility)
            )

            myGpu.DrawPerObject(idx, part.Shape.Name, part.Shape.Topology, matIdx, part.TextureName())

        override this.ToString() =
            "GraficController-" + graficWindow.ToString()