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
open Common
open GraficWindow
open BaseObject

type ObjectConstants    = GraficBase.Structures.ObjectConstants
type MaterialConstants  = GltfBase.Structures.MaterialConstantsPBR
type FrameConstants     = GltfBase.Structures.FrameConstants
type DirectionalLight   = GltfBase.Structures.DirectionalLight

// ----------------------------------------------------------------------------------------------------
// Runner Prozess über der GPU
// ----------------------------------------------------------------------------------------------------
module Running =

    let logger = LogManager.GetLogger("Runner")
    let logDebug = Debug(logger)
    let logInfo = Info(logger)
    let logError = Error(logger)
    let logWarn = Warn(logger)

    type RunnerStatus =
        | New
        | Prepared
        | Running
        | Idle
        | Terminated

    // ----------------------------------------------------------------------------------------------------
    // Runner mit VGltf
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type Runner(_graficWindow: MyWindow) =
        let mutable timer = new GameTimer()
        let mutable objects = new Dictionary<string, Objekt>()
        let mutable status = RunnerStatus.New
        let mutable gpu: MyGPU = new MyGPU()

        let mutable noc: NodeKatalog = null
        let mutable mec: MeshKatalog = null
        let mutable mac: MaterialKatalog = null
        let mutable tec: TextureKatalog = null

        let mutable lightDir = Vector4.Zero
        let mutable frameLight: DirectionalLight = DirectionalLight(Color4.White) 

        let mutable defaultInputLayoutDesc: InputLayoutDescription = null
        let mutable defaultRootSignatureDesc: RootSignatureDescription = null
        let mutable defaultVertexShaderDesc: ShaderDescription = null
        let mutable defaultPixelShaderDesc: ShaderDescription = null
        let mutable defaultDomainShaderDesc: ShaderDescription = null
        let mutable defaultHullShaderDesc: ShaderDescription = null
        let mutable defaultRasterizerDesc: RasterizerDescription = null
        let mutable defaultBlendDesc = BlendDescription.Default()
        let mutable defaultSampleDesc = SampleDescription()
        let mutable defaultTopologyType = PrimitiveTopologyType.Triangle
        let mutable defaultTopology = PrimitiveTopology.TriangleList

        // ----------------------------------------------------------------------------------------------------
        // Singleton
        // ----------------------------------------------------------------------------------------------------
        static let mutable instance: Runner = null

        static member Instance
            with get () = instance
            and set (value) = instance <- value

        static member CreateInstance
            (
                _graficWindow: MyWindow,
                _inputLayoutDescription: InputLayoutDescription,
                _rootSignatureDesc: RootSignatureDescription,
                _vertexShaderDescription: ShaderDescription,
                _pixelShaderDescription: ShaderDescription
            ) =
            Runner.Instance <- new Runner(_graficWindow)

            instance.Configure(
                _inputLayoutDescription,
                _rootSignatureDesc,
                _vertexShaderDescription,
                _pixelShaderDescription
            )

            _graficWindow.Renderer <- instance.GPU
            instance

        // ----------------------------------------------------------------------------------------------------
        // Client
        // ----------------------------------------------------------------------------------------------------
        static member AddObject(objekt: Objekt) = instance.AddObject(objekt)

        static member GetObjects() : Objekt IEnumerable =
            let result: Dictionary<string, Objekt> = instance.Objects
            result.Values

        static member Run() = instance.Run()

        static member Prepare() = instance.Prepare()

        static member Reset() = instance.Reset()

        static member InitLight(dir: Vector3, color: Color) = instance.initLight (dir, color)

        member this.initLight(dir: Vector3, color: Color) =
            lightDir <- Vector3.Transform(dir, Matrix.Identity)
            frameLight <- new DirectionalLight(color.ToColor4(), new Vector3(lightDir.X, lightDir.Y, lightDir.Z))

        // ----------------------------------------------------------------------------------------------------
        // Toggle Displayable Properties
        // ----------------------------------------------------------------------------------------------------
        member this.ToggleRasterizerState() = 
            if defaultRasterizerDesc = rasterWiredDescription then
                defaultRasterizerDesc <- rasterSolidDescription
            else
                defaultRasterizerDesc  <- rasterWiredDescription
            gpu.RasterizerDesc <- defaultRasterizerDesc

        // ----------------------------------------------------------------------------------------------------
        // Configuration
        // ----------------------------------------------------------------------------------------------------
        member this.Configure
            (
                _inputLayoutDescription,
                _rootSignatureDesc,
                _vertexShaderDescription,
                _pixelShaderDescription
            ) =

            defaultInputLayoutDesc <- _inputLayoutDescription
            defaultRootSignatureDesc <- _rootSignatureDesc
            defaultVertexShaderDesc <- _vertexShaderDescription
            defaultPixelShaderDesc <- _pixelShaderDescription
            defaultDomainShaderDesc <- ShaderDescription.CreateNotRequired(ShaderType.Domain)
            defaultHullShaderDesc <- ShaderDescription.CreateNotRequired(ShaderType.Hull)
            defaultSampleDesc <- SampleDescription(1, 0)
            defaultRasterizerDesc <- rasterSolidDescription
            defaultBlendDesc <- BlendDescription.Default()
            defaultTopologyType <- PrimitiveTopologyType.Triangle

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
            gpu.MatLength <- D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
            gpu.ItemLength <- D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()

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
        member this.GPU = gpu

        member this.NodeKatalog = noc

        member this.MeshKatalog = mec

        member this.MaterialKatalog = mac

        member this.TextureKatalog = tec

        member this.Status = status

        member this.Timer = timer

        member this.SetRunning() = status <- RunnerStatus.Running

        member this.isRunning() = status = RunnerStatus.Running

        member this.notIdle() = status <> RunnerStatus.Idle

        member this.Objects = objects

        // ----------------------------------------------------------------------------------------------------
        // Methods
        // ----------------------------------------------------------------------------------------------------
        member this.AddObject(objekt: Objekt) = objects.Add(objekt.Name, objekt)

        member this.AnzahlNodes(objekte: Objekt list) =
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
            mac.Reset()  
            noc.Reset() 

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

            while loop.NextFrame() && this.isRunning () do
                if this.notIdle () then
                    logInfo ("Step")
                    this.Timer.Tick()
                    this.GPU.StartUpdate()
                    this.updatePerFrame ()
                    this.GPU.StartDraw()
                    let mutable bufferIdx = 0
                    for objekt in objekts do
                        logDebug ("Object " + objekt.Name)
                        objekt.GlobalTransforms()
                        let nodes = objekt.LeafNodes()
                        for node in nodes do
                            this.updatePerObject (node, bufferIdx)
                            this.updatePerMaterial (objekt.Name, bufferIdx, node)
                            this.drawPerNode (objekt.Name, bufferIdx, node)
                            bufferIdx <- bufferIdx + 1

                    this.GPU.EndDraw()

        member this.updatePerObject(adapter: NodeAdapter, partIdx) =

            let _world = Matrix(adapter.Node.Matrix)
            let _view = Camera.Instance.View
            let _proj = Camera.Instance.Proj
            let _invView = Matrix.Invert(_view)
            let _invProj = Matrix.Invert(_proj)
            let _viewProj = _view * _proj
            let _invViewProj = Matrix.Invert(_viewProj)
            let _eyePos = Camera.Instance.EyePosition

            let objConst =
                new ObjectConstants(
                    World = _world,
                    View = Matrix.Transpose(_view),
                    InvView = Matrix.Transpose(_invView),
                    Proj = Matrix.Transpose(_proj),
                    InvProj = Matrix.Transpose(_invProj),
                    ViewProj = Matrix.Transpose(_viewProj),
                    InvViewProj = Matrix.Transpose(_invViewProj),
                    WorldViewProjection = _world * _viewProj,
                    WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(_world)),
                    ViewProjection = _viewProj,
                    EyePosW = _eyePos
                )

            let perObject = GraficBase.Structures.Transpose(objConst)

            gpu.UpdateView(partIdx, ref perObject)

        member this.updatePerFrame() =
            let frameConst = 
                new FrameConstants( 
                    Light = frameLight 
                )
            gpu.UpdateFrame(ref frameConst)

        member this.updatePerMaterial(_objectName, _bufferIdx, node: NodeAdapter) =
            let mesh = mec.Mesh(_objectName, node.Node.Mesh.Value)
            let myMaterial = mac.GetMaterial(_objectName, mesh.Material)
            let mutable matConst = new MaterialConstants(myMaterial)
            matConst.camera  <- Camera.Instance.EyePosition
            gpu.UpdateMaterial(_bufferIdx, ref matConst)

        member this.drawPerNode(_objectName, _bufferIdx: int, _node: NodeAdapter) =
            let mesh = mec.Mesh(_objectName, _node.Node.Mesh.Value)
            let material = mesh.Material
            let topology = PrimitiveTopology.TriangleList
            let textures = tec.GetTextures(_objectName, material)

            let vBuffer = mec.GetVertexBuffer(_objectName, _node.Node.Mesh.Value)
            let iBuffer = mec.GetIndexBuffer(_objectName, _node.Node.Mesh.Value)
            let iCount  = mec.getIndexCount (_objectName, _node.Node.Mesh.Value)

            this.UpdatePipeline()

            logDebug ("DRAW Object     " + _objectName)
            logDebug ("DRAW IDX        " + _bufferIdx.ToString())
            logDebug ("DRAW Node       " + _node.Node.Name)
            logDebug ("DRAW Mesh       " + mesh.Mesh.ToString())
            logDebug ("DRAW Material   " + mesh.Material.ToString())
            logDebug ("DRAW Vertex anz " + iCount.ToString())
            logInfo  ("  ")

            gpu.DrawPerObject(iCount, _bufferIdx, vBuffer, iBuffer, topology, textures)


        member this.UpdatePipeline() =
            gpu.UpdatePipeline(
                defaultInputLayoutDesc,
                defaultRootSignatureDesc,
                defaultVertexShaderDesc,
                defaultPixelShaderDesc,
                defaultDomainShaderDesc,
                defaultHullShaderDesc,
                defaultSampleDesc,
                defaultTopologyType,
                defaultTopology,
                defaultRasterizerDesc,
                defaultBlendDesc
            )
