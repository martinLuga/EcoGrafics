namespace GltfBase
//
//  Common.fs
//
//  Created by Martin Luga on 10.09.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open Base
open Base.Framework
open Base.GeometryUtils
open SharpDX
open SharpDX.Direct3D
open SharpDX.Direct3D12
open System
open System.Collections.Generic
open VGltf
open VGltf.Types
 
module Common = 

    // ----------------------------------------------------------------------------------------------------
    // Helper Classes
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type MyMaterial
        (
            _index: int,
            _material: Material,
            _baseColourFactor: float32 [],
            _emissiveFactor: float32 [],
            _metallicRoughnessValues: float32 []
        ) =
        let mutable index = _index
        let mutable material = _material        
        let mutable baseColourFactor =_baseColourFactor  
        let mutable emissiveFactor =_emissiveFactor 
        let mutable metallicRoughnessValues =_metallicRoughnessValues 

        member this.Index = index
        member this.Material = material 
        member this.BaseColourFactor = baseColourFactor  
        member this.EmissiveFactor = emissiveFactor 
        member this.MetallicRoughnessValues = metallicRoughnessValues 

    [<AllowNullLiteral>]
    type MyTexture(_objName:string, _name:string, _indx:int, _kind:TextureInfoKind, _matIdx:int, _smpIdx:int, _sampler:Sampler, _image:System.Drawing.Image, _data:byte[], _info:Image, _cube:bool) =
        let mutable objName = _objName
        let mutable indx    = _indx
        let mutable name    = _name
        let mutable kind    = _kind
        let mutable matIdx  = _matIdx
        let mutable smpIdx  = _smpIdx
        let mutable sampler = _sampler 
        let mutable image   = _image
        let mutable data    = _data
        let mutable info    = _info
        let mutable cube    = _cube

        member this.ObjectName = objName
        member this.Name    = name
        member this.Index   = indx
        member this.Kind    = kind
        member this.MaterialIdx = matIdx
        member this.SamplerIdx = smpIdx 
        member this.Sampler = sampler 
        member this.Image   = image
        member this.Data    = data
        member this.Info    = info
        member this.Cube    = cube

        override this.ToString() = "MyTexture (" + _kind.ToString() + ") : " + name + " : " + indx.ToString()

    // ----------------------------------------------------------------------------------------------------
    //  NestedDicts
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type NestedDict4<'TYP1, 'TYP2, 'TYP3, 'TYP4, 'RESULT when 'TYP1:equality and 'TYP2:equality and 'TYP3:equality and 'TYP4:equality and 'RESULT:equality and 'RESULT:null> () =
        let mutable key1Dict  = Dictionary<'TYP1,   Dictionary<'TYP2,  Dictionary<'TYP3,  Dictionary<'TYP4, 'RESULT>>>>()
        let newKey2Dict    () = Dictionary<'TYP2, Dictionary<'TYP3,  Dictionary<'TYP4,'RESULT>>>()
        let newKey3Dict    () = Dictionary<'TYP3, Dictionary<'TYP4,'RESULT>>()
        let newKey4Dict    () = Dictionary<'TYP4, 'RESULT>()

        member this.Add(o1:'TYP1, o2:'TYP2, o3:'TYP3, o4:'TYP4, result:'RESULT) =
            key1Dict.
                TryItem(o1, newKey2Dict()).
                TryItem(o2, newKey3Dict()).
                TryItem(o3, newKey4Dict()).
                Replace(o4, result)

        member this.Item(o1: 'TYP1, o2: 'TYP2, o3: 'TYP3, o4: 'TYP4) =
            key1Dict.Item(o1).Item(o2).Item(o3).Item(o4)

        member this.ContainsKey(o1: 'TYP1, o2: 'TYP2, o3: 'TYP3, o4: 'TYP4) = this.Item(o1, o2, o3, o4) <> null

        member this.Clear() = key1Dict.Clear()

    [<AllowNullLiteralAttribute>]
    type NestedDict3<'TYP1, 'TYP2, 'TYP3, 'RESULT when 'TYP1:equality and 'TYP2:equality and 'TYP3:equality and 'RESULT:equality and 'RESULT:null> () =
        let mutable key1Dict  = Dictionary<'TYP1, Dictionary<'TYP2,  Dictionary<'TYP3,'RESULT>>>()
        let newKey2Dict    () = Dictionary<'TYP2, Dictionary<'TYP3,'RESULT>>()
        let newKey3Dict    () = Dictionary<'TYP3, 'RESULT>()

        member this.Add(o1: 'TYP1, o2: 'TYP2, o3: 'TYP3, result: 'RESULT) =
            key1Dict
                .TryItem(o1, newKey2Dict ())
                .TryItem(o2, newKey3Dict ())
                .Replace(o3, result)

        member this.Item(o1: 'TYP1, o2: 'TYP2, o3: 'TYP3) = key1Dict.Item(o1).Item(o2).Item(o3)

        member this.Items(o1: 'TYP1, o2: 'TYP2 ) = 
            try
                key1Dict.Item(o1).Item(o2).Values |> Seq.toList
            with :? KeyNotFoundException -> []

        member this.ContainsKey(o1: 'TYP1, o2: 'TYP2, o3: 'TYP3) = 
            try
                this.Item(o1, o2, o3) |> ignore
                true
            with :? KeyNotFoundException -> false

        member this.Clear() = key1Dict.Clear()

        member this.Items() =
                key1Dict.Values
                    |> Seq.toList
                    |> Seq.concat
                    |> Seq.map(fun kp -> kp.Value)
                    |> Seq.concat
                    |> Seq.map(fun kp -> kp.Value)
                    |> Seq.toList

    [<AllowNullLiteralAttribute>]
    type NestedDict2<'TYP1, 'TYP2, 'RESULT when 'TYP1: equality and 'TYP2: equality and 'RESULT: equality and 'RESULT: null>
        () =
        let mutable key1Dict =
            Dictionary<'TYP1, Dictionary<'TYP2, 'RESULT>>()

        let newKey2Dict () = Dictionary<'TYP2, 'RESULT>()

        member this.Add(o1: 'TYP1, o2: 'TYP2, result: 'RESULT) =
            key1Dict
                .TryItem(o1, newKey2Dict ())
                .Replace(o2, result)

        member this.Item(o1: 'TYP1, o2: 'TYP2) = key1Dict.Item(o1).Item(o2)

        member this.Items(o1: 'TYP1) = key1Dict.Item(o1).Values

        member this.ContainsKey(o1: 'TYP1, o2: 'TYP2) = this.Item(o1, o2) <> null

        member this.Clear() = key1Dict.Clear()

        member this.AllItems =
                key1Dict.Values
                    |> Seq.toList
                    |> Seq.concat
                    |> Seq.map(fun kp -> kp.Value)
                    |> Seq.toList

        member this.Count = this.AllItems.Length

    // ----------------------------------------------------------------------------------------------------
    // NodeAdapter
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type NodeAdapter(_gltf:Gltf, _idx:int) =
        let gltf = _gltf
        let idx  = _idx
        let mutable node:Node = null
        let mutable children: NodeAdapter list = []

        override this.ToString() =
            if node = null then this.instantiate()
            node.Name

        member this.Idx = _idx

        member this.Node = node

        member this.Children = children

        member this.GetChildren() = 
            this.instantiate()
            children

        member this.AllItems()      = 
            this.Items(this.Idx) |> Seq.toList

        member this.AllNodes()      = this.Nodes(this.Idx) |> Seq.toList

        member this.Count           = this.AllItems().Length

        member this.LeafesCount     = (this.LeafAdapters() ).Length

        member this.instantiate() = 
            if node = null then 
                node <- gltf.Nodes[idx]
                let childreni = node.Children 
                children <- 
                    if childreni <> null then
                        childreni |> Seq.map(fun i -> new NodeAdapter(gltf, i) )|> Seq.toList
                    else 
                        []

        member this.instantiateAll() = 
            this.instantiate()
            for child in this.GetChildren() do
                child.instantiateAll()

        member this.printAll() = 
            printf " %i " idx 
            for child in this.GetChildren() do
                printfn " " 
                child.printAll()

        // All leafes as int[] recursively
        member this.Items(idx) =
            // Process recursively all children
            let node = gltf.Nodes[ idx ]

            if node.Children <> null then
                node.Children
                |> Seq.append (
                    node.Children
                    |> Seq.collect (fun child -> this.Items(child))
                )
            else
                [ idx ]

        // All nodes!! as Node recursively
        member this.Nodes(idx)  = 
            let mynode = gltf.Nodes[ idx ]
            if mynode.Children <> null then
                mynode.Children |> Seq.map (fun i -> gltf.Nodes[i])
                |> Seq.append (                   
                    mynode.Children
                    |> Seq.collect (fun i  -> this.Nodes(i))                
                )
            else
                [ mynode ]

        // All Adapter (leafes only) recursively
        member this.LeafAdapters():NodeAdapter list  =  
            this.instantiate()
            if this.Children.Length > 0 then                 
                this.Children
                |> List.collect (fun ada -> ada.LeafAdapters())
            
            else
                [this]

        member this.Adapters()  =  
            this.instantiate()
            if this.Children.Length > 0 then 
                [this]
                |> List.append( 
                    this.Children
                    |> List.collect (fun ada -> ada.Adapters())
                )            
            else
                [this]

        member this.WithIdx(_idx)  =  
            this.Adapters()
                |> List.find (fun ada -> ada.Idx = _idx)

        member this.UpdatePositionsDeep(objectWorld) =
            this.UpdatePos (this.Idx, objectWorld)

        member this.UpdatePos (idx, _parentMatrix:Matrix) =
            let mynode = gltf.Nodes[ idx ]
            let myTransform = createLocalTransform (mynode.Translation , mynode.Rotation,  mynode.Scale) 
            let newMatrix = myTransform * _parentMatrix
            mynode.Matrix <- newMatrix.ToArray()

            if mynode.Children <> null then
                mynode.Children 
                    |> Seq.iter (fun i -> this.UpdatePos(i, newMatrix))

        member this.Mesh = 
            if node.Mesh.HasValue then 
                gltf.Meshes[node.Mesh.Value] 
            else null

    // ----------------------------------------------------------------------------------------------------
    // Conversions
    // ----------------------------------------------------------------------------------------------------
    let myMaterial(mat:Material) = 
        let a = mat.EmissiveFactor.[0]
        new ModelSupport.Material(
            name=mat.Name,
            diffuseAlbedo=Vector4.Zero,
            fresnelR0=Vector3.Zero,
            roughness=0.0f,
            ambient=Color4.White,
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=0.0f,
            emissive=Color4.White,
            hasTexture=false
        )

    let myTexture(tex:Texture) = 
        new ModelSupport.Texture(tex.Name)

    let myTopologyType(src_typ:Nullable<Types.Mesh.PrimitiveType.ModeEnum>) =
        if src_typ.HasValue then
            let typ = src_typ.Value
            match typ with
            | Types.Mesh.PrimitiveType.ModeEnum.POINTS      -> PrimitiveTopologyType.Point
            | Types.Mesh.PrimitiveType.ModeEnum.LINES       -> PrimitiveTopologyType.Line
            | Types.Mesh.PrimitiveType.ModeEnum.TRIANGLES   -> PrimitiveTopologyType.Triangle
            | _ -> raise(SystemException("Not supported"))
        else 
            PrimitiveTopologyType.Triangle

    let myTopology(src_typ:Nullable<Types.Mesh.PrimitiveType.ModeEnum>) =
        if src_typ.HasValue then
            let typ = src_typ.Value
            match typ with
            | Types.Mesh.PrimitiveType.ModeEnum.POINTS      -> PrimitiveTopology.PointList
            | Types.Mesh.PrimitiveType.ModeEnum.LINES       -> PrimitiveTopology.LineList
            | Types.Mesh.PrimitiveType.ModeEnum.TRIANGLES   -> PrimitiveTopology.TriangleList
            | _ -> raise(SystemException("Not supported"))
        else 
            PrimitiveTopology.TriangleList