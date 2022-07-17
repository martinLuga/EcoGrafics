﻿namespace ShaderPBR
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

open glTFLoader.Schema 
 
module Common = 

    // ----------------------------------------------------------------------------------------------------
    // Helper Classes
    // ----------------------------------------------------------------------------------------------------
 
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