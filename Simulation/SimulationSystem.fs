namespace Simulation
//
//  SimulationSystem.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 

open System.Threading.Tasks
open System.Threading

open log4net
open SharpDX

open ApplicationBase.WindowControl
open ApplicationBase.GraficSystem
open ApplicationBase.MoveableObject

open Base.Logging
open DirectX.D3DUtilities 

open GPUModel.MyGPU
open GPUModel.MyGraphicWindow
open GPUModel.MyPipelineConfiguration

open Shader.ShaderSupport

/// <summary>
/// Steuerung der Simulation
/// 
/// GraficSystem
/// Umgebung
/// Workflows
///
/// </summary>

open WeltModul

module SimulationSystem = 

    let logger = LogManager.GetLogger("Simulation.SimulationSystem")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger) 
    
    let mutable cancelAll           = new CancellationTokenSource()
    let mutable cancelMotion        = new CancellationTokenSource()
    let mutable cancelCollision     = new CancellationTokenSource()
    let mutable cancelUmgebungen    = new CancellationTokenSource()

    let DEFAULT_CAMERA_POS = Vector3(-5.0f, 5.0f, -15.0f)
    let DEFAULT_SIM_POS = Vector3.Zero 
    let DEFAULT_LIGHT_POS = Vector3(25.0f, -25.0f,  10.0f) 

    /// <summary>
    /// SimulationSystem type 
    /// Erweiterung um Kollisions und Task Funktionalität
    /// </summary>

    [<AllowNullLiteral>] 
    type MySimulation(graficWindow:MyWindow) =
        inherit MySystem(graficWindow)
        static let mutable instance = new MySimulation(MyWindow.Instance)  // Singleton
        let mutable isActive = false

        /// <summary>
        /// Umgebungen Workflow 
        /// Kontrollieren, dass kein Objekt die Welt verlässt
        /// </summary>
        let umgebungWorkflow (system:MySimulation) = async {
            let ID = System.DateTime.Now.ToString()   
            clock.Start()
            let umgebungen = Welt.Instance.Umgebungen.Values 
            let graficWorld = system.Displayables
            let moveableObjects = graficWorld.Values |> Seq.filter (fun x -> (x :? Moveable))|> Seq.map(fun x -> (x:?>Moveable)) 
            logInfo("Umgebungen WF started with " + umgebungen.Count.ToString() + " Umgebungen ")
            while true do      
                do! Async.Sleep 1
                for moveable in moveableObjects do
                    for umgebung in umgebungen do
                        umgebung.Monitor(moveable)
            logInfo("UmgebungWorkflow terminated")
        }

        member this.UmgebungWorkflow =
            umgebungWorkflow this   

        static member Instance
            with get() = instance
            and set(value) = instance <- value

        /// <summary>
        /// Konstruktor
        /// </summary>
        static member CreateInstance(defaultConfigurations: MyPipelineConfiguration list) =
            MyGPU.Instance.Initialize(MyWindow.Instance)
            MyGPU.Instance.FrameLength <- D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
            MyGPU.Instance.MatLength   <- D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
            MyGPU.Instance.ItemLength  <- D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()
            MyGPU.Instance.SetPipelineConfigurations(defaultConfigurations)

        /// <summary>
        /// Initializer
        /// </summary>
        member this.initialize() =
            this.ClearObjects()  
            this.SetPixelShader(ShaderClass.PhongPSType) 
            this.SetRasterizerState(RasterType.Wired)
            this.SetBlendType(BlendType.Opaque) 
            initCamera(
                DEFAULT_CAMERA_POS,
                DEFAULT_SIM_POS,                // Camera target
                aspectRatio,                    // Aspect ratio
                MathUtil.TwoPi / 200.0f,        // Scrollamount horizontal
                MathUtil.TwoPi / 200.0f)        // Scrollamount vertical
            initLight (
                DEFAULT_LIGHT_POS,
                Color.White) 

        member this.initializeWorld(ursprung:Vector3, laenge:float32, malX:int, malY:int, malZ:int)  =
            Welt.Instance.Initialize(ursprung, laenge, malX, malY, malZ)        
            this.InitObjects(Welt.Instance.GetDisplayables())
                
        member this.initializeForWorld(ursprung:Vector3, umgebungsLaenge:float32, malX:int, malY:int, malZ:int)  =
            this.initialize()
            this.initializeWorld(ursprung, umgebungsLaenge, malX, malY, malZ)

        member this.InitializeForWorldData(weltDaten:WeltDaten) =
            this.initializeForWorld(weltDaten.ursprung, weltDaten.laenge, weltDaten.malX, weltDaten.malY, weltDaten.malZ)

        override this.AddObjects(simulationObjects) =
            base.AddObjects(simulationObjects)
            Welt.Instance.registriereObjekteBeiUmgebung(simulationObjects)

        member this.SetCameraPos(newCameraPos) = 
            initCamera(
                newCameraPos,
                DEFAULT_SIM_POS,                // Camera target
                aspectRatio,                    // Aspect ratio
                MathUtil.TwoPi / 200.0f,        // Scrollamount horizontal
                MathUtil.TwoPi / 200.0f)        // Scrollamount vertical

        member this.SetLightPos(dir) =
            lightDir <- Vector3.Transform(dir, worldMatrix)
            frameLight <- new DirectionalLight(Color.White.ToColor4(), new Vector3(lightDir.X, lightDir.Y, lightDir.Z))
            this.FrameLight <- frameLight
            this.LightDir <- lightDir

        member this.changeColorTemperature temperature =
            writeToOutputWindow("Temperatur is" + temperature.ToString())
            if temperature >  0.0f then
               MyWindow.Instance.SetBackColor System.Drawing.Color.Black                          // über  0 Grad
            if temperature > 20.0f then
               MyWindow.Instance.SetBackColor System.Drawing.Color.PaleVioletRed                  // über 20 Grad
            if temperature > 30.0f then
               MyWindow.Instance.SetBackColor System.Drawing.Color.MediumVioletRed                // über 30 Grad
            if temperature > 40.0f then
               MyWindow.Instance.SetBackColor System.Drawing.Color.OrangeRed                       // über 40 Grad
            if temperature > 50.0f then
               MyWindow.Instance.SetBackColor System.Drawing.Color.IndianRed                       // über 50 Grad
            if temperature > 60.0f then
               MyWindow.Instance.SetBackColor System.Drawing.Color.DarkRed                         // über 60 Grad   
            if temperature > 70.0f then
               MyWindow.Instance.SetBackColor System.Drawing.Color.Crimson                         // über 70 Grad   
            if temperature > 80.0f then
               MyWindow.Instance.SetBackColor System.Drawing.Color.Fuchsia                         // über 80 Grad                 

        member this.HideUmgebungen() =
            Welt.Instance.HideUmgebungen()
            this.InstallObjects() 

        member this.UnhideUmgebungen() =
            Welt.Instance.UnhideUmgebungen()
            this.InstallObjects() 
        
        member this.toggleUmgebungen() =
            Welt.Instance.ToggleUmgebungen()
            this.InstallObjects()

        /// <summary>
        /// Starte alle Workflows
        /// </summary>
        member this.startWorkflows() = 
             cancelAll <- new CancellationTokenSource()
             let asynctasks =
                 Welt.Instance.MotionWorkflows
                 |> Seq.append [this.UmgebungWorkflow]
                 |> Async.Parallel 
 
             Async.StartAsTask (asynctasks, TaskCreationOptions.None, cancelAll.Token)|> ignore
             logInfo("All workflows started ")
             this.startUmgebungWorkflows()
             isActive <- true

        member this.stopWorkflows() =             
            this.stopUmgebungWorkflows()
            cancelAll.Cancel()
            logInfo("All workflows stopped ")
            isActive <- false

        /// <summary>
        /// Umgebung- Workflow
        /// </summary>
        member this.startUmgebungenWorkflow() = 
            cancelUmgebungen <- new CancellationTokenSource()  
            let starteable = umgebungWorkflow this
            Async.Start(starteable, cancelUmgebungen.Token)
            logInfo("Umgebungen WF started ")

        member this.stoptUmgebungenWorkflow() =
            cancelUmgebungen.Cancel()
            logInfo("Umgebungen WF deaktiviert")

        /// <summary>
        /// Motion
        /// </summary>
        member this.startMotionWorkflows() = 
            cancelMotion <- new CancellationTokenSource()             
            let motionWorkflows = Welt.Instance.MotionWorkflows 
            let asynctasks = 
                motionWorkflows
                |> Async.Parallel

            Async.StartAsTask (asynctasks, TaskCreationOptions.None, cancelMotion.Token)|> ignore
            logInfo("All motion workflows started ")
            isActive <- true

        member this.startUmgebungWorkflows() =
            for umg in Welt.Instance.Umgebungen.Values do
                if umg.hasElements() then
                    umg.startWorkflow()

        member this.stopUmgebungWorkflows() =
            for umg in Welt.Instance.Umgebungen.Values do
                if umg.hasElements() then
                    umg.stopWorkflow()

        member this.stopMotionWorkflows() =
            cancelMotion.Cancel()
            logInfo("All motions stopped ")
            isActive <- false 

        member this.toggleWorkflows() =             
            if isActive then 
                this.stopWorkflows()
            else                
                this.startWorkflows()

