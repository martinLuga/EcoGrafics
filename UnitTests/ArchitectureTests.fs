namespace ecografics
//
//  Architecture.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open ApplicationBase 
open Base.FileSupport
open Base.Framework
open Base.LoggingSupport
open Base.ShaderSupport
open Base.VertexDefs
open DirectX.Assets
open DirectX.TextureSupport
open ExampleShaders
open GPUModel.MyGPU
open GPUModel.MyPipelineConfiguration
open GraficBase.GraficController
open GraficBase.GraficWindow
open Initializations
open log4net
open NUnit.Framework
open SharpDX
open System
open System.IO

module Architecture =

    configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
    let getLogger(name:string) = LogManager.GetLogger(name)

    [<SetUpFixture>]
    type MySetUpClass() =
        [<OneTimeSetUp>]
        member this.RunBeforeAnyTests() =
            let dir = Path.GetDirectoryName(typeof<MySetUpClass>.Assembly.Location) 
            Environment.CurrentDirectory <- dir
    
            // or
            Directory.SetCurrentDirectory(dir);

    [<TestFixture>]
    type GPUTests() = 

        [<DefaultValue>] val mutable logger: ILog
        [<DefaultValue>] val mutable myWindow:MyWindow
        [<DefaultValue>] val mutable myGpu:MyGPU

        [<OneTimeSetUp>]
        member this.setUp() =
            configureLog4net "Tests" "resource" "log4net.config"
            this.logger <- LogManager.GetLogger("ArchitectureTests")

            WindowLayout.Setup("TEST")

            MyController.CreateInstance(
                "UnitTests",
                this.myWindow,
                [ pipelineConfigBasic
                  pipelineConfigTesselateQuad
                  pipelineConfigTesselateTri ],
                  defaultConfiguration=pipelineConfigBasic
            )             
            MyController.Instance.initLight (new Vector3( 0.0f,  -5.0f,  10.0f), Color.White)     // In Richtung hinten nach unten

            // Camera  
            MyController.Instance.ConfigureCamera(Vector3( 0.0f, 5.0f, -10.0f), Vector3.Zero)       
            
        [<OneTimeTearDownAttribute>]
        member this.tearDown() =
             (this.myGpu:> IDisposable).Dispose()
             this.logger.Info("ArchitectureTests cleaned up ")


        member this.RunApp() =
            let displayables = getGraphicObjects() 
            MyController.Instance.InstallObjects(displayables)
            MyController.Instance.Run()  
            this.logger.Info("ArchitectureTests ended ")

    [<TestFixture>]
    type Framework() = 

        [<DefaultValue>] val mutable logger : ILog

        [<SetUp>]
        member this.setUp() =
            this.logger <- getLogger("GraficFunctions")

        [<Test>]
        member this.TestMakefour() =  
            let result = MAKEFOURCC('D', 'X', 'T', '5')
            logger.Info("MAKEFOURCC result " + result.ToString())

        [<Test>]
        member this.TestTextureSupport() =  
            let filename = fileNameInMap "EcoGrafics" "UnitTests" "Textures" "grass.dds"
            let texRes = CreateTextureFromDDS_2(device, filename) 
            logger.Info("Textur gelesen")

        [<Test>]
        member this.TestEveryNthElement() =  
            let elements = seq {for I in 1 .. 50 do createVertex Vector3.Zero Vector3.UnitZ  Color.Wheat  (new Vector2(0.0f, 0.0f)) true}
            let every2 = everyNth 10 elements 
            Assert.IsNotEmpty(every2)