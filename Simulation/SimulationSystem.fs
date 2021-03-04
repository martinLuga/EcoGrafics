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
    let mutable cancelAll = new CancellationTokenSource()

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
        static let mutable instance = new MySimulation()  // Singleton
        
        new() = new MySimulation(MyWindow.Instance)

        static member Instance
            with get() = instance
            and set(value) = instance <- value

        static member CreateInstance(defaultConfigurations: MyPipelineConfiguration list) =
            MyGPU.Instance.Initialize(MyWindow.Instance)
            MyGPU.Instance.FrameLength <- D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
            MyGPU.Instance.MatLength   <- D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
            MyGPU.Instance.ItemLength  <- D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()
            MyGPU.Instance.SetPipelineConfigurations(defaultConfigurations)


        member this.initialize() =
            this.ClearObjects()  
            this.SetPixelShader(ShaderClass.PhongPSType) 
            this.SetRasterizerState(RasterType.Solid)
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

        member this.AddSimulationObjects(simulationObjects) =
            this.AddObjects(simulationObjects)
            Welt.Instance.registriereObjektListe(simulationObjects)

        member this.SetCameraPos(newCameraPos) = 
            initCamera(
                newCameraPos,
                DEFAULT_SIM_POS,                // Camera target
                aspectRatio,                    // Aspect ratio
                MathUtil.TwoPi / 200.0f,        // Scrollamount horizontal
                MathUtil.TwoPi / 200.0f)        // Scrollamount vertical

        member this.SetLightPos(newLightPos) =
            initLight (
                newLightPos,
                Color.White)  

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

        member this.UmgebungWorkflow = async {
            let ID = System.DateTime.Now.ToString()   
            clock.Start()
            let umgebungen = Welt.Instance.Umgebungen.Values 
            let graficWorld = this.Displayables
            let moveables = graficWorld.Values |> Seq.filter (fun x -> (x :? Moveable))|> Seq.map(fun x -> (x:?>Moveable)) 
            let worldLimits = Welt.Instance.WorldLimits
            logInfo("Umgebung WF started with " + umgebungen.Count.ToString() + " Umgebungen ")
            while true do      
                do! Async.Sleep 1
                for movbl in moveables do                   
                    for imm in worldLimits do
                        movbl.CheckNear(imm)
                    for umgebung in umgebungen do
                        umgebung.Control(movbl)
            logInfo("UmgebungWorkflow terminated")
        }

        member this.hideUmgebungen() =
            Welt.Instance.HideUmgebungen()
            this.InstallObjects() 
        
        member this.toggleUmgebungen() =
            Welt.Instance.ToggleUmgebungen()
            this.InstallObjects()

        /// <summary>
        /// Workflow
        /// </summary>
        member this.startWorkflows() = 
             cancelAll <- new CancellationTokenSource()
             let motionWorkflows = Welt.Instance.MotionWorkflows         
             let collisionWorkflows = Welt.Instance.CollisionWorkflows
             let asynctasks =
                 motionWorkflows
                 |> Seq.append collisionWorkflows
                 |> Seq.append [this.UmgebungWorkflow]
                 |> Async.Parallel 
 
             Async.StartAsTask (asynctasks, TaskCreationOptions.None, cancelAll.Token)|> ignore
             logInfo("All workflows started ")
             this.IsRunnung <- true

        member this.stopWorkflows() = 
            cancelAll.Cancel()
            logInfo("All workflows stopped ")
            this.IsRunnung <- false

        member this.toggleWorkflows() =             
            if this.IsRunnung then 
                this.stopWorkflows()
            else
                
                this.startWorkflows()

