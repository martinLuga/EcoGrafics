namespace Builder
//
//  Vertex3D.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System
open System.Globalization
open System.Collections.Generic

open log4net

open SharpDX

open Base.LoggingSupport
open Base.ModelSupport
open Base.StringSupport

open Aspose.Svg
open Aspose.Svg.Dom
open Aspose.Svg.DataTypes
open Aspose.Svg.Paths
open Aspose.Svg.Dom.Traversal.Filters

// ----------------------------------------------------------------------------------------------------
// SVGFormat
//  bestehend aus Vertex und Index-Liste
// ----------------------------------------------------------------------------------------------------
module SVGFormat =

    let logger = LogManager.GetLogger("SvgFormat")
    let logDebug = Debug(logger)

    exception ParseException of string

    let MATERIAL_COLOR(color:Color) = 
        new Material( 
            name=color.ToString(),
            ambient=Color4(0.2f),
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=20.0f,
            emissive=color.ToColor4()
        )

    type RectFilter () =
        inherit NodeFilter()
        override this.AcceptNode(node:Node) =
            if node.NodeName.Equals("class") then NodeFilter.FILTER_ACCEPT else NodeFilter.FILTER_REJECT

    // ----------------------------------------------------------------------------------------------------
    // Path
    // ----------------------------------------------------------------------------------------------------
    // 1. Mit id, name
    //      <path d="M1633.1 472.8l2.2-2.4 4.6-3.6-0.1 3.2-0.1 4.1-2.7-0.2-1.1 2.2-2.8-3.3z" id="BN" name="Brunei Darussalam">
    //
    // 2. mit Class
    //       <path class="Canada" d="M 680.3 187.6 677.9 187.7 672.1 185.8 668.6 182.8 670.5 182.3 676.4 183.9 680.6 186.5 680.3 187.6 Z">
    // ----------------------------------------------------------------------------------------------------    
    type Path(node:Element) =
        let mutable node = node
        let mutable name = ""
        let mutable points = new List<Vector3>()

        member this.Name 
            with get() = name
        
        member this.ForClass() = 
            name <- node.ClassName
            let values = node.Attributes.GetNamedItem("d")
            points <- this.parsePath (values.Value)  
            name, points

        member this.ForName() =        
            let named = node.Attributes.GetNamedItem("name")
            let name = named.NodeValue
            let pathElement = node :?>SVGPathElement 
            points <- this.parseSegments(pathElement)
            name, points

        member this.ForId() =        
            let named = node.Attributes.GetNamedItem("id")
            let name = named.NodeValue
            let pathElement = node :?>SVGPathElement 
            points <- this.parseSegments(pathElement)
            name, points

        member this.parsePath(values: string):List<Vector3> =
            let mutable points = new List<Vector3>()
            let input = noLetters(values).Trim()
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

        member this.parseSegments(element:SVGPathElement):List<Vector3> = 
            let mutable points = new List<Vector3>()
            let mutable lastPoint = Vector3.Zero
            let mutable firstPoint = Vector3.Zero
            let pathSegList = element.PathSegList |> Seq.toList

            let first = pathSegList.Head 

            match first.PathSegType with
            | SVGPathSeg.PATHSEG_MOVETO_ABS -> 
                let move = first :?> SVGPathSegMovetoAbs 
                lastPoint <- Vector3(move.X, 0.0f, move.Y)
            
            | _ -> 
                let message = "Unerwarteter Path Typ " + first.PathSegType.ToString() 
                raise (ParseException(message))

            points.Add(lastPoint)
            firstPoint <- lastPoint

            let rels = pathSegList.Tail
            for relSeg in rels do
                match relSeg.PathSegType with
                | SVGPathSeg.PATHSEG_LINETO_REL -> 
                    let rel = relSeg :?> SVGPathSegLinetoRel
                    let nextPoint = lastPoint + Vector3(rel.X , 0.0f, rel.Y)
                    points.Add(nextPoint)
                    lastPoint <- nextPoint 
                | SVGPathSeg.PATHSEG_CURVETO_CUBIC_REL ->  
                    let rel = relSeg :?> SVGPathSegCurvetoCubicRel
                    let nextPoint = lastPoint + Vector3(rel.X , 0.0f, rel.Y)
                    points.Add(nextPoint)
                    lastPoint <- nextPoint                 
                | SVGPathSeg.PATHSEG_LINETO_HORIZONTAL_REL ->                 
                    let rel = relSeg :?> SVGPathSegLinetoHorizontalRel
                    let nextPoint = lastPoint + Vector3(rel.X , 0.0f, lastPoint.Y)
                    points.Add(nextPoint)
                    lastPoint <- nextPoint                 
                | SVGPathSeg.PATHSEG_CLOSEPATH ->                 
                    let rel = relSeg :?> SVGPathSegClosePath
                    let nextPoint = firstPoint
                    points.Add(nextPoint)
                    lastPoint <- nextPoint  
                | SVGPathSeg.PATHSEG_LINETO_VERTICAL_REL -> 
                    let rel = relSeg :?> SVGPathSegLinetoVerticalRel
                    let nextPoint = lastPoint + Vector3(lastPoint.X , 0.0f, rel.Y)
                    points.Add(nextPoint)
                    lastPoint <- nextPoint                  
                | SVGPathSeg.PATHSEG_MOVETO_REL -> 
                    let rel = relSeg :?> SVGPathSegMovetoRel
                    lastPoint <- Vector3(rel.X , 0.0f, rel.Y) 
                | _ ->  
                    let message = "Unerwarteter Path Typ " + relSeg.PathSegType.ToString() 
                    raise (ParseException(message))
            points

        member this.Evaluate() =
            if node.ClassName <> null  && node.ClassName <> "" then
                this.ForClass()
            else 
                if  node.Attributes.GetNamedItem("name") <> null then
                    this.ForName()
                else    
                    if node.Attributes.GetNamedItem("id") <> null then
                        this.ForId()
                    else 
                        null, null            
                
    type Polygon(node:Element) =
        let mutable points = new List<Vector3>()

        member this.ForId() =        
            let named = node.Attributes.GetNamedItem("id")
            let name = named.Value
            let pathElement = node :?>SVGPolygonElement 
            points <- this.parsePolygon(pathElement)
            name, points
            
        member this.parsePolygon(element: SVGPolygonElement)  =  
            this.parsePoints(element.Points)

        member this.parsePoints(pointList: SVGPointList) =
            let mutable points = new List<Vector3>()
            let first = pointList.Item(uint64 0)
            let firstPoint =  
                Vector3(
                    first.X,
                    0.0f,
                    first.Y
                )
            for p in pointList do
                let point = 
                    Vector3(
                        p.X,
                        0.0f,
                        p.Y
                    )
                points.Add(point)
            points.Add(firstPoint)
            points

        member this.Evaluate() =  
            if node.Attributes.GetNamedItem("id") <> null then
                this.ForId()
            else 
                null, null   