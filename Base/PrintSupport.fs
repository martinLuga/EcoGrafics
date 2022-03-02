namespace Base
//
//  PrintSupport.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.

open System
open log4net 

// ----------------------------------------------------------------------------------------------------
// PrintSupport
//  Formatierte  Anzeige von Vector , Matrix etc
// ----------------------------------------------------------------------------------------------------
module PrintSupport =

    let logger = LogManager.GetLogger("Utility")

    let mutable matrix: float32 [,] = Array2D.create 6 7 0.0f

    let displayAsString (num: float32) = num.ToString()

    let dispElement (matrix: float32 [,], line: int, col: int) = displayAsString (matrix.[line, col])

    let displayLine (lineNr: int) =
        let line =
            String.Format(
                "          | {0} {1} {2} {3} {4} {5} {6} |",
                dispElement (matrix, lineNr, 0),
                dispElement (matrix, lineNr, 1),
                dispElement (matrix, lineNr, 2),
                dispElement (matrix, lineNr, 3),
                dispElement (matrix, lineNr, 4),
                dispElement (matrix, lineNr, 5),
                dispElement (matrix, lineNr, 6)
            )

        System.Console.WriteLine(line)

    let dline (line: float32 []) =
        let mutable lineS = ""

        for i in 0 .. line.Length - 1 do
            lineS <- lineS + sprintf "%.2f  " line[i]

        lineS

    let subarray (index: float32 [], pos:int, len:int) =
        let mutable line:float32 [] = Array.zeroCreate len 

        for i in 0 .. len-1 do
            line.[i] <- index.[pos + i]

        line

    let dmatrix (matrix: float32 [], i:int, j:int) =
        let mutable mat = ""

        for i in 0 .. i - 1 do
            let line = subarray(matrix, i, j) 
            mat <- mat + "\n" + dline (line)

        mat

    let drow (matrix: float32 [,], lineNr: int) =
        let mutable line = ""
        let slice = matrix.[lineNr, 0 .. matrix.GetLength(1) - 1]
        dline (slice)

    let darray (index: float32 []) =
        let mutable line = ""

        for i in 0 .. index.Length - 1 do
            line <- line + index.[i].ToString()

        line

    let dindex (index: seq<int []>) =
        let mutable line = ""
        let gewEnum = index.GetEnumerator()

        while gewEnum.MoveNext() = true do
            let index = gewEnum.Current
            line <- line + sprintf "(%i,%i) " index.[0] index.[1]

        logger.Debug(line)
