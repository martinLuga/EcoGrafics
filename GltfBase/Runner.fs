namespace GltfBase
//
//  Runner.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System
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
open GraficBase.CameraControl

open DirectX.D3DUtilities

open AnotherGPU
open Katalog
open NodeAdapter
open GraficWindow
open BaseObject
open Structures

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

        let mutable lightDir = Vector4.Zero
        let mutable frameLight: DirectionalLight = DirectionalLight(Color3.White, Vector3.One) 

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
        let mutable defaultShaderDefines = new List<ShaderDefinePBR>()

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

        static member InitLight(color:Color3, dir:Vector3) = instance.InitLight (color, dir)

        static member InitCamera(cameraPosition, cameraTarget) = instance.InitCamera(cameraPosition, cameraTarget)

        interface IDisposable with 
            member this.Dispose() =  
                (gpu:> IDisposable).Dispose() 

        member this.InitLight(color:Color3, dir:Vector3) =
            frameLight <- new DirectionalLight(color, dir)

        member this.InitCamera(cameraPosition, cameraTarget) =
            CameraController.Instance.ConfigureCamera( 
                cameraPosition, cameraTarget, 
                float32 gpu.AspectRatio, 
                DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL, DEFAULT_ROT_STRENGTH, 0.1f
            ) 

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

            MaterialKatalog.CreateInstance(gpu)
            TextureKatalog.CreateInstance(gpu)
            NodeKatalog.CreateInstance(gpu)

        // ----------------------------------------------------------------------------------------------------
        // GPU
        // ----------------------------------------------------------------------------------------------------
        member this.ConfigureGPU() =

            gpu.FrameLength <- D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
            gpu.MatLength <- D3DUtil.CalcConstantBufferByteSize<MaterialConstantsPBR>()
            gpu.ItemLength <- D3DUtil.CalcConstantBufferByteSize<ObjectConstantsPBR>()

            gpu.Initialize(_graficWindow)
            this.RefreshPipline()

        member this.RefreshPipline() =
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
            logInfo ("Prepare")
            gpu.StartInstall()
            let anzObjects = objects.Values |> Seq.toList
            let anzahlNodes = this.AnzahlNodes(anzObjects)
            let anzMaterials = MaterialKatalog.Instance.Count()
            gpu.PrepareInstall(anzahlNodes, anzMaterials)
            MeshKatalog.Instance.ToGPU(gpu.DirectRecorder.CommandList)
            TextureKatalog.Instance.ToGPU()
            gpu.ExecuteInstall()

        member this.Reset() =
            logInfo ("Reset")
            objects.Clear()
            gpu.Reset()
            TextureKatalog.Instance.Reset() 
            MaterialKatalog.Instance.Reset()  
            NodeKatalog.Instance.Reset() 
            MeshKatalog.Instance.Reset()

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
                    logDebug ("Step")
                    this.Timer.Tick()
                    this.GPU.StartUpdate()
                    this.updatePerFrame()
                    this.GPU.StartDraw()
                    let mutable bufferIdx = 0
                    for objekt in objekts do
                        logDebug ("Object " + objekt.Name)
                        objekt.GlobalTransforms()
                        let adapters = objekt.LeafNodes()
                        for _adapter in adapters do
                            if TEST_MESH_IDX = 0 || _adapter.Node.Mesh.Value = TEST_MESH_IDX then
                                this.updatePerObject (_adapter, bufferIdx)
                                this.updatePerMaterial (objekt.Name, bufferIdx, _adapter)
                                this.drawPerNode (objekt.Name, bufferIdx, _adapter)
                                bufferIdx <- bufferIdx + 1

                    this.GPU.EndDraw()

        member this.updatePerObject(adapter: NodeAdapter, partIdx) =
            let _world = Matrix(adapter.Node.Matrix)
            let _view = Camera.Instance.View
            let _proj = Camera.Instance.Proj
            let objConst = new ObjectConstantsPBR(_world, _view, _proj)
            let perObject = GltfBase.Structures.Transpose(objConst)
            gpu.UpdateView(partIdx, ref perObject)

        member this.updatePerFrame() =
            let frameConst = new FrameConstants(frameLight)
            gpu.UpdateFrame(ref frameConst)

        member this.updatePerMaterial(_objectName, _bufferIdx, node: NodeAdapter) =
            let mesh = MeshKatalog.Instance.Mesh(_objectName, node.Node.Mesh.Value)
            let myMaterial = MaterialKatalog.Instance.GetMaterial(_objectName, mesh.MatIdx)
            let matConst = new MaterialConstantsPBR(myMaterial, Camera.Instance.EyePosition)
            gpu.UpdateMaterial(_bufferIdx, ref matConst)

        member this.drawPerNode(_objectName, _bufferIdx: int, _adapter: NodeAdapter) =
            let mesh = MeshKatalog.Instance.Mesh(_objectName, _adapter.Node.Mesh.Value)
            let matIdx = mesh.MatIdx
            let meshIdx = mesh.MeshIdx
            let material = MaterialKatalog.Instance.GetMaterial(_objectName, matIdx)
            let topology = PrimitiveTopology.TriangleList
            let textures = TextureKatalog.Instance.GetTextures(_objectName, matIdx)

            let vBuffer = MeshKatalog.Instance.GetVertexBuffer(_objectName, meshIdx)
            let iBuffer = MeshKatalog.Instance.GetIndexBuffer(_objectName, meshIdx)
            let iCount  = MeshKatalog.Instance.getIndexCount (_objectName, meshIdx)

            this.UpdatePipeline(_adapter)

            logDebug ("DRAW Object     " + _objectName)
            logDebug ("DRAW BufferIDX  " + _bufferIdx.ToString())
            logDebug ("DRAW Node       " + _adapter.Node.Name)
            logDebug ("DRAW Mesh       " + meshIdx.ToString())
            logDebug ("DRAW Material   " + matIdx.ToString() + " " + material.Material.Name)
            logDebug ("DRAW Vertex anz " + iCount.ToString())

            gpu.DrawPerObject(iCount, _bufferIdx, vBuffer, iBuffer, topology, textures)
            logDebug ("  ")

        member this.UpdatePipeline(_node:NodeAdapter) =
            let _shaderDefines = _node.ShaderDefines
            let defStrings = (_shaderDefines |> Seq.map(fun sd -> string sd ) |> Seq.toList) 
            defaultPixelShaderDesc.Defines <- defStrings  
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
