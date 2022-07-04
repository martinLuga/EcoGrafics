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

open Geometry.GeometricModel3D

open Base.MaterialsAndTextures

open Aspose.Svg
open Aspose.Svg.Dom
open Aspose.Svg.Paths
open Aspose.Svg.Dom.Traversal.Filters

open SVGFormat

// ----------------------------------------------------------------------------------------------------
// SVGFormat
//  bestehend aus Vertex und Index-Liste
// ----------------------------------------------------------------------------------------------------
module Svg =

    let logger = LogManager.GetLogger("Builder.Svg")
    let logDebug = Debug(logger)

    let MATERIAL_COLOR(color:Color) = 
        new Material( 
            name=color.ToString(),
            ambient=Color4(0.2f),
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=20.0f,
            emissive=color.ToColor4()
        )

    type Element with
        member this.Name() =
            let named = this.Attributes.GetNamedItem("name")
            if named <> null then 
                named.NodeValue
            else 
                let idd = this.Attributes.GetNamedItem("id")
                if idd <> null then 
                    idd.NodeValue
                else null

    // ----------------------------------------------------------------------------------------------------
    // SVGBuilder
    // ----------------------------------------------------------------------------------------------------
    // 1. Points aus *.svg einlesen
    // 2. Kontur erstellen 
    // 3. Ein Part erstellen
    // ----------------------------------------------------------------------------------------------------    
    type SvgBuilder(fileName:String) =
        let mutable klasse = ""
        let mutable elementNumber = -1
        let mutable fileName = fileName
        let mutable position = Vector3.Zero
        let mutable height = 0.0f
        let mutable part : Part = null
        let mutable partNr = 0
        let mutable matNr = 0
        let mutable parts : List<Part> = new List<Part>()
        let mutable objects : List<BaseObject> = new List<BaseObject>()
        let mutable objectName = ""
        let mutable shape : Shape = null 
        let mutable document:SVGDocument = new SVGDocument(fileName)
        let mutable size = Vector3.One
        let mutable name = ""
        let mutable lastName = ""
        let mutable partName = ""
        let mutable material = Material()
        let mutable texture= Texture() 
        let mutable visibility = Visibility.Opaque
        let mutable shaders = ShaderConfiguration.CreateForTesselation()

        // ----------------------------------------------------------------------------------------------------
        //  Erzeugen von Konturen für eine Menge von Punkten als Objekte
        // ----------------------------------------------------------------------------------------------------
        member this.Build
            (
                _klass:string,
                _number:string,
                _height: float32,
                _material: Material,
                _texture: Texture,
                _position: Vector3,
                _sizeFactor: Vector3,
                _visibility: Visibility,
                _augment: Augmentation,
                _quality: Quality,
                _shaders: ShaderConfiguration
            ) =
            position <- _position
            height <- _height
            klasse <- _klass
            size <- _sizeFactor
            material <- _material
            elementNumber <- if _number = null then -1 else Convert.ToInt32(_number.Trim(), CultureInfo.InvariantCulture)
            texture <- _texture
            visibility <- _visibility
            shaders <- _shaders 

            let root = document.RootElement

            // Für alle Einträge von Element <class>
            if klasse = null then
                this.BuildAll(root)
            else 
                // Für all Elemente mit dem namen
                let elem  = 
                    root.Children
                        |> Seq.filter (
                            fun child -> 
                                child.ClassName = klasse
                                || child.Name() = klasse
                                || child.Id = klasse
                            ) 
                        |> Seq.toList

                if elem.IsEmpty then  
                    let message = "Kein Eintrag gefunden für Elementname " + klasse 
                    raise (ParseException(message))

                for e in elem do
                    this.BuildElement(e)

         member this.BuildAll(element) =
            for node in element.Children do
                this.BuildElement(node) 

        member this.BuildElement(node) =
            match node.LocalName with
            
            | "path" -> 
                this.BuildPath(node)

            | "g" ->     
                objectName <- node.Name()
                for node in node.Children do
                    match node.LocalName with                        
                    | "path"  ->               
                        this.BuildPart(node)

                    | "polygon" -> 
                        this.BuildPolygon(node)
                                
                    | _ -> ()
    
                // Ein Objekt, mehrere Parts
                let object = this.createObjectParts (objectName, Vector3.Zero, parts)
                objects.Add(object) 

            | "polygon" ->                 
                this.BuildPolygon(node) 
                let object = this.createObjectParts (objectName, position, parts)
                objects.Add(object) 
                
            | _ -> ()

        member this.ObjectName(name) =        
            if name <> lastName then partNr <- 0 else partNr <- partNr + 1
            lastName <- name
            name + partNr.ToString()

        // Ein Objekt, ein Part
        member this.BuildPolygon(node) =  
            let mutable color = Color.White
            let colorAttr = node.Attributes.GetNamedItem("fill")
            if colorAttr = null then 
                color <- Color.White
            else 
                let colorHex = colorAttr.Value.TrimStart('#')
                let num = Int32.Parse(colorHex, System.Globalization.NumberStyles.HexNumber) 
                color <- Color.FromBgra(num)

            color <- Color.Transparent
            
            let polygon = new Polygon(node)
            let partName, points = polygon.Evaluate() 
            resize(points, size)
            let origin = computeMinimumXYZ(points|> Seq.toList)
            let part = this.createPart (origin, points, partName, height, material, texture, color, visibility, shaders)
            parts.Add(part)

        // Ein Objekt, ein Part
        member this.BuildPath(node) = 
            let path = new Path(node)
            let objectName, points = path.Evaluate() 
            let oname = this.ObjectName(objectName)
            let mat = this.getMaterial(klasse, material) 
            resize(points, size)
            let origin = computeMinimumXYZ(points|> Seq.toList)
            let part = this.createPart (Vector3.Zero, points, oname, height, mat, texture, Color.White, visibility, shaders) 
            let object = this.createObject (oname, position + origin, part)
            objects.Add(object) 

        member this.BuildPart(node) =        
            let path = new Path(node)
            let partName, points = path.Evaluate()
            resize(points, size)
            let part = this.createPart (Vector3.Zero, points, partName, height, material, texture, Color.White, visibility, shaders)
            parts.Add(part)

        // ----------------------------------------------------------------------------------------------------
        //  Service Funktionen
        // ----------------------------------------------------------------------------------------------------
        member this.createPart(origin, points: List<Vector3>, partName, height, material, texture, color:Color, visibility, shaders) =
            shape <-
                new Corpus(
                    name = partName.ToUpper(),
                    origin = origin,
                    contour = points.ToArray(),
                    height = height,
                    colorBottom = color,
                    colorTop = color,
                    colorSide = Color.Black
                )
            new Part(partName, shape, material, texture, visibility, shaders)  

        member this.createObject(objectName, origin, part:Part) =
            new BaseObject(
                name=objectName,
                display = 
                    new Display(
                        parts = [part]
                    ), 
                position = origin
            ) 

        member this.createObjectParts (objectName, origin, parts) = 
            new BaseObject(
                name=objectName,
                display = 
                    new Display(
                        parts |> Seq.toList
                    ), 
                position = origin
            ) 
 

        member this.getMaterial(element, material) =
            if element = null then
                matNr <- 
                    if matNr < DefaultMaterials.Length - 1 then 
                        matNr + 1
                    else 0
                DefaultMaterials.[matNr]
            else 
                material

        member this.Objects =
            if elementNumber < 0 then
                objects |> Seq.toList
            else
                [objects.Item(elementNumber)] 

