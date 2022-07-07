namespace Builder
//
//  Vertex3D.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System
open System.Collections.Generic

open SharpDX

open Base.VertexDefs
open Base.ModelSupport
open Base.MaterialsAndTextures

open Geometry.GeometricModel3D

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

    // ----------------------------------------------------------------------------------------------------
    //Gemeinsam genutzte Builder Funktionen
    // ----------------------------------------------------------------------------------------------------  
    type ShapeBuilder(name:String, fileName:String) =
        
        let mutable vertices = new List<Vertex>()
        let mutable size = Vector3.One

        // ----------------------------------------------------------------------------------------------------
        // Normierung. Größe und Position.
        // ----------------------------------------------------------------------------------------------------            
        member this.adjustXYZ()=
           let min = computeMinimum(vertices|> Seq.toList) 
           vertices <- vertices |> Seq.map (fun v -> v.Shifted(-min.Position)) |> ResizeArray
           ()

        member this.ComputeFactor() =
            let min = computeMinimum(vertices|> Seq.toList)
            let max= computeMaximum(vertices|> Seq.toList)
            let mutable box = BoundingBox()
            box.Minimum <- min.Position 
            box.Maximum <- max.Position  
            
            let actualHeight = box.Maximum.Y - box.Minimum
            let standardHeight = 1.0f
            standardHeight / actualHeight 

        member this.Resize() =
            let aFactor = this.ComputeFactor() * size
            for i = 0 to vertices.Count - 1 do
                let mutable resizedVertex = vertices.Item(i)
                resizedVertex.Position <- vertices.Item(i).Position * aFactor
                vertices.Item(i) <- resizedVertex