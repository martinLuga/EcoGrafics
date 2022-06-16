namespace Builder
//
//  Vertex3D.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System
open System.IO
open System.Globalization
open System.Collections.Generic

open log4net

open SharpDX

open Base.ShaderSupport
open Base.LoggingSupport
open Base.ModelSupport
open Base.StringSupport
open Base.MathSupport
open Base.ObjectBase
open Base.GeometryUtils

open Geometry.GeometricModel

open Base.MaterialsAndTextures

open Aspose.Svg
open Aspose.Svg.Dom
open Aspose.Svg.Paths
open Aspose.Svg.Dom.Traversal.Filters

// ----------------------------------------------------------------------------------------------------
// SVGFormat
//  bestehend aus Vertex und Index-Liste
// ----------------------------------------------------------------------------------------------------
module BuilderSupport =

    let createHilitePart(part:Part) =
        let mutable box = BoundingBox()
        box.Minimum <- part.Shape.Minimum 
        box.Maximum <- part.Shape.Maximum 
        let augmentName = part.Name + "-hilite"
        new Part(
            augmentName,
            shape = Quader.NewFromMinMax(augmentName.ToUpper(), part.Shape.Minimum, part.Shape.Maximum , Color.White),
            material = MAT_LT_BLUE,
            visibility = Visibility.Transparent
        )

    let createHilitePartFrom(objectName, parts) =
        let minimum = Base.MathSupport.computeMinimum (Seq.map (fun (p:Part) -> p.Shape.Minimum) parts |>  Seq.toList) 
        let maximum = Base.MathSupport.computeMaximum (Seq.map (fun (p:Part) -> p.Shape.Maximum) parts |>  Seq.toList) 
        new Part(
            objectName + "-hilite",
            shape = Quader.NewFromMinMax(objectName + "-hilite", minimum, maximum , Color.White),
            material = MAT_LT_BLUE, 
            visibility = Visibility.Transparent
        )

    let createCenterPart(part: Part) =
        let radius = (Vector3.Distance(part.Shape.Maximum, part.Shape.Minimum))/2.0f
        let mutable box = BoundingBox()
        box.Minimum <- part.Shape.Minimum 
        box.Maximum <- part.Shape.Maximum 
        let center = box.Center
        let center = part.Center
        let augmentName = part.Name + "-center"
        new Part(
            augmentName,
            shape = new Kugel(augmentName.ToUpper(), center, 0.1f, Color.Red),
            material = MAT_RED,
            visibility = Visibility.Opaque
        )

    let createCenterPartFrom(objectName, parts) =
        let minimum = Base.MathSupport.computeMinimum (Seq.map (fun (p:Part) -> p.Shape.Minimum) parts |>  Seq.toList) 
        let maximum = Base.MathSupport.computeMaximum (Seq.map (fun (p:Part) -> p.Shape.Maximum) parts |>  Seq.toList) 
        new Part(
            objectName + "-center",
            shape = new Kugel(objectName + "-center", minimum, 0.1f, Color.Red),
            material = MAT_RED,
            visibility = Visibility.Opaque
        )