namespace ecografics
//
//  Architecture.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net
open NUnit.Framework

open Base.VertexDefs
open Base.ShaderCompile

open ShaderCommons 
open Initializations

// ----------------------------------------------------------------------------------------------------
// Shader Komponenten als String erzeugen
// Damit kann der Shader aus F# parametrisiert werden
// und können mit Shader Datasets kombiniert werden
// ----------------------------------------------------------------------------------------------------  
module ShaderTests =

    [<TestFixture>]
    type CompileTests() = 

        [<DefaultValue>] val mutable logger: ILog
        [<SetUp>]
        member this.setUp() =
            logger.Info("CompileTests set up ")
            
        [<OneTimeTearDownAttribute>]
        member this.tearDown() =
             logger.Info("CompileTests cleaned up ")

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
            logger.Info("Shader compiled from string")

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
            logger.Info("Shader compiled from string")

    [<TestFixture>]
    type AnalyzeTests() = 
        
        [<DefaultValue>] val mutable logger: ILog
        
        [<SetUp>]
        member this.setUp() =
           logger.Info("AnalyzeTests set up ")

        [<Test>]
        member this.AnalyzeVertexTest() =
            let vertex = new Vertex()
            let t = vertex.GetType()
            let fields = t.GetFields()
            logger.Debug("-----------")
            let name = sprintf "%s" t.FullName
            logger.Debug(name)
            fields |> Array.iter (fun field -> 
                let sprop =  sprintf "%s: %s: %O" field.Name field.FieldType.FullName field
                logger.Debug(sprop) 
            )