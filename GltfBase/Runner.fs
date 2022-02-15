namespace GltfBase
//
//  Runner.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System.Collections.Generic 
open System

open log4net

open SharpDX
open SharpDX.Direct3D
open SharpDX.Direct3D12
open SharpDX.DXGI 
open SharpDX.Windows

open Base.GameTimer
open Base.LoggingSupport
open Base.ShaderSupport
open Base.GeometryUtils
open GraficBase.GraficWindow
open GraficBase.Camera

open DirectX.D3DUtilities

open ModernGPU
open Katalog
open ExampleShaders
open GltfSupport
open Common

// ----------------------------------------------------------------------------------------------------
// Runner Prozess über der GPU
// ---------------------------------------------------------------------------------------------------- 
module Running =

    open ModelSupport

    let logger = LogManager.GetLogger("Runner")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger) 
    let logError = Error(logger)
    let logWarn  = Warn(logger)

    type RunnerStatus = | New | Prepared | Running | Idle | Terminated

    // ----------------------------------------------------------------------------------------------------
    // Runner mit VGltf
    // ---------------------------------------------------------------------------------------------------- 
    [<AllowNullLiteral>]
    type Runner(_graficWindow: MyWindow) = 
        let mutable timer = new GameTimer()
        let mutable objects = new Dictionary<string, Objekt>()
        let mutable status = RunnerStatus.New
        let mutable gpu:MyModernGPU = new MyModernGPU()

        let mutable noc:NodeKatalog = null
        let mutable mec:MeshKatalog = null
        let mutable mac:MaterialKatalog = null
        let mutable tec:TextureKatalog = null
        
        let mutable lightDir = Vector4.Zero
        let mutable frameLight : DirectionalLight = DirectionalLight(Color4.White)
        
        let mutable defaultInputLayoutDesc:InputLayoutDescription = inputLayoutDescription
        let mutable defaultRootSignatureDesc:RootSignatureDescription = rootSignatureGltfDesc
        let mutable defaultVertexShaderDesc : ShaderDescription = null
        let mutable defaultPixelShaderDesc : ShaderDescription = null
        let mutable defaultDomainShaderDesc : ShaderDescription = null
        let mutable defaultHullShaderDesc : ShaderDescription = null
        let mutable defaultRasterizerDesc = new RasterizerDescription(RasterType.Wired, rasterizerStateWired)
        let mutable defaultBlendDesc = BlendDescription.Default()
        let mutable defaultSampleDesc = SampleDescription()
        let mutable defaultTopologyType = PrimitiveTopologyType.Triangle

        // ----------------------------------------------------------------------------------------------------
        // Singleton
        // ----------------------------------------------------------------------------------------------------         
        static let mutable instance:Runner = null 
        static member Instance 
            with get() = instance
            and set(value) = instance <- value

        static member CreateInstance(_graficWindow: MyWindow ) =
            Runner.Instance <- new Runner (_graficWindow)
            instance.Configure()
            _graficWindow.Renderer <- instance.GPU
            instance

        static member AddObject(objekt:Objekt) =
            instance.AddObject(objekt)

        static member GetObjects():Objekt IEnumerable =
            let result:Dictionary<string, Objekt> = instance.Objects
            result.Values

        static member Run() =
            instance.Run()

        static member Prepare() =
            instance.Prepare()

        static member Reset() =
            instance.Reset()

        static member InitLight(dir:Vector3, color: Color)  =
            instance.initLight(dir, color)

        // ----------------------------------------------------------------------------------------------------
        // Configuration
        // ----------------------------------------------------------------------------------------------------  
        member this.Configure() = 
            defaultInputLayoutDesc      <- inputLayoutDescription
            defaultRootSignatureDesc    <- rootSignatureGltfDesc
            defaultVertexShaderDesc     <- vertexShaderPBRDesc
            defaultPixelShaderDesc      <- pixelShaderPBRDesc
            defaultDomainShaderDesc     <- ShaderDescription.CreateNotRequired(ShaderType.Domain)
            defaultHullShaderDesc       <- ShaderDescription.CreateNotRequired(ShaderType.Hull)
            defaultSampleDesc           <- SampleDescription(1, 0)
            defaultRasterizerDesc       <- RasterizerDescription.Default()
            defaultBlendDesc            <- BlendDescription.Default()
            defaultTopologyType         <- PrimitiveTopologyType.Triangle
      
            gpu.FrameLength             <- D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
            gpu.MatLength               <- D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
            gpu.ItemLength              <- D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()

            gpu.Initialize(_graficWindow)

            gpu.InstallPipelineState(
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

            mec <- new MeshKatalog(gpu.Device)
            mac <- new MaterialKatalog(gpu.Device)
            tec <- new TextureKatalog(gpu)
            noc <- new NodeKatalog(gpu)

        member this.ConfigureGPU() =
            gpu.FrameLength <- D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
            gpu.MatLength   <- D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
            gpu.ItemLength  <- D3DUtil.CalcConstantBufferByteSize<ViewConstants>()
            gpu.Initialize(_graficWindow)
        
        member this.initLight(dir:Vector3, color: Color) = 
            lightDir <- Vector3.Transform(dir, Matrix.Identity)
            frameLight <- new DirectionalLight(color.ToColor4(), new Vector3(lightDir.X, lightDir.Y, lightDir.Z))

        // ----------------------------------------------------------------------------------------------------
        // Member
        // ---------------------------------------------------------------------------------------------------- 
        member this.GPU
            with get() = gpu

        member this.NodeKatalog
            with get() = noc 

        member this.MeshKatalog
            with get() = mec 
        
        member this.MaterialKatalog 
            with get() = mac

        member this.TextureKatalog 
            with get() = tec

        member this.Status
            with get() = status

        member this.Timer
            with get() = timer

        member this.SetRunning() =
            status <- RunnerStatus.Running

        member this.isRunning() =
            status = RunnerStatus.Running

        member this.notIdle() =
            status <> RunnerStatus.Idle

        member this.Objects
            with get() = objects

        // ----------------------------------------------------------------------------------------------------
        // Methods
        // ---------------------------------------------------------------------------------------------------- 
        member this.AddObject(objekt:Objekt) =
            objects.Add(objekt.Name, objekt)

        member this.AnzahlNodes(objekte:Objekt list) =
            if objekte.IsEmpty then
                0
            else
                objekte
                |> List.map (fun obj -> obj.NodeCount)
                |> List.reduce (fun len1 len2 -> len1 + len2)

        member this.Prepare() =
            gpu.StartInstall()
            let objectvals = objects.Values |> Seq.toList
            gpu.PrepareInstall(this.AnzahlNodes(objectvals), mac.Count()) 
            mec.ToGPU(gpu.DirectRecorder.CommandList) 
            tec.ToGPU() 
            gpu.ExecuteInstall() 

        member this.Reset() = 
            objects.Clear()
            mec.Reset()
            tec.Reset()

        member this.Run() = 
            logInfo("Run") 
            this.GPU.Begin()
            this.SetRunning()
            this.Timer.Reset()

            // Windows Render-Loop 
            let objekts = this.Objects.Values  
            let loop = new RenderLoop(_graficWindow)
            while loop.NextFrame() &&  this.isRunning() do                     
                if this.notIdle() then
                    logError("Step") 
                    this.Timer.Tick()
                    this.GPU.StartUpdate()
                    this.updatePerFrame() 
                    this.GPU.StartDraw()                 
                    for objekt in objekts do 
                        logInfo("Object " + objekt.Name)
                        objekt.GlobalTransforms()
                        let nodes = objekt.LeafNodes()  
                        for adapter in nodes do 
                            logDebug("Node " + adapter.Node.Name)                            
                            this.updateView(adapter) 
                            if adapter.Node.Mesh.HasValue then
                                let mesh = mec.Mesh(objekt.Name, adapter.Node.Mesh.Value) 
                                this.updatePerMaterial(objekt.Name, mesh.Material)
                                this.drawPerObject(objekt.Name, adapter, mesh.Mesh, mesh.Material, PrimitiveTopology.TriangleList, tec.GetTextures()) 
                    this.GPU.EndDraw()
            logInfo("Terminated")  

        member this.updateView(adapter:NodeAdapter) =
            let node = adapter.Node 
            let transMatrix = createTranslationMatrix(node.Translation) 
            let rotMatrix = createRotationMatrix(node.Rotation)
            let scaleMatrix = createScaleMatrix(node.Scale)


            let world = transMatrix * rotMatrix * scaleMatrix 

            let _world          = world
            let _view           = Camera.Instance.View
            let _proj           = Camera.Instance.Proj
            let _invView        = Matrix.Invert(_view)
            let _invProj        = Matrix.Invert(_proj) 
            let _viewProj       = _view * _proj
            let _invViewProj    = Matrix.Invert(_viewProj) 
            let _eyePos         = Camera.Instance.EyePosition
            let viewConstants = 
                new ViewConstants(  
                    model=_world,       
                    view= _invView ,
                    projection=_invProj
                )
            gpu.UpdateView(adapter.Idx, ref viewConstants)

         member this.updatePerFrame() =
            let frameConst = 
                new FrameConstants(
                    light=frameLight
                )
            gpu.UpdateFrame(ref frameConst)
        
        member this.updatePerMaterial(_objectName, idx:Nullable<int>) = 
            if idx.HasValue && idx.Value >= 0 then
                let material = mac.GetMaterial(_objectName, idx.Value)  
                if material <> null then
                    logDebug("Material " + idx.ToString() + " " + material.Name)                
                    let matConst = new MaterialConstants(material)
                    gpu.UpdateMaterial(idx.Value, ref matConst)
          
        member this.drawPerObject(_objectName, _adapter:NodeAdapter, _mesh, _material, _topology, _textures) =             

            let vBuffer = mec.GetVertexBuffer(_objectName, _mesh) 
            let iBuffer = mec.GetIndexBuffer(_objectName, _mesh)
            let iCount  = mec.getIndexCount(_objectName, _mesh)
            
            gpu.DrawPerObject(iCount, _adapter.Idx, _material, vBuffer , iBuffer, _topology, _textures)

