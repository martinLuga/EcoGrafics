namespace GltfBase
//
//  Runner.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System.Collections.Generic 

open log4net

open SharpDX
open SharpDX.Direct3D
open SharpDX.Direct3D12
open SharpDX.DXGI 
open SharpDX.Windows

open Base.GameTimer
open Base.LoggingSupport
open Base.ShaderSupport 
open GraficBase.Camera

open DirectX.D3DUtilities

open AnotherGPU
open Katalog
open ExampleShaders
open GltfSupport
open Common
open GraficWindow

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
        let mutable gpu:MyGPU = new MyGPU()

        let mutable noc:NodeKatalog = null
        let mutable mec:MeshKatalog = null
        let mutable mac:MaterialKatalog = null
        let mutable tec:TextureKatalog = null
        
        let mutable lightDir = Vector4.Zero
        let mutable frameLight : DirectionalLight = DirectionalLight(Color4.White)
        
        let mutable defaultInputLayoutDesc:InputLayoutDescription = inputLayoutDescription
        let mutable defaultRootSignatureDesc:RootSignatureDescription = rootSignatureGltfDesc
        let mutable defaultVertexShaderDesc : ShaderDescription = vertexShaderDesc
        let mutable defaultPixelShaderDesc : ShaderDescription = pixelShaderDepthDesc
        let mutable defaultDomainShaderDesc : ShaderDescription = ShaderDescription.CreateNotRequired(ShaderType.Domain)
        let mutable defaultHullShaderDesc : ShaderDescription = ShaderDescription.CreateNotRequired(ShaderType.Hull)
        let mutable defaultRasterizerDesc = new RasterizerDescription(RasterType.Wired, rasterizerStateWired)
        let mutable defaultBlendDesc =  new BlendDescription(BlendType.Opaque, blendStateOpaque)
        let mutable defaultSampleDesc = new SampleDescription(1, 0)
        let mutable defaultTopologyType = PrimitiveTopologyType.Triangle
        let mutable defaultTopology  = PrimitiveTopology.TriangleList

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

        // ----------------------------------------------------------------------------------------------------
        // Client
        // ----------------------------------------------------------------------------------------------------   
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

        member this.initLight(dir:Vector3, color: Color) = 
            lightDir <- Vector3.Transform(dir, Matrix.Identity)
            frameLight <- new DirectionalLight(color.ToColor4(), new Vector3(lightDir.X, lightDir.Y, lightDir.Z))

        // ----------------------------------------------------------------------------------------------------
        // Configuration
        // ----------------------------------------------------------------------------------------------------  
        member this.Configure() = 

            this.ConfigureGPU()

            mec <- new MeshKatalog(gpu.Device)
            mac <- new MaterialKatalog(gpu.Device)
            tec <- new TextureKatalog(gpu)
            noc <- new NodeKatalog(gpu)

        // ----------------------------------------------------------------------------------------------------
        // GPU
        // ----------------------------------------------------------------------------------------------------  
        member this.ConfigureGPU() =

            gpu.FrameLength <- D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
            gpu.MatLength   <- D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
            gpu.ItemLength  <- D3DUtil.CalcConstantBufferByteSize<ViewConstants>()

            gpu.Initialize(_graficWindow)

            gpu.InstallPipelineProvider(
                defaultInputLayoutDesc,
                defaultRootSignatureDesc,
                defaultVertexShaderDesc,
                defaultPixelShaderDesc,
                defaultDomainShaderDesc,
                defaultHullShaderDesc,
                defaultSampleDesc,
                defaultBlendDesc,
                defaultRasterizerDesc,
                defaultTopologyType
            )
        
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
                |> List.map (fun obj -> obj.LeafesCount)
                |> List.reduce (fun len1 len2 -> len1 + len2)

        member this.Prepare() =
            gpu.StartInstall()
            let anzObjects = objects.Values |> Seq.toList
            let anzahlNodes = this.AnzahlNodes(anzObjects)
            let anzMaterials = mac.Count()
            gpu.PrepareInstall(anzahlNodes, anzMaterials) 
            mec.ToGPU(gpu.DirectRecorder.CommandList) 
            tec.ToGPU() 
            gpu.ExecuteInstall() 

        member this.Reset() = 
            objects.Clear()
            mec.Reset()
            tec.Reset()

        // ----------------------------------------------------------------------------------------------------
        // Run Grafic App
        // ---------------------------------------------------------------------------------------------------- 
        member this.Run() = 

            this.GPU.Begin()
            this.SetRunning()
            this.Timer.Reset()

            // Windows Render-Loop 
            let objekts = this.Objects.Values  
            let loop = new RenderLoop(_graficWindow)
            while loop.NextFrame() &&  this.isRunning() do                     
                if this.notIdle() then
                    logInfo("Step") 
                    this.Timer.Tick()
                    this.GPU.StartUpdate()
                    this.updatePerFrame() 
                    this.GPU.StartDraw()  
                    let mutable bufferIdx = 0                 
                    for objekt in objekts do 
                        logDebug("Object " + objekt.Name)
                        objekt.GlobalTransforms()
                        let nodes = objekt.LeafNodes()
                        for node in nodes do                             
                            this.updatePerObject(node, bufferIdx)  
                            this.updatePerMaterial(objekt.Name, node, bufferIdx)
                            this.drawPerNode(objekt.Name, bufferIdx, node) 
                            bufferIdx <- bufferIdx + 1
                    this.GPU.EndDraw()

        member this.updatePerObject(adapter:NodeAdapter, partIdx) =

            let world = Matrix(adapter.Node.Matrix )
            let view  = Camera.Instance.View
            let proj  = Camera.Instance.Proj 

            let viewConstants = 
                new ViewConstants(  
                    model=world,       
                    view= view,
                    projection=proj
                )
            gpu.UpdateView(partIdx, ref viewConstants)

         member this.updatePerFrame() =
            let frameConst = 
                new FrameConstants(
                    light=frameLight
                )
            gpu.UpdateFrame(ref frameConst)
        
        member this.updatePerMaterial(_objectName, node:NodeAdapter, _bufferIdx) =         
            let mesh = mec.Mesh(_objectName, node.Node.Mesh.Value)
            let myMaterial = mac.GetMaterial(_objectName, mesh.Material)  
            let material = myMaterial.Material
            if material <> null then               
                let mutable matConst = new MaterialConstants(material)
                matConst.camera <- Camera.Instance.EyePosition
                matConst.normalScale <- 5.0f
                gpu.UpdateMaterial(_bufferIdx, ref matConst)
          
        member this.drawPerNode(_objectName, _bufferIdx:int, _node:NodeAdapter) = 
            let mesh = mec.Mesh(_objectName, _node.Node.Mesh.Value)
            let material = mesh.Material 
            let topology = PrimitiveTopology.TriangleList
            let textures = tec.GetTextures(_objectName, material)

            let vBuffer = mec.GetVertexBuffer(_objectName, _node.Node.Mesh.Value) 
            let iBuffer = mec.GetIndexBuffer(_objectName, _node.Node.Mesh.Value)
            let iCount  = mec.getIndexCount(_objectName, _node.Node.Mesh.Value)

            this.UpdatePipeline()

            logDebug ("DRAW Object     " + _objectName)
            logDebug ("DRAW IDX        " + _bufferIdx.ToString())
            logDebug ("DRAW Node       " + _node.Node.Name)
            logDebug ("DRAW Mesh       " + mesh.Mesh.ToString())
            logDebug ("DRAW Material   " + mesh.Material.ToString())
            logDebug ("DRAW Vertex anz " + iCount.ToString())
            logInfo ("  ")
            
            gpu.DrawPerObject(iCount, _bufferIdx, vBuffer , iBuffer, topology, textures)


        member this.UpdatePipeline() =
            gpu.UpdatePipeline(
                defaultInputLayoutDesc,
                defaultRootSignatureDesc ,
                defaultVertexShaderDesc ,
                defaultPixelShaderDesc,
                defaultDomainShaderDesc,
                defaultHullShaderDesc,
                defaultSampleDesc,
                defaultTopologyType,
                defaultTopology,
                defaultRasterizerDesc,
                defaultBlendDesc  
            )

