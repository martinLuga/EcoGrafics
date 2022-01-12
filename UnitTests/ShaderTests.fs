namespace ecografics
//
//  Architecture.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open Base.LoggingSupport
open log4net
open NUnit.Framework
open Shader.ShaderCompile

open ShaderCommons 

// ----------------------------------------------------------------------------------------------------
// Shader Komponenten als String erzeugen
// Damit kann der Shader aus F# parametrisiert werden
// und können mit Shader Datasets kombiniert werden
// ----------------------------------------------------------------------------------------------------  
module ShaderTests =

    configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
    let getLogger(name:string) = LogManager.GetLogger(name)

    [<TestFixture>]
    type CompileTests() = 

        [<DefaultValue>] val mutable logger: ILog
        [<SetUp>]
        member this.setUp() =
            this.logger <- LogManager.GetLogger("CompileTests")
            
        [<OneTimeTearDownAttribute>]
        member this.tearDown() =
             this.logger.Info("CompileTests cleaned up ")

        [<Test>]
        member this.CompileFromString() =
            let parametersString =  
                "
                    struct VertexIn
                    {
                        float3 PosL    : POSITION;
                        float3 NormalL : NORMAL;
                        float2 TexC    : TEXCOORD;
                    };                
                    struct VertexOut
                    {
                        float4 PosH : SV_POSITION;
                        float3 PosL : POSITION;
                    };
                    float4 PS(VertexOut pin) : SV_Target
                    {
                        return pin.PosH;
                    }
                "
            let shader = shaderFromString (parametersString, "PS", "ps_5_1")            
            Assert.NotNull (shader)
            this.logger.Info("Shader compiled from string")

        [<Test>]
        member this.CompileFromStringAndFile() =
            let vertexParameters =
                "
                    struct VertexIn
                    {
                        float3 PosL    : POSITION;
                        float3 NormalL : NORMAL;
                        float2 TexC    : TEXCOORD;
                    };                
                    struct VertexOut
                    {
                        float4 PosH : SV_POSITION;
                        float3 PosL : POSITION;
                    };
                    "
            let shString = commonsLuna + vertexParameters
            let fileShaderCode = "SKYSH"
            let shader = shaderFromStringAndFile (shString, "shaders", fileShaderCode, "PS", "ps_5_1")            
            Assert.NotNull (shader)
            this.logger.Info("Shader compiled from string")