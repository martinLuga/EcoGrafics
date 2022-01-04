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

module Cache =

    // ----------------------------------------------------------------------------------------------------
    // Alle PipelineConfigurations werden im  NestedDict zu ihren Schlüsseln abgelegt
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type NestedDict<'TOPOLTYPE, 'TOPOL, 'PSO when 'TOPOLTYPE:equality and 'TOPOL:equality> () =
        let mutable topolTypeDict   = Dictionary<'TOPOLTYPE, Dictionary<'TOPOL, 'PSO>>()
        let newTopolDict()          = Dictionary<'TOPOL, 'PSO>()

        member this.Add(topolType:'TOPOLTYPE, topol:'TOPOL, result:'PSO) =
            topolTypeDict.
                TryItem(topolType, newTopolDict()).
                Replace(topol, result)

        member this.Item(topolType:'TOPOLTYPE, topol:'TOPOL) =
            topolTypeDict.Item(topolType).Item(topol) 

    [<AllowNullLiteral>]
    type ShaderCache() = 
        static let mutable instance:ShaderCache = null   
        let mutable ndict = new NestedDict<PrimitiveTopologyType, PrimitiveTopology, ShaderDescription>()
        static member Instance
            with get() = 
                if instance = null then
                    instance <- new ShaderCache()
                instance
            and set(value) = instance <- value

        static member AddShader(_TopologyType:PrimitiveTopologyType, _Topology:PrimitiveTopology, _ShaderDesc:ShaderDescription) =
            ShaderCache.Instance.Add(_TopologyType, _Topology , _ShaderDesc)

        static member AddAllShaders(pConfs: (PrimitiveTopologyType * PrimitiveTopology * ShaderDescription) list) =
            for c in pConfs do
                ShaderCache.Instance.Add(c)

        static member GetConfig(topoType:PrimitiveTopologyType, topo:PrimitiveTopology) =
            ShaderCache.Instance.Get(topoType, topo) 
    
        member this.Add(_TopologyType:PrimitiveTopologyType, _Topology:PrimitiveTopology, _ShaderDesc:ShaderDescription) =
            ndict.Add(_TopologyType, _Topology, _ShaderDesc)

        member this.Get(topoType:PrimitiveTopologyType, topo:PrimitiveTopology) =
            ndict.Item(topoType, topo)

