namespace PBRBase
//
//  Common.fs
//
//  Created by Martin Luga on 10.09.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open Base
open SharpDX.Direct3D
open SharpDX.Direct3D12

open System

open glTFLoader.Schema 
 
module Common = 

    // ----------------------------------------------------------------------------------------------------
    // Helper Classes
    // ----------------------------------------------------------------------------------------------------
    let myTexture(tex:Texture) = 
        new ModelSupport.Texture(tex.Name)

    let myTopologyType(typ: MeshPrimitive) =
        match typ.Mode with
        | MeshPrimitive.ModeEnum.POINTS      -> PrimitiveTopologyType.Point
        | MeshPrimitive.ModeEnum.LINES       -> PrimitiveTopologyType.Line
        | MeshPrimitive.ModeEnum.TRIANGLES   -> PrimitiveTopologyType.Triangle
        | _ -> raise(SystemException("Not supported"))

    let myTopology(typ: MeshPrimitive) =
        match typ.Mode with
        | MeshPrimitive.ModeEnum.POINTS      -> PrimitiveTopology.PointList
        | MeshPrimitive.ModeEnum.LINES       -> PrimitiveTopology.LineList
        | MeshPrimitive.ModeEnum.TRIANGLES   -> PrimitiveTopology.TriangleList
        | _ -> raise(SystemException("Not supported"))