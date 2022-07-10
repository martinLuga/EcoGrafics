namespace ecografics
//
//  Hardware.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open NUnit.Framework

open SharpDX.DXGI 
open SharpDX.Direct3D
open SharpDX.Direct3D12

open Base.LoggingSupport 

open GraficBase.GraficWindow

open GPUModel.MyGPU

open DirectX.SoundSupport

open Initializations

module Hardware = 

    configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
    let getLogger(name:string) = LogManager.GetLogger(name)

    [<TestFixture>]
    type DirectX12() = 

        [<DefaultValue>] val mutable logger : ILog
        [<DefaultValue>] val mutable graficWindow : MyWindow
        [<DefaultValue>] val mutable myGpu:MasterGPU  
        [<DefaultValue>] val mutable debugController:Debug1 

        [<SetUp>]
        member this.Init() =
            this.logger <- getLogger("DirectX12")
            this.logger.Info(" ")

        [<Test>]
        member this.CreateDevice() = 
            let factory = new Factory4()
            let adapter = factory.Adapters.[0]
            let device = new Device(adapter, FeatureLevel.Level_11_0) 
            logger.Info(" ")

        [<Test>]
        member this.HardwareResources() = 
            let factory = new Factory4() 
            for adapter in factory.Adapters do
                let desc = adapter.Description
                let text = "***Adapter: " +  desc.Description +  "\n"  
                logger.Info(text)
                LogAdapterOutputs(adapter)

    [<TestFixture>]
    type XAudio2() = 

        [<DefaultValue>] val mutable logger : ILog
        [<DefaultValue>] val mutable graficWindow : MyWindow
        [<DefaultValue>] val mutable myGpu:MasterGPU  
        [<DefaultValue>] val mutable debugController:Debug1 
        [<DefaultValue>] val mutable wavePlayer:WavePlayer

        [<SetUp>]
        member this.Init() =
            this.logger <- getLogger("XAudio2")
            this.logger.Info(" ")
            this.wavePlayer <- new WavePlayer()

        [<Test>]
        member this.PlayWave() =
            PLaySoundFile("sound/CoinIn.wav")

        [<Test>]
        member this.PlayWhoa() =
            PLaySoundFile("sound/matrix_whoa3.wav")

