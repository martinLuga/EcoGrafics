namespace ExampleSurfaces
//
//  DigitSupport.fs
//
//  Created by Martin Luga on 06.07.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open SharpDX
open System.IO
open log4net
open Base.ModelSupport
open Base.ObjectBase
open Base.MaterialsAndTextures
open Base.LoggingSupport
open Surfaces


// ----------------------------------------------------------------------------------------------------
// ZifferAnzeige mit Hilfe von Segmenten
//  Animiert
// ----------------------------------------------------------------------------------------------------

module DigitSupport = 

    let logger = LogManager.GetLogger("DigitSupport")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger) 

    let MAT_BACKGROUND  = MAT_BLACK
    let MAT_NOT_SET     = MAT_BACKGROUND
    let MAT_SET         = MAT_DGROD

    let getSegmente(zahl) =
        //    | "a"  // oben             
        //    | "b"  // rechts oben
        //    | "c"  // rechts unten
        //    | "d"  // unten           
        //    | "e"  // unten links
        //    | "f"  // oben links
        //    | "g"  // mitte    
        match zahl with
        | "1" -> [      "b" ; "c"                           ] 
        | "2" -> [ "a"; "b" ;       "d" ; "e"       ; "g"   ] 
        | "3" -> [ "a"; "b" ; "c" ; "d" ;             "g"   ] 
        | "4" -> [      "b" ; "c" ;             "f" ; "g"   ] 
        | "5" -> [ "a";       "c" ; "d" ;       "f" ; "g"   ] 
        | "6" -> [ "a";       "c" ; "d" ; "e" ; "f" ; "g"   ] 
        | "7" -> [ "a"; "b" ; "c"                           ] 
        | "8" -> [ "a"; "b" ; "c" ; "d" ; "e" ; "f" ; "g"   ] 
        | "9" -> [ "a"; "b" ; "c" ; "d" ;       "f" ; "g"   ] 
        | "0" -> [ "a"; "b" ; "c" ; "d" ; "e" ; "f"         ] 
        | _ ->                
            let message = "Keine Zahl " + zahl 
            raise (System.Exception(message))

    let getTexture(zahl) =

        match zahl with
        | "1" -> TEXT_ONE 
        | "2" -> TEXT_TWO
        | "3" -> TEXT_THREE
        | "4" -> TEXT_FOUR
        | "5" -> TEXT_FIFE
        | "6" -> TEXT_SIX
        | "7" -> TEXT_SEVEN 
        | "8" -> TEXT_EIGHT 
        | "9" -> TEXT_NINE
        | "0" -> TEXT_NULL
        | "." -> TEXT_POINT 
        | _ ->                
            let message = "Keine Zahl " + zahl 
            raise (System.Exception(message))

    // ----------------------------------------------------------------------------------------------------
    // Oberklasse für animierte Objekte 
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>] 
    [<AbstractClass>] 
    type AnimatedObject(name: string, display: Display, position:Vector3, rotation:Matrix, scale:Vector3 ) =
        inherit BaseObject (name, display, position, rotation, scale)

        let mutable listener:string = "" // TODO
        
        new (name, position, rotation, scale) = AnimatedObject(name, new Display(), position, rotation, scale)
        new (name, position, scale) = AnimatedObject(name, new Display(), position, Matrix.Identity, scale)

        member this.Listener 
            with get() = listener

    // ----------------------------------------------------------------------------------------------------
    // Anzeige einer Ziffer bestehend aus 7 Segmenten
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]  
    type ZifferSegment(name: string, objekt:BaseObject, position:Vector3, rotation:Matrix, scale:Vector3 ) =  
        inherit AnimatedObject (name, objekt.Display, position, rotation, scale)

        new (name, objekt) = ZifferSegment(name,  objekt, Vector3.Zero, Matrix.Identity, Vector3.One) 
        new (name, objekt, position) = ZifferSegment(name, objekt, position, Matrix.Identity, Vector3.One)

        member this.PartWithName (name: string) =
            this.Display.Parts |> List.find (fun part -> part.Name = name)

        member this.InitFromXML(file:File) =
            ()

        member this.AsXML() =
            ()

        member this.ShowInitialized() =
            this.Display.Parts |> List.iter (fun p -> p.Material <- MAT_SET)

        member this.ShowNothing() =
            this.Display.Parts |> List.iter (fun p -> p.Material <- MAT_NOT_SET)

        member this.Show(number:int) =
            this.ShowInitialized()   
            let numString = number.ToString()
            let segmentsToBeHilited = getSegmente(numString)
            segmentsToBeHilited 
                |> List.map (fun s -> this.PartWithName (s))
                |> List.iter(fun part -> part.Material <- MAT_SET)

    // ----------------------------------------------------------------------------------------------------
    // Anzeige einer Ziffer dargestellt durch eine Textur
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]  
    type ZifferTexture(name: string, position:Vector3, laenge:float32, hoehe:float32, rotation:Matrix, scale:Vector3 ) =  
        inherit AnimatedObject (name, new Display(), position, rotation, scale)

        new (name, position, laenge, hoehe) = ZifferTexture(name, position, laenge, hoehe, Matrix.Identity, Vector3.One)

        member this.Part =
            if this.Display.Parts.IsEmpty then
                this.Display.AddPart(
                    PART_PLANE("NUMBR", Vector3.Zero, 0, laenge , hoehe, MAT_NONE, TEXT_EMPTY)
                )
            this.Display.Parts.[0]

        member this.InitFromXML(file:File) =
            ()

        member this.AsXML() =
            ()

        member this.ShowInitialized() =
            this.Part.Texture <- TEXT_EMPTY

        member this.Show(number:int) =
            let numString = number.ToString()
            this.Part.Texture <- getTexture(numString) 

    // ----------------------------------------------------------------------------------------------------
    // Anzeige mehrerer Ziffern dargestellt durch eine Textur
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]  
    type MultiZifferTexture(name:string, anz:int, position:Vector3, laenge:float32, hoehe:float32, rotation:Matrix, scale:Vector3 ) =  
        inherit AnimatedObject (name, new Display(), position, rotation, scale)
        let mutable count = 0

        new (name, anz, position, laenge, hoehe) = MultiZifferTexture(name, anz, position,  laenge , hoehe, Matrix.Identity, Vector3.One)

        member this.Count 
            with get() = count

        member this.Parts =
            if this.Display.Parts.IsEmpty then
                for i in 0..anz-1 do
                    let partName = "NUMBR" + i.ToString()
                    this.Display.AddPart(
                        PART_PLANE(partName, Vector3.Zero, i, laenge, hoehe, MAT_NONE, TEXT_EMPTY)
                    )
            this.Display.Parts 

        member this.InitFromXML(file:File) =
            ()

        member this.AsXML() =
            ()

        member this.ResetCounter() =
            for i in 0..anz-1 do
                this.Digit(0, i)
        
        member this.SetCount(counter:int) =
            count <- counter
            this.ShowCount()

        member this.ShowCount() =
            this.ResetCounter()
            let output = sprintf "%04i" count   
            for i in 0..anz-1 do
                this.Digit(int(output.Substring(i,1)), i) 

        member this.Show(number:int) =
            this.ResetCounter()
            this.Digit(number, anz-1)          

        member this.Digit(number:int, slot:int) =
            let numString = number.ToString()
            this.Parts.[slot].Texture <- getTexture(numString) 

        member this.AddCounter(addTo:int) =
            count <- count + addTo
            logDebug(this.Name + " ADD " + addTo.ToString()) 
            logDebug(this.Name + " COUNT " + count.ToString()) 
            this.ShowCount()