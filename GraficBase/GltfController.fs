namespace GraficBase 
//
//  SimulationSystem.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//     
open System.Collections.Generic

open log4net

open SharpDX
open SharpDX.Windows
open SharpDX.DXGI 
open SharpDX.Direct3D12
open SharpDX.Mathematics.Interop
open System.Runtime.InteropServices

 
open Base.LoggingSupport
open Base.ObjectBase 
open Base.ShaderSupport
open Base.GameTimer

open DirectX.D3DUtilities
open DirectX.Pipeline

open GPUModel.MyGPU
open GPUModel.MyFrame

open VGltf
open VGltf.Types

open GraficWindow
open GraficController

// ----------------------------------------------------------------------------------------------------
//  Simulation - Anzeige
//  
//  GraficSystem
//  Physics
// 
// ----------------------------------------------------------------------------------------------------    
module SimulationController =
    
    let logger = LogManager.GetLogger("GraficBase.GltfController")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger) 

    let GROUND_HEIGHT = 5.0f
    let INFINITE_DOWN = Vector3(-999999.9f, -999999.9f, -999999.9f)

    let mutable matNr = 0
    let mutable materials:Dictionary<int,Material> = new Dictionary<int,Material>()
    let mutable materialIndices = new Dictionary<string, int>()
    
    // ----------------------------------------------------------------------------------------------------
    //  SimulationSystem type 
    //  Erweiterung um Kollisions und Task Funktionalität
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]  
    type MyGLTFController(graficWindow:MyWindow ) =
        inherit MyController(graficWindow)

        // ----------------------------------------------------------------------------------------------------
        //  Konstruktor
        // ----------------------------------------------------------------------------------------------------
        static member CreateInstance(graficWindow: MyWindow) =          
            let instance = MyGLTFController(graficWindow )
            graficWindow.Renderer <- instance.GPU
            instance.Configure()
            instance
   
        member this.Reset() = 
            base.Reset()

        member this.AddObject(object:BaseObject) = 
            base.AddObject(object)    
        
        member this.Prepare() =
            base.Prepare()

        override this.ConfigureGPU() =
            this.GPU.FrameLength <- D3DUtil.CalcConstantBufferByteSize<FrameConstants>()    // TODO 
            this.GPU.MatLength   <- Marshal.SizeOf(typeof<Material>) 
            this.GPU.ItemLength  <- D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()   // TODO 

        override this.Run() = 
            logInfo("Run") 
            this.GPU.Begin()
            this.IsRunning <- ControllerStatus.Running
            this.Timer.Reset()
            // Windows Render-Loop 
            let sorted = this.Objects.Values|> Seq.sortBy(fun disp -> disp.Transparent) 
            let loop = new RenderLoop(graficWindow)
            while loop.NextFrame() &&  this.isRunning() do                     
                if this.notIdle() then
                    logInfo("Step") 
                    this.Timer.Tick()
                    this.GPU.StartUpdate()
                    this.updatePerFrame() 
                    this.GPU.StartDraw()                 
                    let mutable PartIdx = 0    
                    for object in sorted do
                        for part in object.Display.Parts do  
                            this.updatePerPart(PartIdx, object, part)                           // Position wird pro Part gesetzt 
                            this.updatePerMaterial(part.Material, part.hasTexture())            // Material wird pro Part gesetzt
                            this.drawPerPart(PartIdx, part)
                            PartIdx <- PartIdx + 1
                    this.GPU.EndDraw()
            logInfo("Terminated") 
            logDebug("Terminated") 
   
        // ----------------------------------------------------------------------------------------------------
        //  Accessor
        // ----------------------------------------------------------------------------------------------------

        member this.GetObjects() = 
            base.Objects.Values  |> Seq.toList

        override this.ToString() =
            "GltfController-" + graficWindow.ToString()

