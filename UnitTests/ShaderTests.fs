namespace ecografics
//
//  Shader.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System
open log4net
open NUnit.Framework

open SharpDX.DXGI
open SharpDX.Direct3D 
open SharpDX.Direct3D12

open Base.LoggingSupport

open GraficBase.ShaderConfiguration

open GPUModel.MyPipelineSupport

open Shader.ShaderSupport

module Shader =    

    // ----------------------------------------------------------------------------------------------------
    //  Test des Nested Dictionarys 
    // ----------------------------------------------------------------------------------------------------
    [<TestFixture>]
    type NestedDictTests() = 

        [<DefaultValue>] val mutable logger: ILog 
        [<DefaultValue>] val mutable device:Device
        [<DefaultValue>] val mutable ndict:NestedDict<ShaderClass, BlendType, RasterType, TopologyType, string>

        [<OneTimeSetUp>]
        member this.setUp() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            this.logger <- LogManager.GetLogger("NestedDictTests")
            let factory = new Factory4()
            let adapter = factory.Adapters.[0]
            this.device <- new Device(adapter, FeatureLevel.Level_11_0) 
            this.ndict  <- new NestedDict<ShaderClass, BlendType, RasterType, TopologyType, string>()
            this.logger.Info("Setup\n")

        [<OneTimeTearDownAttribute>]
        member this.tearDown() =
            (this.device:> IDisposable).Dispose()
            this.logger.Info("Teardown\n")

        [<Test>]
        member this.NestedDictAddTest() = 
            this.logger.Info("NestedDictAddTest")
            this.ndict.Add(
                ShaderClass.SimpleVSType,
                ShaderClass.SimpleVSType,
                ShaderClass.TriDSType,
                ShaderClass.TriHSType,
                BlendType.Transparent,
                RasterType.Solid,
                TopologyType.Triangle,
                "OK")
            let result = 
                this.ndict.Item(ShaderClass.SimpleVSType, ShaderClass.SimpleVSType, ShaderClass.TriDSType, ShaderClass.TriHSType, BlendType.Transparent, RasterType.Solid, TopologyType.Triangle)
            this.logger.Info("NDict Item = " + result)            
            this.logger.Info("NestedDictAddTest ended\n")

        [<Test>]
        member this.NestedDictAdd2Test() = 
            this.logger.Info("NestedDictAddTest")
            this.ndict.Add(
                ShaderClass.SimpleVSType,
                ShaderClass.SimpleVSType,
                ShaderClass.TriDSType,
                ShaderClass.TriHSType,
                BlendType.Transparent,
                RasterType.Solid,
                TopologyType.Triangle,
                "OK")

            this.ndict.Add(
                ShaderClass.SimpleVSType,
                ShaderClass.SimpleVSType,
                ShaderClass.TriDSType,
                ShaderClass.TriHSType,
                BlendType.Transparent,
                RasterType.Solid,
                TopologyType.Triangle,
                "OK")
          
            this.logger.Info("NestedDictAddTest ended\n")

    // ----------------------------------------------------------------------------------------------------
    //  Test des Stores 
    // ----------------------------------------------------------------------------------------------------
    [<TestFixture>]
    type PsoStoreTests() = 

        [<DefaultValue>] val mutable logger: ILog 
        [<DefaultValue>] val mutable device:Device
        [<DefaultValue>] val mutable pStore:PipelineStore
        [<DefaultValue>] val mutable pso:PipelineState

        [<OneTimeSetUp>]
        member this.setUpAll() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            this.logger <- LogManager.GetLogger("PsoStoreTests")
            this.device <- new Device(null, FeatureLevel.Level_11_0)  
            this.pStore <- new PipelineStore(this.device)
            this.pso    <- new PipelineState(nativeint 0)
            this.logger.Info("Setup\n")

        [<OneTimeTearDown>]
        member this.tearDown() =
            (this.device:> IDisposable).Dispose()
            this.logger.Info("Teardown\n")

        [<Test>]
        member this.BuildPsoTest() = 
            this.logger.Info("BuildPsoTest")            
            this.pso <- this.pStore.buildPso(pipelineConfigTesselateQuad)
            Assert.NotNull(this.pso)
            this.logger.Info("BuildPsoTest ended\n")

        [<Test>]
        member this.GetSuccessfullTest() = 
            this.logger.Info("GetTest")
            try
                let pso = 
                    this.pStore.Get(
                        ShaderClass.SimpleVSType,
                        ShaderClass.SimplePSType,
                        ShaderClass.NotSet,
                        ShaderClass.NotSet,
                        BlendType.Opaque,
                        RasterType.Solid,
                        TopologyType.Triangle
                    )
                this.logger.Info("GetTest found pso\n")
                Assert.Pass()
            with 
            | :? PipelineStateNotFoundException as ex ->                 
                this.logger.Error("GetTest failed\n")
                Assert.Fail()

        [<Test>]
        member this.GetAndInsertTest() = 
            this.logger.Info("GetAndInsertTest")
            try
                let pso = 
                    this.pStore.Get(
                        ShaderClass.SimpleVSType,
                        ShaderClass.SimplePSType,
                        ShaderClass.NotSet,
                        ShaderClass.NotSet,
                        BlendType.Opaque,
                        RasterType.Solid,
                        TopologyType.Triangle
                    )
                this.logger.Error("Pso found")
                Assert.Fail()
            with 
            | :? PipelineStateNotFoundException as ex -> 
               this.logger.Info("No pso found, inserting new")
               let pso = null
               this.pStore.Add(
                   ShaderClass.SimpleVSType,
                   ShaderClass.SimplePSType,
                   ShaderClass.NotSet,
                   ShaderClass.NotSet,
                   BlendType.Opaque,
                   RasterType.Solid,
                   TopologyType.Triangle,
                   pso
               )
               this.logger.Info("New pso inserted")
            this.logger.Info("GetAndInsertTest ended\n")

    // ----------------------------------------------------------------------------------------------------
    //  Test des Client Interface
    // ----------------------------------------------------------------------------------------------------
    [<TestFixture>]
    type PsoProviderTests() = 

        [<DefaultValue>] val mutable logger: ILog 
        [<DefaultValue>] val mutable provider:PipelineProvider
        [<DefaultValue>] val mutable device:Device

        [<OneTimeSetUp>]
        member this.setUpAll() =
            configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
            this.logger <- LogManager.GetLogger("PsoProviderTests")
            this.logger.Info("Setup PsoProviderTests\n")
            this.device     <- new Device(null, FeatureLevel.Level_11_0) 
            this.provider   <- new PipelineProvider(this.device)

        [<SetUpAttribute>]
        member this.setUp() = 
            this.logger.Info("Created a provider")

        [<OneTimeTearDown>]
        member this.tearDown() =  
            this.device.Dispose()
            this.logger.Info("TearDown PsoProviderTests\n")

        [<Test>]
        member this.ActivatePsoTest() = 
            this.provider.ActivateConfig(pipelineConfigBasic)
            this.logger.Info("Activated a PsoTest\n")

        [<Test>]
        member this.GetPipelineStateTest() = 
            let pso = this.provider.GetCurrentPipelineState()
            Assert.NotNull(pso)
            this.logger.Info("Read pipelineState\n")