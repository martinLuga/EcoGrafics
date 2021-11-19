namespace Base
//
//  Framework.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open Framework

// ----------------------------------------------------------------------------
// Kapsel für Matrix-Funktionen
//  Zeilen
//  Spalten
//  Diagonalen
// ----------------------------------------------------------------------------

module BracketObject = 

    type Richtung = | Haupt=0| Gegen=1

    type Bracket(rowDim, colDim) =
        let mutable matrix:int[,] = Array2D.create rowDim colDim 0 

        member this.Matrix 
            with get() = matrix
            and set(value) = matrix <- value

        member this.Initialize (pmatrix:int[,]) = 
            matrix <- pmatrix

        member this.Copy (abracket:Bracket) = 
            matrix <- abracket.Matrix

        member this.Reset() =
            this.Initialize(Array2D.create rowDim colDim 0)

        member this.Setze(col:int, wert:int) = 
            // Bereits komplett besetzt
            let spalte = this.GetSpalte(col)
            // Finde die höchste Zeile = 0
            let ersteFreie = ErsterFreie(spalte)
            logDebug("Erste freie Zeile: " + ersteFreie.ToString())
            // Besetze mit Wert
            if ersteFreie >= 0 then
                matrix.[ersteFreie, col] <- wert
            else 
                logDebug("Spalte komplett ")
            ersteFreie

        member this.GetZeile(nrow) =
            matrix.[nrow,*]

        member this.GetSpalte(ncol) =
            matrix.[*,ncol]

        member this.diagHaupt(iStart, jStart) =
            let resultDim = min rowDim colDim
            let mutable result:int[] =  Array.create resultDim 0
            let mutable ii = 0
            let mutable i = iStart
            let mutable j = jStart
            while i <= rowDim-1 && j <= colDim-1 do
                result.[ii] <- matrix.[i,j]
                ii <- ii + 1
                i <- i + 1
                j <- j + 1
            result   
            
        member this.diagGegen(iStart, jStart) =
            let resultDim = min rowDim colDim
            let mutable result:int[] = Array.create resultDim 0
            let mutable ii = 0
            let mutable i = iStart
            let mutable j = jStart
            while i >= 0 && j >=  0 do
                result.[ii] <- matrix.[i,j]
                ii <- ii + 1
                i <- i - 1
                j <- j - 1
            result   

        member this.GetDiagonale(iStart, jStart, richtung:Richtung) =
            match richtung with
            | Richtung.Haupt -> this.diagHaupt(iStart, jStart)
            | Richtung.Gegen -> this.diagGegen(iStart, jStart)
            | _ -> this.diagHaupt(iStart, jStart)

        member this.GetHauptDiagonalen() =
            seq{0..colDim-1}
                |> Seq.map(fun j -> this.GetDiagonale(0, j, Richtung.Haupt))
            |> Seq.append ( 
                seq{0..rowDim-1}
                    |> Seq.map(fun i -> this.GetDiagonale(i, 0, Richtung.Haupt))
                )
            |> Seq.toList

        member this.GetGegenDiagonalen() =
            seq{rowDim-1 .. -1 .. 0}
                |> Seq.map(fun i -> this.GetDiagonale(i, colDim-1, Richtung.Gegen))
            |> Seq.append ( 
                seq{colDim-1.. -1 .. 0}
                    |> Seq.map(fun i -> this.GetDiagonale(rowDim-1, i, Richtung.Gegen))
                )
            |> Seq.toList

        member this.GetDiagonalen() =
            this.GetGegenDiagonalen()
                |> List.append (this.GetHauptDiagonalen())

        member this.GetSpalten() =
            seq{colDim-1.. -1 .. 0}
               |> Seq.map(fun j -> this.GetSpalte(j))
               |> Seq.toList

        member this.GetRows() =
            seq{rowDim-1.. -1 .. 0}
               |> Seq.map(fun j -> this.GetZeile(j))
               |> Seq.toList

        member this.GetElements() =
            this.GetRows() 
                |> List.append(this.GetSpalten())
                |> List.append(this.GetDiagonalen())

        member this.hatGefunden(spielerNr:int)  =
            let mutable found = false
            // Für alle Spalte, Rows, Diagonalen
            for elem in this.GetElements() do
                found <- findeVierHintereinander(elem, spielerNr)
            found