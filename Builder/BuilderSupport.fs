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

    let transparentColor4(color:Color, isTransparent) = 
        if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()

    // ----------------------------------------------------------------------------------------------------
    // Augmentierung
    // ----------------------------------------------------------------------------------------------------  
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
    // Gemeinsam genutzte Builder Funktionen
    // ---------------------------------------------------------------------------------------------------- 
    [<AllowNullLiteral>]
    type ShapeBuilder(name:String, fileName:String) =        
        let mutable name = name
        let mutable fileName = fileName

        let mutable vertices = new List<Vertex>()
        let mutable indices = new List<int>()
        let mutable size = Vector3.One
        
        let mutable parts : List<Part> = new List<Part>()
        let mutable part  : Part = null

        member this.Name
            with get() = name
            and set(value) = name <- value

        member this.FileName
            with get() = fileName
            and set(value) = fileName <- value

        member this.Parts
            with get() = parts
            and set(value) = parts <- value

        member this.Part
            with get() = part
            and set(value) = part <- value

        abstract Vertices:List<Vertex> with get, set
        default this.Vertices
            with get() = vertices
            and set(value) = vertices <- value

        member this.Indices
            with get() = indices
            and set(value) = indices <- value

        member this.Size
            with get() = size
            and set(value) = size <- value

        member this.GetVertices =
            parts 
            |> Seq.map(fun p -> p.Shape.Vertices)   
            |> Seq.concat
            |> Seq.toList 

        // ----------------------------------------------------------------------------------------------------
        // Normierung. Größe und Position.
        // ----------------------------------------------------------------------------------------------------
        abstract adjustXYZ:unit->Unit
        default this.adjustXYZ()=
           let min = computeMinimum(this.Vertices|> Seq.toList) 
           for part in this.Parts do 
                part.Shape.Vertices <- part.Shape.Vertices |> Seq.map (fun v -> v.Shifted(-min.Position)) |> ResizeArray

        abstract ComputeFactor:Unit->float32
        default this.ComputeFactor() =
            let minimum = computeMinimum(this.GetVertices|> Seq.toList)
            let maximum = computeMaximum(this.GetVertices|> Seq.toList)
            let actualHeight = maximum.Position.Y - minimum.Position.Y
            let actualDepth = maximum.Position.Z - minimum.Position.Z
            let actualWidt = maximum.Position.X - minimum.Position.X
            let mutable actualSize = max actualHeight actualWidt             
            actualSize <- max actualSize actualDepth 
            let standardHeight = 1.0f
            standardHeight / actualSize 

        abstract Normalize:Unit->Unit
        default this.Normalize() =
            let mutable aFactor = this.ComputeFactor() 
            let factor = Vector3(aFactor, aFactor, aFactor)
            for part in this.Parts do 
                part.Shape.Vertices <- part.Shape.Vertices |> Seq.map (fun v -> v.Resized(factor)) |> ResizeArray 

        abstract Resize:unit->Unit
        default this.Resize() =
            let newSize = this.Size  
            for part in this.Parts do 
                part.Shape.Vertices <- part.Shape.Vertices |> Seq.map (fun v -> v.Resized(newSize)) |> ResizeArray

        member this.Augment(_augment) =
            match _augment with
            | Augmentation.Hilite ->
                let hp = createHilitePartFrom(this.Name, this.Parts)  
                this.Parts.Add(hp)
                logDebug ("Augmentation Hilte " + hp.Shape.Name )
            | Augmentation.ShowCenter ->
                let hp = createCenterPartFrom(this.Name, this.Parts)  
                this.Parts.Add(hp)
                this.Parts.Add(this.Part)
            | None -> ()
            | _ -> raise (System.Exception("Augmentation not supported"))