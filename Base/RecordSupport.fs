namespace Base
//
//  RecordSupport.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open System.Collections.Generic

// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------
// Verarbeiten von Files, deren Zeilen Records darstellen, die von einem bestimmten Typ sind
// Es können Gruppen von Zeilen selektiert werden
// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------
module RecordSupport =

    type Comparator=string->bool

    type System.String with
        member this.FirstColumn() =
            if this.Length > 0 then
                this.Split(' ').[0].Trim()
            else ""

        member this.SecondColumn() =
            if this.Length > 1 then
                this.Split(' ').[1].Trim()
            else ""
        
        member this.FirstColumnIs(value:string) =
            this.FirstColumn() = value

    // ----------------------------------------------------------------------------------------------------
    //  Record Typ durch die erste Spalte eines Records erkennen
    // ----------------------------------------------------------------------------------------------------

    let comparesTyp         (typ: string)     (line: string) = line.FirstColumnIs(typ)
    let comparesNotTyp      (typ: string)     (line: string) = not (line.FirstColumnIs(typ))
    let comparesTypRange    (typen: string[]) (line: string) = 
        let mutable result = false
        for typ in typen do
            if line.FirstColumnIs(typ) then result <- true 
        result

    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // SELECT
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------

    let selectWith (list: string list, comp: Comparator) =
        list |> List.filter (fun line -> comp (line))

    let selectUntil (list: string list, compare: string -> bool) =
        list
        |> List.takeWhile (fun line -> not (compare (line)))

    let selectUntilType (list: string list, typ: string) =
        let list = selectUntil (list, comparesTyp (typ))
        let idx = list.Length
        list, idx

    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // FIND
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------

    // ----------------------------------------------------------------------------------------------------
    //  Finden der nächsten Zeile, bei der der Vergleich true ist.
    //  <returns>Die gefundene Zeile</returns>
    //  <returns>Der Index der Zeile</returns>
    //  <returns>Ende-Kennzeichen</returns>
    // ----------------------------------------------------------------------------------------------------
    let findWith (list: string list, comp: Comparator) =
        let mutable notFound = false
        let mutable line = ""
        let mutable idx = 0

        try
            line <- list |> List.find (fun line -> comp (line))
            idx  <- list |> List.findIndex (fun line -> comp (line))
        with :? KeyNotFoundException -> notFound <- true; idx <- line.Length

        line, idx, notFound

    let findOccurencesInFile (lines: string list, typ) =
        let compTyp = comparesTyp typ

        let resultLines =
            lines |> Seq.filter (fun line -> compTyp (line))

        let resultAnzahl = resultLines |> Seq.length
        resultLines, resultAnzahl

    let findOccurencesCount (lines: string list, typ) =
        let compTyp = comparesTyp typ

        lines
        |> Seq.filter (fun line -> compTyp (line))
        |> Seq.length

    let findOccurencesPositions (lines: string list, typ) =
        let compTyp = comparesTyp typ
        lines
        |> Seq.indexed 
        |> Seq.filter (fun line -> compTyp (snd line ))
        |> Seq.map (fun pair -> fst pair)

    let splitAtType (lines: string list, typ) =
        let mutable workLines:string list = []
        let mutable result:string list list = []
        let compTyp = comparesTyp typ
        let mutable atEnd = false 

        let ind = lines |> List.findIndex(fun line -> compTyp (line))
        let toBeOmitted, remainder = lines|> List.splitAt (ind)
        workLines <- remainder

        while not atEnd do
            try
                let ind = workLines.Tail |> List.findIndex(fun line -> compTyp (line))
                let sublist, remainder = workLines|> List.splitAt (ind)
                result <- result @ [sublist]
                let sublist, remainder = workLines|> List.splitAt (ind + 1)
                workLines <- remainder
            with :? KeyNotFoundException -> atEnd <- true            
        result <- result @ [workLines]
        result

    let findWithType (list: string list, typ) =
        let compTyp = comparesTyp typ
        findWith (list, compTyp)

    // ----------------------------------------------------------------------------------------------------
    //  Erkennen aller Zeilen einer Gruppe.
    // ----------------------------------------------------------------------------------------------------
    let findNextAndSplit (lines: string list, comp:Comparator) =
        let mutable atEnd = false
        let mutable nextHeader : string = ""
        let mutable headerPosition = 0
        let mutable result : string list = []
        let mutable remainder : string list = []

        try
            //  Auf den nächsten Header positionieren
            let header, iNext, innerEnd = findWith (lines, comp) 
            nextHeader      <- header
            headerPosition  <- iNext
            atEnd           <- innerEnd 

            if innerEnd  then 
                nextHeader <- "Ende"
                headerPosition <- lines.Length
                result <- lines
                atEnd <- true
            else
                //  Vor dem nächsten Header splitten gibt die aktuellen Lines
                let objLines, omitted = lines|> List.splitAt (iNext)
                result <-  objLines
                //  Nach dem nächsten Header splitten gibt die restlichen Lines
                let omitted, remaining = lines|> List.splitAt (iNext + 1)
                remainder <- remaining
        //  Keinen Header gefunden. Ende erreicht
        with :? KeyNotFoundException -> atEnd <- true          
        
        //  <returns>Den nächsten Header</returns>
        //  <returns>Die Positiondes Headers</returns>
        //  <returns>Die Zeilen der Gruppe</returns>
        //  <returns>Die restlichen Zeilen der Datei</returns>
        //  <returns>Ende-Kennzeichen</returns>

        nextHeader, headerPosition, result, remainder, atEnd 