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

open Geometry.GeometricModel

open Base.MaterialsAndTextures

open Aspose.Svg
open Aspose.Svg.Dom
open Aspose.Svg.Dom.Traversal.Filters

// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------
// Simple Format
// bestehend aus Vertex und Index-Liste
// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------
module SVGFormat =
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

    type RectFilter () =
        inherit NodeFilter()
        override this.AcceptNode(node:Node) =
            if node.NodeName.Equals("class") then NodeFilter.FILTER_ACCEPT else NodeFilter.FILTER_REJECT

    // ----------------------------------------------------------------------------------------------------
    // SVGBuilder
    // ----------------------------------------------------------------------------------------------------
    // 1. EINLESEN: Satz Vertexe einlesen
    // ----------------------------------------------------------------------------------------------------
    // 2. Kontur erstellen 
    // ----------------------------------------------------------------------------------------------------
    // 3. Ein Part erstellen
    // ----------------------------------------------------------------------------------------------------    
    type SvgBuilder(element:string, number:int, fileName:String) =
        let mutable element = element
        let mutable elementNumber = number
        let mutable fileName = fileName
        let mutable points = new List<Vector3>()
        let mutable part : Part = null
        let mutable parts : List<Part> = new List<Part>()
        let mutable shape : Shape = null 
        let mutable document:SVGDocument = null
        let mutable size =Vector3.One
        
        // ----------------------------------------------------------------------------------------------------
        //  Erzeugen der Kontur für eine Menge von Punkten
        // ----------------------------------------------------------------------------------------------------
        member this.Build(height:float32, material:Material, texture:Texture, sizeFactor: Vector3, visibility:Visibility, augment:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
            size <-  sizeFactor
            document <-  new SVGDocument (fileName) 
            let svgElement = document.RootElement

            for node in svgElement.Children do
                if node.ClassName = element then
                    let values = node.Attributes.Item(1)
                    input <- noLetters(values.Value).Trim()
                    let vals = input.Split(' ') 
                    
                    points <- new List<Vector3>() 
                    for i in 0..2..vals.Length-2 do
                        let point = 
                            Vector3(
                                Convert.ToSingle(vals.[i].Trim(), CultureInfo.InvariantCulture),
                                0.0f, 
                                Convert.ToSingle(vals.[i+1].Trim(), CultureInfo.InvariantCulture)
                            )
                        points.Add(point)
                    this.addPart(height, material, texture, visibility, shaders)
                else 
                    let named = node.Attributes.GetNamedItem("name") 
                    if named <> null && named.TextContent = element then
                        let values = node.Attributes.GetNamedItem("d")
                        input <- noLetters(values.Value).Trim()
                        let vals = input.Split(' ') 
                    
                        points <- new List<Vector3>() 
                        for i in 0..2..vals.Length-2 do
                            let point = 
                                Vector3(
                                    Convert.ToSingle(vals.[i].Trim(), CultureInfo.InvariantCulture),
                                    0.0f, 
                                    Convert.ToSingle(vals.[i+1].Trim(), CultureInfo.InvariantCulture)
                                )
                            points.Add(point)
                        this.addPart(height, material, texture, visibility, shaders)

        member this.addPart(height, material, texture, visibility, shaders) =
                    
            this.adjustXYZ()

            this.Resize()

            shape <- 
                new Corpus(
                    name = element,
                    contour = points.ToArray(),
                    height = height,
                    colorBottom = Color.White,
                    colorTop = Color.White,
                    colorSide = Color.White
                )

            part <- 
                new Part(
                    element,
                    shape,
                    material,
                    texture,
                    visibility,
                    shaders
                    )

            parts.Add(part)               

        member this.Parts =
            [parts.Item(elementNumber)] 

        member this.adjustXYZ()=
           let min = computeMinimum(points|> Seq.toList) 
           points <- points |> Seq.map (fun p -> p-min) |> ResizeArray
           ()

        member this.ComputeFactor() =
            let min = computeMinimum(points|> Seq.toList)
            let max= computeMaximum(points|> Seq.toList)
            let mutable box = BoundingBox()
            box.Minimum <- min  
            box.Maximum <- max   
            
            let actualSize = box.Maximum.X - box.Minimum
            let standardSize = 5.0f
            standardSize / actualSize 

        member this.Resize() =
            let aFactor = this.ComputeFactor() * size 
            for i = 0 to points.Count - 1 do
                let mutable resizedVertex = points.Item(i)
                resizedVertex  <- points.Item(i) * aFactor
                points.Item(i) <- resizedVertex