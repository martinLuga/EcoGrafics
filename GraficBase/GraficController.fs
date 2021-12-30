namespace GraficBase
//
//  ExampleApp.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
// 

open System
open System.Collections.Generic

open log4net

open SharpDX
open SharpDX.Windows
open SharpDX.Direct3D 
open SharpDX.Direct3D12
open SharpDX.Mathematics.Interop

open Base.ModelSupport
open Base.LoggingSupport
open Base.ObjectBase 
open Base.ShaderSupport
open Base.GameTimer

open DirectX
open DirectX.D3DUtilities
open DirectX.Camera

open GPUModel.MyGPU
open GPUModel.MyPipelineConfiguration

open Shader.FrameResources.CookBook

open GraficWindow
open CameraControl 

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
    type MyController(application:string, graficWindow: MyWindow) =
        static let mutable instance:MyController = null
        
        let mutable aspectRatio = 1.0f
        
        let mutable defaultVertexShaderDesc  : ShaderDescription = null
        let mutable defaultPixelShaderDesc : ShaderDescription = null
        let mutable defaultDomainShaderDesc : ShaderDescription = null
        let mutable defaultHullShaderDesc : ShaderDescription = null
        let mutable blendType = BlendType.Opaque
        let mutable defaultBlendType = BlendType.Undefinded
        let mutable defaultRasterType : RasterType = RasterType.Undefinded

        let mutable objects = Dictionary<string, BaseObject>()
        let mutable frameLight : DirectionalLight = DirectionalLight(Color4.White)
        let mutable graficWindow = graficWindow
        let mutable status = ControllerStatus.New
        let mutable lightDir = Vector4.Zero
        let mutable myGpu = new MyGPU()
        let mutable rasterizationFactor = 8.0f
        let mutable rasterizerType = RasterType.Solid
        let mutable startCameraPosition = Vector3.Zero
        let mutable startCameraTarget = Vector3.Zero
        let mutable tessellationFactor = 8.0f
        let mutable worldMatrix = Matrix.Identity
        let mutable matNr = 0
        let mutable materials:Dictionary<int,Material> = new Dictionary<int,Material>()
        let mutable materialIndices = new Dictionary<string, int>()
        
        let mutable timer = new GameTimer()

        static member Instance  
            with get() = instance
            and set(value) = instance <- value

        // ----------------------------------------------------------------------------------------------------
        // Construct
        // ----------------------------------------------------------------------------------------------------
        static member CreateInstance(application:string, graficWindow: MyWindow, configurations: MyPipelineConfiguration list, defaultConfiguration:MyPipelineConfiguration) =
            MyController.Instance <- MyController.newForConfiguration(application, graficWindow, configurations, defaultConfiguration) 

        static member newForConfiguration(application:string, graficWindow: MyWindow, configurations: MyPipelineConfiguration list, defaultConfiguration:MyPipelineConfiguration) =
            let instance = MyController(application, graficWindow)
            graficWindow.Renderer <- instance.GPU
            instance.ConfigureGPU(configurations)            
            instance.ConfigurePipeline(defaultConfiguration)
            instance

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
        // Initialize
        // ----------------------------------------------------------------------------------------------------
        member this.ConfigurePipeline(defaultConfiguration:MyPipelineConfiguration) =
            defaultVertexShaderDesc <- defaultConfiguration.VertexShaderDesc
            defaultPixelShaderDesc <- defaultConfiguration.PixelShaderDesc
            defaultDomainShaderDesc <- defaultConfiguration.DomainShaderDesc
            defaultHullShaderDesc <- defaultConfiguration.HullShaderDesc
            defaultRasterType  <- defaultConfiguration.RasterizerStateDesc.Type
            defaultBlendType   <- defaultConfiguration. BlendStateDesc.Type

        member this.ConfigureGPU(defaultConfigurations) =
            myGpu.FrameLength <- D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
            myGpu.MatLength   <- D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
            myGpu.ItemLength  <- D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()
            myGpu.Initialize(graficWindow)
            myGpu.InstallPipelineProvider(defaultConfigurations)

        member this.AspectRatio  
            with get () = graficWindow.AspectRatio

        member this.ConfigureCamera(position:Vector3, target:Vector3) = 
            CameraController.Instance.ConfigureCamera( position, target, graficWindow.AspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL, DEFAULT_STRENGTH)  

        member this.InitDefaultCamera() =
            this.ConfigureCamera(DEFAULT_CAMERA_POS, DEFAULT_TARGET_POS)

        member this.initDefaultLight() =
            this.initLight(DEFAULT_LIGHT_DIR, DEFAULT_LIGHT_COLOR)

        member this.ConfigVision(newCameraPosition:Vector3, lightDirection:Vector3, lightColor) =
            CameraController.Instance.ConfigureCamera(newCameraPosition, startCameraTarget, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL, DEFAULT_STRENGTH) 
            this.initLight(lightDirection, lightColor) 

        member this.initLight(dir:Vector3, color: Color) = 
            lightDir <- Vector3.Transform(dir, worldMatrix)
            frameLight <- new DirectionalLight(color.ToColor4(), new Vector3(lightDir.X, lightDir.Y, lightDir.Z))

        // ----------------------------------------------------------------------------------------------------
        // Material
        // ----------------------------------------------------------------------------------------------------
        member this.ClearMaterials() = 
           matNr <- 0
           materialIndices.Clear()
           materials.Clear()

        member this.AddMaterials(materials:Material list) =
            for material in materials do                   
                this.addMaterialCPU(material)                
                myGpu.UpdateMaterial(this.getMaterialGPU (material.Name, false))

        member this.addMaterialCPU(material:Material) =
            if not (materialIndices.ContainsKey(material.Name)) then
                materials.Add(matNr, material)    
                materialIndices.Add(material.Name, matNr)
                matNr <- matNr + 1

        member this.getMaterialCPU(name) =
            let mutable tempMatNr = 0
            let success = materialIndices.TryGetValue(name, &tempMatNr)
            if success then 
                materials.Item(tempMatNr) 
            else null

        member this.getMaterialGPU(name:string, hasTexture:bool) = 
            let material = this.getMaterialCPU(name)
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
            this.SetRasterizerState(defaultRasterType)
            this.SetBlendType(defaultBlendType) 

        member this.Prepare() =
            this.SetIdle()
            myGpu.StartInstall()
            myGpu.PrepareInstall(this.AnzahlParts(objects.Values |>Seq.toList))

            for object in objects.Values do  
                for part in object.Display.Parts do                  
                    this.InstallPart(part)
            
            myGpu.FinalizeInstall() 
            myGpu.FinishInstall()
            this.Start()

        member this.InstallObjects(objects:BaseObject list) =
            this.AddObjects(objects)
            this.Prepare()
        
        member this.InstallPart(part: Part) =
            if  myGpu.hasMesh(part.Shape.Name)  then
                logDebug("Mesh present for " + part.Shape.Name)
            else
                logDebug("Install Mesh for " + part.Shape.Name)
                myGpu.InstallMesh(
                    part.Shape.Name,
                    part.Shape.CreateVertexData(part.Visibility),
                    part.Shape.Topology
                )
            
            if this.getMaterialCPU(part.Material.Name) = null then
                this.addMaterialCPU(part.Material)
                logDebug("Install Material " + part.Material.Name)            
                myGpu.UpdateMaterial(this.getMaterialGPU (part.Material.Name, part.hasTexture ()))

            if part.Texture <> null then
                myGpu.InstallTexture(part.Texture.Name, part.Texture.Path)

        // ---------------------------------------------------------------------------------------------------- 
        // Alle Meshes erneut schreiben
        // ---------------------------------------------------------------------------------------------------- 
        member this.RefreshShapes() = 
            myGpu.StartInstall()
            myGpu.resetMeshCache()
            for object in objects.Values do  
                for part in object.Display.Parts do 
                    myGpu.InstallMesh(part.Shape.Name, part.Shape.CreateVertexData(part.Visibility), part.Shape.Topology) 
                    logDebug("Refresh Mesh for " + part.Shape.Name)
            myGpu.FinalizeInstall() 
            myGpu.FinishInstall()

        // ----------------------------------------------------------------------------------------------------
        // Toggle Displayable Properties
        // ----------------------------------------------------------------------------------------------------
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
            logError("Stop")
            status <- ControllerStatus.Terminated

        member this.Start() =
            logError("Start")
            status <- ControllerStatus.Running

        member this.SetIdle() =
            logError("Idle")
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
            let sorted = objects.Values|> Seq.sortBy(fun disp -> disp.Transparent) 
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
                    CameraPosition  = Camera.Instance.Position    
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

        member this.updatePerPart(idx:int, displayable:BaseObject, part:Part) = 
            logDebug("Update part " + idx.ToString() + " " + part.Shape.Name)
            let viewProjectionMatrix = Camera.Instance.ViewProj
            let world = displayable.World
            let perObjectWorld = world * Camera.Instance.World
            let newObject = 
                new ObjectConstants(
                    World=perObjectWorld,
                    WorldInverseTranspose=Matrix.Transpose(Matrix.Invert(perObjectWorld)),
                    WorldViewProjection=perObjectWorld * viewProjectionMatrix,
                    ViewProjection=viewProjectionMatrix
                )
            let perObject = Transpose(newObject)
            myGpu.UpdateObject(idx, ref perObject)

        member this.drawPerPart(idx, part:Part) =  

            logDebug("Draw part " + idx.ToString() + " " + part.Shape.Name)
            
            let matIdx = materialIndices.Item(part.Material.Name)

            let blendType           = this.blendTypeFromVisibility(part.Visibility)
               
            let pipelineConfigName  = this.configForMesh(part.Shape.TopologyType, part.Shape.Topology)

            let pShader = 
                if part.Shaders.PixelShader.IsEmpty() then
                    defaultPixelShaderDesc
                else
                    part.Shaders.PixelShader

            if part.Shape.Flexible then
                part.Shape.Update(timer)
                myGpu.ReplaceMesh( 
                    part.Shape.Name,
                    part.Shape.CreateVertexData(part.Visibility)
                ) 
            
            myGpu.UpdatePipeline(pipelineConfigName, pShader, blendDescFromType(blendType), toplogyDescFromDirectX(part.Shape.TopologyType)) 

            myGpu.DrawPerObject(idx, part.Shape.Name, part.Shape.Topology, matIdx, part.TextureName())

        // Im Rahmen der Überarbeitung des Konzepts
        // zu erneuern
        member this.configForMesh(topologyType, topology) =
            match topologyType with
            | PrimitiveTopologyType.Point
            | PrimitiveTopologyType.Line
            | PrimitiveTopologyType.Triangle    -> "Basic"
            | PrimitiveTopologyType.Patch -> 
                match topology with
                | PrimitiveTopology.PointList                   -> "TesselatedTri"
                | PrimitiveTopology.PatchListWith3ControlPoints -> "TesselatedTri"
                | PrimitiveTopology.PatchListWith4ControlPoints -> "TesselatedQuad"
                | PrimitiveTopology.PatchListWith6ControlPoints -> "TesselatedQuad"
                | _ -> raise (new Exception("PrimitiveTopologyType not implemented"))

            | _ -> raise (new Exception("PrimitiveTopologyType not implemented"))

        member this.blendTypeFromVisibility(visibility:Visibility) =
            match visibility with 
            | Visibility.Opaque       -> BlendType.Opaque
            | Visibility.Transparent  -> BlendType.Transparent
            | Visibility.Invisible    -> BlendType.Transparent

        member this.TransparenceFromVisibility(visibility:Visibility) =
            match visibility with 
            | Visibility.Opaque       -> false
            | Visibility.Transparent  -> true
            | _ -> raise (new Exception("Visibility missing"))

        override this.ToString() =
            "GraficController-" + graficWindow.ToString()