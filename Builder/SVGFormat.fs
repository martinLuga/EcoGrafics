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
    // 1. Points aus *.svg einlesen
    // 2. Kontur erstellen 
    // 3. Ein Part erstellen
    // ----------------------------------------------------------------------------------------------------    
    type SvgBuilder(fileName:String, element:string, ?number:int) =
        let mutable element = element
        let mutable elementNumber = defaultArg number -1
        let mutable fileName = fileName
        let mutable part : Part = null
        let mutable partNr = 0
        let mutable parts : List<Part> = new List<Part>()
        let mutable shape : Shape = null 
        let mutable document:SVGDocument = null
        let mutable size =Vector3.One
        
        // ----------------------------------------------------------------------------------------------------
        //  Erzeugen der Kontur für eine Menge von Punkten
        // ----------------------------------------------------------------------------------------------------
        member this.Build
            (
                height: float32,
                material: Material,
                texture: Texture,
                sizeFactor: Vector3,
                visibility: Visibility,
                augment: Augmentation,
                quality: Quality,
                shaders: ShaderConfiguration
            ) =
            size <- sizeFactor
            document <- new SVGDocument(fileName)
            let svgElement = document.RootElement

            for node in svgElement.Children do
                if node.ClassName = element then
                    let values = node.Attributes.Item(1)
                    this.createPoints (values, height, material, texture, visibility, shaders)
                else
                    // TODO
                    let named = node.Attributes.GetNamedItem("name")
                    if named <> null && named.TextContent = element then
                        let values = node.Attributes.GetNamedItem("d")
                        this.createPoints (values, height, material, texture, visibility, shaders)

        member this.createPoints(values: Attr, height, material, texture, visibility, shaders) =
            input <- noLetters(values.Value).Trim()
            let vals = input.Split(' ')

            let mutable points = new List<Vector3>()

            for i in 0..2 .. vals.Length - 2 do
                let point =
                    Vector3(
                        Convert.ToSingle(vals.[i].Trim(), CultureInfo.InvariantCulture),
                        0.0f,
                        Convert.ToSingle(vals.[i + 1].Trim(), CultureInfo.InvariantCulture)
                    )

                points.Add(point)

            this.addPart (&points, height, material, texture, visibility, shaders)

        member this.addPart(points: List<Vector3> byref, height, material, texture, visibility, shaders) =

            if elementNumber >= 0 then
                this.adjustXYZ (&points)

            this.Resize(&points)

            let partName = element + partNr.ToString()
            partNr <- partNr + 1

            shape <-
                new Corpus(
                    name = partName,
                    contour = points.ToArray(),
                    height = height,
                    colorBottom = Color.White,
                    colorTop = Color.White,
                    colorSide = Color.White
                )

            part <- new Part(partName, shape, material, texture, visibility, shaders)

            parts.Add(part)               

        member this.Parts =
            if elementNumber < 0 then
                parts |> Seq.toList
            else
                [parts.Item(elementNumber)] 

        member this.adjustXYZ(points: List<Vector3> byref)=
           let min = computeMinimum(points|> Seq.toList) 
           points <- points |> Seq.map (fun p -> p-min) |> ResizeArray
           ()

        member this.Resize(points: List<Vector3> byref) =
            for i = 0 to points.Count - 1 do
                let mutable resizedVertex = points.Item(i)
                resizedVertex  <- points.Item(i) * size
                points.Item(i) <- resizedVertex