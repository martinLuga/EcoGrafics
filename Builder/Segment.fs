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
    type SegmentBuilder(_segment:BaseObject, color:Color) = 

        let mutable segment = _segment

        let mutable objekte = new List<BaseObject>()

        let mutable delta = Vector3(20.0f, 0.0f, 0.0f)

        member this.Build (input:string, position) =
            
            if input.Length = 1 then
                this.BuildOne(input, position)
            else 
                this.BuildAll(input, position)

        member this.BuildOne(zahl, position) = 

            let objekt = new BaseObject(zahl, position)

            let segmentsToBeHilited = this.Segment(zahl)

            let parts = segment.Display.Parts

            let PartWithName(name:string) =
                parts |> List.find (fun part -> part.Name = name)

            let newParts = 
                segmentsToBeHilited 
                |> List.map(fun s -> PartWithName(s)) 

            objekt.Display.Parts <- newParts

            objekte.Add(objekt)

        member this.BuildAll(zahlen:string, position) = 
            let ueberAlleZahlen = zahlen.GetEnumerator()
            let mutable pos = position
            while ueberAlleZahlen.MoveNext() do
                let zahl = ueberAlleZahlen.Current
                this.BuildOne(zahl.ToString(), pos)
                pos <- pos + delta

        member this.Segment(zahl):string list =
            //    | "a"  // oben             
            //    | "b"  // rechts oben
            //    | "c"  // rechts unten
            //    | "d"  // unten           
            //    | "e"  // unten links
            //    | "f"  // oben links
            //    | "g"  // mitte    
            match zahl with
            | "1" -> [      "b" ; "c"                           ]     // OK 
            | "2" -> [ "a"; "b" ;       "d" ; "e"       ; "g"   ]     // OK
            | "3" -> [ "a"; "b" ; "c" ; "d" ;             "g"   ]     // OK
            | "4" -> [      "b" ; "c" ;             "f" ; "g"   ]     // OK
            | "5" -> [ "a";       "c" ; "d" ;       "f" ; "g"   ]     // OK
            | "6" -> [ "a";       "c" ; "d" ; "e" ; "f" ; "g"   ]     // OK 
            | "7" -> [ "a"; "b" ; "c"                           ]     // 
            | "8" -> [ "a"; "b" ; "c" ; "d" ; "e" ; "f" ; "g"   ]     // 
            | "9" -> [ "a"; "b" ; "c" ; "d" ;       "f" ; "g"   ]     // 
            | "0" -> [ "a"; "b" ; "c" ; "d" ; "e" ; "f"         ]     // 
            | _ ->                
                let message = "Keine Zahl " + zahl 
                raise (System.Exception(message))

        member this.Objects = 
            objekte |> Seq.toList
           

