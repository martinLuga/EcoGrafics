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
module SimpleFormat =

    let logger = LogManager.GetLogger("Builder.Simple")
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
    // SimpleBuilder
    // ----------------------------------------------------------------------------------------------------
    // 1. EINLESEN: Satz Vertexes und Satz Normals einlesen
    // ----------------------------------------------------------------------------------------------------
    // 2. Meshes erstellen:  Aus Vertexen und Indexen Meshes erstellen.
    // ----------------------------------------------------------------------------------------------------
    // 3. Ein Part erstellen
    // ----------------------------------------------------------------------------------------------------    
    type SimpleBuilder(name:String, fileName:String) =
        inherit ShapeBuilder(name, fileName)
        let mutable isTransparent = false
        let mutable actualMaterial : Material = null
        let mutable actualTexture : Texture = null

        // ----------------------------------------------------------------------------------------------------
        //  Erzeugen der Meshdaten für eine Menge von Punkten
        // ----------------------------------------------------------------------------------------------------
        member this. Build(material:Material, texture:Texture, sizeFactor: Vector3, visibility:Visibility, augment:Augmentation, quality:Quality, shaders:ShaderConfiguration) =

            this.Size <- sizeFactor

            actualMaterial <- material

            actualTexture <- texture

            isTransparent <- visibility = Visibility.Transparent

            reader <- new StreamReader(fileName) 
        
            // Anz Vertex
            input <- reader.ReadLine() 
            if  not (input = null) then
                let first = input.Split(':').[1].Trim() 
                vCount <- Convert.ToInt32(first) 

            // Anz Index
            input <- reader.ReadLine() 
            if not (input = null) then
                let first = input.Split(':').[1].Trim()
                tCount <- Convert.ToInt32(first)

            advanceLines()

            let cv2 = Vector2(0.5f, 0.5f)

            // ----------------------------------------------------------------------------------------------------
            //  Vertex
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
                        ) 
                    let norm =
                        Vector3(
                            Convert.ToSingle(vals.[3].Trim(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(vals.[4].Trim(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(vals.[5].Trim(), CultureInfo.InvariantCulture)
                        ) 
                    let vertex:Vertex = createVertex pos norm (transparentColor4(Color.White, isTransparent)) cv2 

                    this.Vertices.Add(vertex)    
                    
            advanceLines()

            // ----------------------------------------------------------------------------------------------------
            //  Index
            // ----------------------------------------------------------------------------------------------------
            for i in 0 ..3.. tCount*3 - 1 do
                input <- reader.ReadLine()

                if input <> null then
                    let m = input.Trim().Split(' ')
                    this.Indices.Add(Convert.ToInt32(m.[0].Trim()))
                    this.Indices.Add(Convert.ToInt32(m.[1].Trim()))
                    this.Indices.Add(Convert.ToInt32(m.[2].Trim()))

            logDebug ("Build complete --------------------------------------------")

            // ----------------------------------------------------------------------------------------------------
            //  Erzeugen des Parts
            // ----------------------------------------------------------------------------------------------------
            this.Part <- 
                new Part(
                    name,
                    new TriangularShape(name, Vector3.Zero, this.Vertices, this.Indices, Vector3.One, quality),
                    material,
                    texture,
                    visibility,
                    shaders
                )
            this.Parts.Add(this.Part)
            
            // ----------------------------------------------------------------------------------------------------
            //  Position an XYZ ausrichten
            // ----------------------------------------------------------------------------------------------------
            this.adjustXYZ()

            this.Normalize()

            this.Resize()

            this.Augment(augment)

        member this.MyVertices  
            with get() =
                this.Parts 
                |> Seq.map(fun p -> p.Shape.Vertices)   
                |> Seq.concat |> ResizeArray

        override this.ComputeFactor() =
            let min = computeMinimum(this.MyVertices |> Seq.toList)
            let max= computeMaximum(this.MyVertices |> Seq.toList)
            let mutable box = BoundingBox()
            box.Minimum <- min.Position
            box.Maximum <- max.Position

            let actualHeight = box.Maximum.Y - box.Minimum.Y
            let standardHeight = 1.0f
            standardHeight / actualHeight 