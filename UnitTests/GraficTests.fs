namespace FrameworkTests

open log4net

open Base.Logging 
open Base.Framework 

open NUnit.Framework

open SharpDX.DXGI 
open SharpDX.Direct3D
open SharpDX.Direct3D12
open SharpDX.Mathematics.Interop

open DirectX.TextureSupport

open Initializations

open GPUModel.MyGPU
open GPUModel.MyGraphicWindow

module GraficTests = 

    [<TestFixture>]
    type DirectX12() = 

        [<DefaultValue>] val mutable logger : ILog
        [<DefaultValue>] val mutable graficWindow : MyWindow
        [<DefaultValue>] val mutable myGpu:MyGPU  
        [<DefaultValue>] val mutable debugController:Debug1 

        [<SetUp>]
        member this.Init() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            this.logger <- LogManager.GetLogger("DirectX12")
            this.logger.Info(" ")

        [<Test>]
        member this.CreateDevice() = 
            let factory = new Factory4()
            let adapter = factory.Adapters.[0]
            let device = new Device(adapter, FeatureLevel.Level_11_0) 
            this.logger.Info(" ")

        [<Test>]
        member this.HardwareResources() = 
            let factory = new Factory4() 
            for adapter in factory.Adapters do
                let desc = adapter.Description
                let text = "***Adapter: " +  desc.Description +  "\n"  
                this.logger.Info(text)
                LogAdapterOutputs(adapter)
                
    [<TestFixture>]
    type GraficFunctions() = 

        [<DefaultValue>] val mutable logger : ILog

        [<SetUp>]
        member this.setUp() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            this.logger <- LogManager.GetLogger("GraficTests")

        [<Test>]
        member this.TestMakefour() =  
            let result = MAKEFOURCC('D', 'X', 'T', '5')
            this.logger.Info("MAKEFOURCC result " + result.ToString())

        [<Test>]
        member this.TestTextureSupport() =  
            let filename = fileNameInMap "EcoGrafics" "UnitTests" "Textures" "grass.dds"
            let texRes = CreateTextureFromDDS_2(device, filename) 
            this.logger.Info("Textur gelesen")