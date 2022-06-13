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
        let mutable matNr = 0
        let mutable parts : List<Part> = new List<Part>()
        let mutable objects : List<BaseObject> = new List<BaseObject>()
        let mutable shape : Shape = null 
        let mutable document:SVGDocument = null
        let mutable size = Vector3.One
        let mutable normalized = false
        
        // ----------------------------------------------------------------------------------------------------
        //  Erzeugen von Konturen für eine Menge von Punkten als Parts
        // ----------------------------------------------------------------------------------------------------
        member this.CreateParts
            (
                height: float32,
                material: Material,
                texture: Texture,
                sizeFactor: Vector3,
                visibility: Visibility,
                augment: Augmentation,
                quality: Quality,
                _normalized: bool,
                shaders: ShaderConfiguration
            ) =
            normalized <- _normalized
            size <- sizeFactor
            document <- new SVGDocument(fileName)
            let svgElement = document.RootElement

            for node in svgElement.Children do
                if node.LocalName = "path" then

                    let mat = this.getMaterial(element, material)
                    partNr <- partNr + 1
                            
                    if node.ClassName = "" then
                        let named = node.Attributes.GetNamedItem("name")
                        let name = node.Attributes.GetNamedItem("name").Value
                        if name = element || element = "*" then
                            let partName = name + partNr.ToString()
                            if named <> null then                            
                                let pathElement = node :?>SVGPathElement 
                                let points = this.parseSegments(pathElement)
                                part <- this.createPart (points[0], points, partName, height, mat, texture, visibility, shaders)
                                parts.Add(part) 
                    else  
                        if node.ClassName = element || element = "*" then
                            let partName = node.ClassName + partNr.ToString()
                            let values = node.Attributes.GetNamedItem("d")
                            let points = this.createPoints (values.Value)
                            part <- this.createPart (points[0], points, partName, height, mat, texture, visibility, shaders)
                            parts.Add(part) 

        // ----------------------------------------------------------------------------------------------------
        //  Erzeugen von Konturen für eine Menge von Punkten als Objekte
        // ----------------------------------------------------------------------------------------------------
        member this.CreateObjects
            (
                height: float32,
                material: Material,
                texture: Texture,
                position: Vector3,
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
                if node.LocalName = "path" then

                    let mat = this.getMaterial(element, material)
                    partNr <- partNr + 1
                            
                    if node.ClassName = "" then
                        let named = node.Attributes.GetNamedItem("name")
                        let name = node.Attributes.GetNamedItem("name").Value
                        if name = element || element = "*" then
                            let objectName = name + partNr.ToString()
                            if named <> null then                            
                                let pathElement = node :?>SVGPathElement 
                                let points = this.parseSegments(pathElement)
                                let object = this.createObject (position + points[0], points, objectName, height, mat, texture, visibility, shaders)
                                objects.Add(object) 
                    else  
                        if node.ClassName = element || element = "*" then
                            let objectName = node.ClassName + partNr.ToString()
                            let values = node.Attributes.GetNamedItem("d")
                            let points = this.createPoints (values.Value)
                            let object = this.createObject (position + points[0], points, objectName, height, mat, texture, visibility, shaders)
                            objects.Add(object) 
            ()

        member this.parseSegments(element:SVGPathElement):List<Vector3> = 
            let mutable points = new List<Vector3>()
            let pathSegList = element.PathSegList |> Seq.toList
            let move = pathSegList.Head :?> SVGPathSegMovetoAbs
            let rels = pathSegList.Tail 

            let mutable lastPoint = Vector3(move.X, 0.0f, move.Y)
            points.Add(lastPoint)

            for relSeg in rels do
                match relSeg.PathSegType with
                | SVGPathSeg.PATHSEG_LINETO_REL -> 
                    let rel = relSeg :?> SVGPathSegLinetoRel
                    let nextPoint = lastPoint + Vector3(rel.X , 0.0f, rel.Y)
                    points.Add(nextPoint)
                    lastPoint <- nextPoint   
                | _ -> ()
            points

        member this.getMaterial(element, material) =
            if element = "*" then
                matNr <- 
                    if matNr < DefaultMaterials.Length - 1 then 
                        matNr + 1
                    else 0
                DefaultMaterials.[matNr]
            else 
                material

        member this.createPoints(values: string):List<Vector3> =
            let mutable points = new List<Vector3>()
            input <- noLetters(values).Trim()
            if input.Length > 0 then
                let vals = input.Split(' ')               

                for i in 0..2 .. vals.Length - 2 do
                    let point =
                        Vector3(
                            Convert.ToSingle(vals.[i].Trim(), CultureInfo.InvariantCulture),
                            0.0f,
                            Convert.ToSingle(vals.[i + 1].Trim(), CultureInfo.InvariantCulture)
                        )

                    points.Add(point)
            points

        member this.createPart(origin:Vector3, points: List<Vector3> , partName, height, material, texture, visibility, shaders) =

            if points.Count > 0 then

                if normalized then
                    axisAligned (points)

                this.Resize(points)

                shape <-
                    new Corpus(
                        name = partName,
                        origin = origin,
                        contour = points.ToArray(),
                        height = height,
                        colorBottom = Color.White,
                        colorTop = Color.White,
                        colorSide = Color.White
                    )

                new Part(partName, shape, material, texture, visibility, shaders) 
            else 
                null
                
        member this.createObject(origin:Vector3, points: List<Vector3> , partName, height, material, texture, visibility, shaders) =
            let part = this.createPart (Vector3.Zero, points, partName, height, material, texture, visibility, shaders)
            let objekt = 
                new BaseObject(
                    name=partName,
                    display = 
                        new Display(
                            parts = [part]
                        ), 
                    position = origin
                ) 
            objekt  

        member this.Parts =
            if elementNumber < 0 then
                parts |> Seq.toList
            else
                [parts.Item(elementNumber)] 

        member this.Objects =
            if elementNumber < 0 then
                objects |> Seq.toList
            else
                [objects.Item(elementNumber)] 

        member this.adjustXYZ(points: List<Vector3>)=
           let min = computeMinimum(points|> Seq.toList) 
           for i = 0 to points.Count - 1 do
               let mutable resized = points.Item(i)
               resized  <- points.Item(i) - min
               points.Item(i) <- resized

        member this.Resize(points: List<Vector3>) =
            for i = 0 to points.Count - 1 do
                let mutable resized = points.Item(i)
                resized  <- points.Item(i) * size
                points.Item(i) <- resized