namespace GraficBase
//
//  Cache.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//


open System.Collections.Generic 

open SharpDX.Direct3D
open SharpDX.Direct3D12

open Base.ShaderSupport
open Base.Framework 

module CacheXX =

    // ----------------------------------------------------------------------------------------------------
    // Alle PipelineConfigurations werden im  NestedDict zu ihren Schlüsseln abgelegt
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type NestedDict<'SHADER_TYPE, 'TOPOLTYPE, 'TOPOL, 'PSO when 'SHADER_TYPE:equality and 'TOPOLTYPE:equality and 'TOPOL:equality and 'PSO:null>  () =
        let mutable shaderTypeDict  = Dictionary<'SHADER_TYPE, Dictionary<'TOPOLTYPE, Dictionary<'TOPOL, 'PSO>>>()
        let newTopolTypeDict()      = Dictionary<'TOPOLTYPE, Dictionary<'TOPOL, 'PSO>>()
        let newTopolDict()          = Dictionary<'TOPOL, 'PSO>()

        member this.Add(shaderType:'SHADER_TYPE, topolType:'TOPOLTYPE, topol:'TOPOL, result:'PSO) =
            shaderTypeDict.
                TryItem(shaderType, newTopolTypeDict()).
                TryItem(topolType, newTopolDict()).
                Replace(topol, result)

        member this.Item(shaderType:'SHADER_TYPE, topolType:'TOPOLTYPE, topol:'TOPOL) =
            try
                shaderTypeDict.Item(shaderType).Item(topolType).Item(topol) 
            with
            | :? KeyNotFoundException -> null

    [<AllowNullLiteral>]
    type ShaderCache() = 
        static let mutable instance:ShaderCache = null   
        let mutable ndict = new NestedDict<ShaderType, PrimitiveTopologyType, PrimitiveTopology, ShaderDescription>()
        static member Instance
            with get() = 
                if instance = null then
                    instance <- new ShaderCache()
                instance
            and set(value) = instance <- value

        static member AddShader(_ShaderType:ShaderType, _TopologyType:PrimitiveTopologyType, _Topology:PrimitiveTopology, _ShaderDesc:ShaderDescription) =
            ShaderCache.Instance.Add(_ShaderType, _TopologyType, _Topology , _ShaderDesc)

        static member AddShaders(pConfs: (ShaderType * PrimitiveTopologyType * PrimitiveTopology * ShaderDescription) list) =
            for c in pConfs do
                ShaderCache.Instance.Add(c)

        static member AddShaderFromDesc(topologyType:PrimitiveTopologyType, topology:PrimitiveTopology, desc: ShaderDescription) =
            ShaderCache.AddShader(desc.Klass, topologyType , topology, desc) 

        static member GetShader(_ShaderType:ShaderType, topoType:PrimitiveTopologyType, topo:PrimitiveTopology) =
            ShaderCache.Instance.Get(_ShaderType, topoType, topo) 
    
        member this.Add(_ShaderType:ShaderType, _TopologyType:PrimitiveTopologyType, _Topology:PrimitiveTopology, _ShaderDesc:ShaderDescription) =
            ndict.Add(_ShaderType, _TopologyType, _Topology, _ShaderDesc)

        member this.Get(_ShaderType:ShaderType, topoType:PrimitiveTopologyType, topo:PrimitiveTopology) =
            ndict.Item(_ShaderType, topoType, topo)

