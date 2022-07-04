namespace Builder
//
//  Vertex3D.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX

open Geometry.GeometricModel3D
open Base.ModelSupport 

open Builder

open Base.MaterialsAndTextures

module ModelExtension =

    [<AbstractClass>]
    type DisplayFile(name: string, fileName: string, parts:Part list, material: Material, texture: Texture, size:Vector3, visibility: Visibility,  augmentation, quality) =
        inherit Display(parts, visibility, size, augmentation)
        
        let mutable name = name
        let mutable fileName = fileName
        let mutable quality=quality

        new(name, fileName, material, texture, visibility, size, augmentation) = new DisplayFile(name, fileName, [],  material, texture, visibility, size, augmentation, Quality.Original)
        
        member this.Name
            with get() = name
            and set(value) = name  <- value

        member this.FileName
            with get() = fileName

    type Geometry(name: string, parts:Part list, visibility: Visibility, size:Vector3, augmentation) =
        inherit Display(parts, visibility, size, augmentation)
        
        let mutable name = name

        do
            let mutable i = 1
            for part in parts do
                part.Name <- name + i.ToString()
                part.Visibility <- visibility

        override this.Parts 
            with get() =  
                if this.Augmentation = Augmentation.Hilite then
                    let hp = this.createHilitePart() 
                    parts @ [hp] 
                else parts

        new(name, visibility, size, augmentation) = new Geometry(name, [], visibility, size, augmentation)
        new(name, parts, visibility, augmentation) = new Geometry(name, parts, visibility, Vector3.One, augmentation)
        new(name, parts) = new Geometry(name, parts, Visibility.Opaque, Vector3.One, Augmentation.None)
        
        member this.Name
            with get() = name
            and set(value) = name  <- value

        member this.createHilitePart() =
            let minimum = Base.MathSupport.computeMinimum (Seq.map (fun (p:Part) -> p.Shape.Minimum) parts |>  Seq.toList) 
            let maximum = Base.MathSupport.computeMaximum (Seq.map (fun (p:Part) -> p.Shape.Maximum) parts |>  Seq.toList) 
            new Part(
                name + "-hilite",
                shape = Quader.NewFromMinMax(name + "-hilite", minimum, maximum , Color.White),
                material = MAT_LT_BLUE,
                visibility = Visibility.Transparent
            )

        override this.ToString() =
            "DisplayGeometry-" + base.ToString()