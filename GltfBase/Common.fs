namespace GltfBase
//
//  Common.fs
//
//  Created by Martin Luga on 10.09.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open Base
open Base.Framework
open Base.ShaderSupport
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
    type MyTexture(_objName:string, _textureIdx:int, _textName:string, _heapIdx:int, _textureType:TextureTypePBR, _matIdx:int, _smpIdx:int, _sampler:Sampler, _image:System.Drawing.Image, _data:byte[], _info:Image, _cube:bool) =
        let mutable objName = _objName
        let mutable heapIdx = _heapIdx
        let mutable idx     = _textureIdx
        let mutable name    = _textName
        let mutable txtTyp  = _textureType
        let mutable matIdx  = _matIdx
        let mutable smpIdx  = _smpIdx
        let mutable sampler = _sampler 
        let mutable image   = _image
        let mutable data    = _data
        let mutable info    = _info
        let mutable cube    = _cube

        member this.ObjectName = objName
        member this.Name    = name
        member this.Idx     = idx
        member this.HeapIdx
            with get() = heapIdx
            and set(value) = heapIdx <- value
        member this.Kind    = txtTyp
        member this.MatIdx  = matIdx
        member this.SmpIdx
            with get() = smpIdx
            and set(value) = smpIdx <- value    
        member this.Sampler = sampler 
        member this.Image   = image
        member this.Data    = data
        member this.Info    = info
        member this.Cube    = cube

        override this.ToString() = "MyTexture (" + _textureType.ToString() + ") : " + idx.ToString() + "/" + name + " " 

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