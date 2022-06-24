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

open Svg

// ----------------------------------------------------------------------------------------------------
// SVGFormat
//  bestehend aus Vertex und Index-Liste
// ----------------------------------------------------------------------------------------------------
module Segment =

    let logger = LogManager.GetLogger("Builder.Segment")
    let logDebug = Debug(logger)

    // ----------------------------------------------------------------------------------------------------
    // SegmentBuilder
    // ----------------------------------------------------------------------------------------------------
    type SegmentBuilder(color:Color) =
        let mutable svgBuilder = new SvgBuilder("model2d\\7-segment_cdeg.svg") 
        let mutable zahlObjekt:BaseObject = null

        member this.Build 
            (
                zahl:string,
                position: Vector3,
                height: float32,
                material: Material,
                texture: Texture,
                sizeFactor: Vector3,
                visibility: Visibility,
                augmentation: Augmentation,
                quality: Quality,
                shaders: ShaderConfiguration
            ) =

            let zahlAnzeige = 
                svgBuilder.Build( 
                    "cdeg",
                    "0",
                    height,
                    material,
                    texture, 
                    position,
                    sizeFactor,
                    visibility,
                    augmentation,
                    quality,
                    shaders
                ) 
                
            zahlObjekt <- svgBuilder.Objects.[0]
            let segmentsToBeHilited = this.Segment(zahl)

            let parts = zahlObjekt.Display.Parts

            let PartWithName(name:string) =
                parts |> List.find (fun part -> part.Name = name)

            for part in parts do 
                part.Material <- MAT_TRANSP

            for segment in segmentsToBeHilited do 
                let p =  PartWithName(segment)
                p.Material <- MAT_GREEN 

            //for part in zahlObjekt.Display.Parts do
            //    match part.Name with
            //    | "a" -> part.Material <- MAT_GREEN     // oben
            //    | "b" -> part.Material <- MAT_ORANGE    // rechts oben
            //    | "c" -> part.Material <- MAT_BLUE      // rechts unten
            //    | "d" -> part.Material <- MAT_DGROD     // unten
            //    | "e" -> part.Material <- MAT_DSGRAY    // unten links
            //    | "f" -> part.Material <- MAT_CYAN      // oben links
            //    | "g" -> part.Material <- MAT_YELLOW    // mitte              
            //    | _ ->  part.Material  <- MAT_BLACK

            zahlObjekt.Name <- zahl

        member this.Segment(zahl):string list =
            match zahl with
            | "1" -> [ "b"; "c"                     ]      
            | "2" -> [ "a"; "b"; "g" ; "e" ; "d"    ]
            | "3" -> [ "a"; "b"; "c"                ]
            | "4" -> [ "a"; "b"; "c"                ]
            | "5" -> [ "a"; "b"; "c"                ]
            | "6" -> [ "a"; "b"; "c"                ]
            | "7" -> [ "a"; "b"; "c"                ]
            | "8" -> [ "a"; "b"; "c"                ]
            | "9" -> [ "a"; "b"; "c"                ]
            | "0" -> [ "a"; "b"; "c"                ]
            | _ ->                
                let message = "Keine Zahl " + zahl 
                raise (System.Exception(message))

        member this.Object = 
            zahlObjekt
           

