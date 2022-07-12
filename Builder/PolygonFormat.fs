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
open Base.VertexDefs

open Geometry.GeometricModel3D

open BuilderSupport

// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------
// Simple Format
// bestehend aus Vertex und Index-Liste
// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------
module PolygonFormat =
    let logger = LogManager.GetLogger("Builder.Polygon")
    let logDebug = Debug(logger)

    let mutable vCount = 0
    let mutable tCount = 0 
    let mutable reader:StreamReader= null
    let mutable input = ""

    let MATERIAL_COLOR(color:Color) = 
        new Material( 
            name=color.ToString(),
            ambient=Color4(0.2f),
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=20.0f,
            emissive=color.ToColor4()
        )

    let advanceLines() =
        while (input <> null && not (input.StartsWith("{", StringComparison.Ordinal))) do
            input <- reader.ReadLine()  

    // ----------------------------------------------------------------------------------------------------
    // PolygonBuilder
    // ----------------------------------------------------------------------------------------------------
    // 1. EINLESEN: Satz Vertexe einlesen
    // ----------------------------------------------------------------------------------------------------
    // 2. Kontur erstellen 
    // ----------------------------------------------------------------------------------------------------
    // 3. Ein Part erstellen
    // ----------------------------------------------------------------------------------------------------    
    type PolygonBuilder(name:String, fileName:String) =
        inherit ShapeBuilder(name, fileName)
        let mutable kontur = new List<Vector3>()
        let mutable shape : Shape = null 
        let mutable size = Vector3.One 

        // ----------------------------------------------------------------------------------------------------
        //  Erzeugen der Kontur für eine Menge von Punkten
        // ----------------------------------------------------------------------------------------------------
        member this. Build(origin:Vector3, height:float32, material:Material, texture:Texture, sizeFactor: Vector3, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
            reader <- new StreamReader(fileName) 
        
            // Anz points
            input <- reader.ReadLine() 
            if  not (input = null) then
                let first = input.Split(':').[1].Trim() 
                vCount <- Convert.ToInt32(first) 

            advanceLines()

            // ----------------------------------------------------------------------------------------------------
            //  Kontur
            // ----------------------------------------------------------------------------------------------------
            for i in 0..vCount-1 do
                input <- reader.ReadLine() 
                if input <> null then 
                    let vals = input.Split(' ') 
                    let pos = 
                        Vector3(
                            Convert.ToSingle(vals.[0].Trim(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(vals.[1].Trim(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(vals.[2].Trim(), CultureInfo.InvariantCulture)
                        ) * sizeFactor
                    kontur.Add(pos)
            advanceLines()

            shape <- 
                new Corpus(
                    name = name,
                    origin = origin,
                    contour = kontur.ToArray(),
                    height = height,
                    colorBottom = Color.White,
                    colorTop = Color.White,
                    colorSide = Color.Black
                )

            // ----------------------------------------------------------------------------------------------------
            //  Erzeugen des Part
            // ----------------------------------------------------------------------------------------------------
            this.Part <- 
                new Part(
                    name,
                    shape,
                    material,
                    texture,
                    visibility,
                    shaders
                )

            match augmentation with
            | Augmentation.Hilite ->
                let hp =  createHilitePart(this.Part) 
                this.Parts.Add(this.Part)
                this.Parts.Add(hp)                
            | Augmentation.ShowCenter ->
                let hp =  createCenterPart(this.Part) 
                this.Parts.Add(hp)
                this.Parts.Add(this.Part)
            | _ ->
                this.Parts.Add(this.Part)