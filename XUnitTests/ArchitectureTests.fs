namespace FrameworkTests

open System

open SharpDX

open log4net

open NUnit.Framework

open Base.Logging

open ApplicationBase
open ApplicationBase.WindowControl
open ApplicationBase.DisplayableObject
open ApplicationBase.WindowLayout

open GPUModel.MyGPU
open GPUModel.MyGraphicWindow

open ApplicationBase.GraficSystem
open ApplicationBase.ShaderConfiguration

open Initializations

module ArchitectureTests =

    [<TestFixture>]
    type GPUTests() = 

        [<DefaultValue>] val mutable logger: ILog
        [<DefaultValue>] val mutable myWindow:MyWindow

        [<OneTimeSetUp>]
        member this.setUp() =
            configureLog4net "Tests" "resource" "log4net.config"
            this.logger <- LogManager.GetLogger("ArchitectureTests")

            WindowLayout.Setup("TEST")

            this.myWindow <- graficWindow

            MySystem.CreateInstance(this.myWindow,  [pipelineConfigBasic; pipelineConfigTesselateQuad; pipelineConfigTesselateTri ])
             
            initLight (new Vector3( 0.0f,  -5.0f,  10.0f), Color.White)     // In Richtung hinten nach unten

            // Camera  
            initCamera(
                Vector3( 0.0f, 5.0f, -10.0f),   // Camera position
                Vector3.Zero,                   // Camera target
                aspectRatio,                    // Aspect ratio
                MathUtil.TwoPi / 100.0f,        // Scrollamount horizontal
                MathUtil.TwoPi / 100.0f)        // Scrollamount vertical
            
        [<OneTimeTearDownAttribute>]
        member this.tearDown() =
             (MyGPU.Instance:> IDisposable).Dispose()
             this.logger.Info("ArchitectureTests cleaned up ")


        member this.RunApp() =
            let displayables = getGraphicObjects() 
            MySystem.Instance.InitObjects(displayables)
            MySystem.Instance.Start()  
            this.logger.Info("ArchitectureTests ended ")