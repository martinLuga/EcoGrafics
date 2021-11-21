namespace Base
//
//  Framework.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open System.Collections.Generic
open Framework

// ----------------------------------------------------------------------------
// Kapsel für Matrix-Funktionen
//  Zeilen
//  Spalten
//  Diagonalen
// ----------------------------------------------------------------------------

module BracketObject = 

    type Richtung = | Haupt=0 | Gegen=1

    type Bra(vector, indexe) =

        let mutable vector:int[] = vector 
        let mutable index:int[,] = indexe

        new (dim) = Bra (Array.create dim 0, Array2D.create dim 2 0 )

        static member createRow (pvector:int[], irow) =
            let vector = pvector            
            let colDim = pvector.Length
            let index = Array2D.create colDim 2 0
            for j in 0.. colDim-1 do 
                index.[j, 0] <- irow
                index.[j, 1] <- j
            new Bra(vector, index)

        static member createColumn (pvector:int[], jCol) =
            let vector = pvector               
            let rowDim = pvector.Length
            let index = Array2D.create rowDim 2 0
            for i in 0.. rowDim-1 do 
                index.[i, 0] <- i
                index.[i, 1] <- jCol
            new Bra(vector, index)

        static member createDiagonal  (pvector:int[], pindex) =
            let vector = pvector               
            let rowDim = pvector.Length
            let index = pindex
            new Bra(vector, index)

        member this.Vector 
            with get() = vector
            and set(value) = vector <- value

        member this.Index 
            with get() = index
            and set(value) = index <- value

        member this.IndexTrimNull() =
            let mutable istart = 0 
            let mutable istop  = 0 
            let mutable ileng  = 0 
            for i in 0 .. index.GetLength(0)-1 do
                 if index.[i,0] > 0 && index.[i,1] > 0 then
                    istart <- i
            for i in istart .. index.GetLength(0)-1 do
                 if index.[i,0] = 0 && index.[i,1] = 0 then
                    istop <- i
            ileng <- istop - istart
            let result = Array2D.create ileng 2 0
            let mutable ii = 0
            for i in istart .. istop do
                result[ii,0] <- index.[i,0]
                result[ii,1] <- index.[i,1]
                ii <- ii + 1
            result



        member this.Copy (abra :Bra ) = 
            vector <- abra.Vector

        member this.GetIndexAt(ind:int) = 
            index.[ind,*]

        member this.SetIndexAt(ind:int, pindex:int[]) = 
            index.[ind,*] <- pindex

        member this.SetValueAt(ind:int, value:int) = 
            vector.[ind] <- value

    type Ket(colDim) =

        let mutable vector:int[] = Array.create colDim 0 
        let mutable index:List<int[]> = new List<int[]>() 

        member this.Vector 
            with get() = vector
            and set(value) = vector <- value

        member this.Initialize (pvector:int[]) = 
            vector <- pvector

        member this.Copy (aket :Ket ) = 
            vector <- aket.Vector


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

        member this.GetWert(nrow, ncol) =
            matrix.[nrow,ncol]

        member this.SetWert(nrow, ncol, wert) =
            matrix.[nrow, ncol] <- wert

        member this.GetZeile(nrow) =
            Bra.createRow ( 
                matrix.[nrow,*], 
                nrow
            )              

        member this.SetZeile(nrow, zeile) =
            matrix.[nrow,*] <- zeile

        member this.GetSpalte(ncol) =
            Bra.createColumn ( 
                matrix.[*,ncol], 
                ncol
            )

        member this.SetSpalte(ncol, spalte) =
            matrix.[*,ncol] <- spalte

        member this.diagHaupt(iStart, jStart) =
            let resultDim = min rowDim colDim
            let mutable bra:Bra =  new Bra(resultDim)
            let mutable result:int[] =  Array.create resultDim 0
            let mutable ii = 0
            let mutable i = iStart
            let mutable j = jStart
            while i <= rowDim-1 && j <= colDim-1 do
                if matrix.[i,j] > 0 then
                    bra.SetIndexAt(ii, [|i;j|])
                    bra.SetValueAt(ii, matrix.[i,j])
                    result.[ii] <- matrix.[i,j]
                    ii <- ii + 1
                i <- i + 1
                j <- j + 1
            bra 
            
        member this.diagGegen(iStart, jStart) =
            let resultDim = min rowDim colDim
            let mutable result:int[] = Array.create resultDim 0
            let mutable bra:Bra =  new Bra(resultDim)
            let mutable ii = 0
            let mutable i = iStart
            let mutable j = jStart
            while i >= 0 && j <= rowDim do
                if matrix.[i,j] > 0 then
                    bra.SetIndexAt(ii, [|i;j|])
                    bra.SetValueAt(ii, matrix.[i,j])
                    result.[ii] <- matrix.[i,j]
                    ii <- ii + 1
                i <- i - 1
                j <- j + 1
            bra   

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
                |> Seq.map(fun i -> this.GetDiagonale(i, 0, Richtung.Gegen))
            |> Seq.append ( 
                seq{0 .. 1 .. colDim-1}
                    |> Seq.map(fun j -> this.GetDiagonale(rowDim-1, j, Richtung.Gegen))
                )
            |> Seq.toList

        member this.GetDiagonalen() =
            this.GetGegenDiagonalen()
                |> List.append (this.GetHauptDiagonalen())

        member this.GetSpalten() =
            seq{colDim-1.. -1 .. 0}
               |> Seq.map(fun j -> this.GetSpalte(j))
               |> Seq.toList

        member this.GetZeilen() =
            seq{rowDim-1.. -1 .. 0}
               |> Seq.map(fun j -> this.GetZeile(j))
               |> Seq.toList

        member this.GetElements() =
            this.GetZeilen() 
                |> List.append(this.GetSpalten())
                |> List.append(this.GetDiagonalen())