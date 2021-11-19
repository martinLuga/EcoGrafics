namespace ecografics
//
//  Shader.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System
open log4net
open NUnit.Framework
open Base.MathSupport
open Base.LoggingSupport
open Base.BracketObject
open System.Collections.Generic

module SlicingTests = 

    configureLoggingInMap "EcoGrafics" "UnitTests" "resource" "log4net.config"
    let logger= LogManager.GetLogger("FrameworkTests")
    logger.Info("Setup\n")

    let dline(slice:int[]) = 
        let mutable line  = "" 
        for i in 0 .. slice.GetLength(0) - 1 do
            line <- line + sprintf "%i  " slice.[i] 
        logger.Debug(line) 

    let drow(matrix:int[,], lineNr:int ) = 
        let mutable line  = ""         
        let slice = matrix.[lineNr, 0..matrix.GetLength(1)-1]
        dline(slice) 

    let dmatrix(matrix:int[,] ) = 
        for i in 0 .. matrix.GetLength(0)-1  do  
            drow(matrix, i); 
        logger.Debug(" ")

    let darray(index:int[])=
        let mutable line  = "" 
        for i in 0 .. index.Length-1  do
            line <- line + index.[i].ToString()
        line

    let dindex(index: List<int[]>) =
        let mutable line  = "" 
        for i in 0 .. index.Count-1  do 
            line <- line + sprintf "  (%s) " (darray(index.Item(i)))
        logger.Debug(line) 

    // ----------------------------------------------------------------------------------------------------
    //  Test des Matrix Slicings 
    // ----------------------------------------------------------------------------------------------------
    [<TestFixture>]
    type HauptDiagonaleTests() = 

        [<DefaultValue>] val mutable matrix:int[,]

        [<SetUp>]
        member this.setUp() =            
            this.matrix <- 
                array2D [
                    [1;1;1;1;1;1;1];
                    [2;2;2;2;2;2;2];
                    [3;3;3;3;3;3;3];
                    [4;4;4;4;4;4;4];
                    [5;5;5;5;5;5;5];
                    [6;6;6;6;6;6;6];
                    ]
            
            dmatrix(this.matrix)

        [<Test>]
        member this.ObereDiagonale_2() =
            logger.Info("Test")
            let (diagonale, index) = sliceHauptDiagonale(this.matrix, -1, 2)
            dline(diagonale)
            dindex(index)

        [<Test>]
        member this.ObereDiagonale_3() =
            logger.Info("Test")
            let (diagonale, index) = sliceHauptDiagonale(this.matrix, -1, 3)
            dline(diagonale)
            dindex(index)

        [<Test>]
        member this.ObereDiagonale_4() =
            logger.Info("Test")
            let (diagonale, index) = sliceHauptDiagonale(this.matrix, -1, 4)
            dline(diagonale)
            dindex(index)

        [<Test>]
        member this.ObereDiagonale_5() =
            logger.Info("Test")
            let (diagonale, index) = sliceHauptDiagonale(this.matrix, -1, 5)
            dline(diagonale)
            dindex(index)

        [<Test>]
        member this.ObereDiagonale_6() =
            logger.Info("Test")
            let (diagonale, index) = sliceHauptDiagonale(this.matrix, -1, 6)
            dline(diagonale)
            dindex(index)

        [<Test>]
        member this.UntereDiagonale_2() =
            logger.Info("Test")
            let (diagonale, index) = sliceHauptDiagonale(this.matrix,  2, -1)
            dline(diagonale) 
            dindex(index)

        [<Test>]
        member this.UntereDiagonale_3() =
            logger.Info("Test")
            let (diagonale, index) = sliceHauptDiagonale(this.matrix,  3, -1)
            dline(diagonale) 
            dindex(index)

        [<Test>]
        member this.UntereDiagonale_4() =
            logger.Info("Test")
            let (diagonale, index) = sliceHauptDiagonale(this.matrix,  4, -1)
            dline(diagonale) 
            dindex(index)
    
        [<Test>]
        member this.UntereDiagonale_5() =
            logger.Info("Test")
            let (diagonale, index) = sliceHauptDiagonale(this.matrix,  5, -1)
            dline(diagonale) 
            dindex(index)

    [<TestFixture>]
    type GegenDiagonaleTests() = 

        [<DefaultValue>] val mutable matrix:int[,]

        [<SetUp>]
        member this.setUp() =            
            this.matrix <- 
                array2D [
                    [1;1;1;1;1;1;1];
                    [2;2;2;2;2;2;2];
                    [3;3;3;3;3;3;3];
                    [4;4;4;4;4;4;4];
                    [5;5;5;5;5;5;5];
                    [6;6;6;6;6;6;6];
                    ]
        
            dmatrix(this.matrix)

        [<Test>]
        member this.ObereDiagonale_2() =
            logger.Info("Test")
            let (diagonale, index) = sliceGegenDiagonale(this.matrix, -1, 2)
            dline(diagonale)
            dindex(index)

        [<Test>]
        member this.ObereDiagonale_3() =
            logger.Info("Test")
            let (diagonale, index) = sliceGegenDiagonale(this.matrix, -1, 3)
            dline(diagonale)
            dindex(index)

        [<Test>]
        member this.ObereDiagonale_4() =
            logger.Info("Test")
            let (diagonale, index) = sliceGegenDiagonale(this.matrix, -1, 4)
            dline(diagonale)
            dindex(index)

        [<Test>]
        member this.ObereDiagonale_5() =
            logger.Info("Test")
            let (diagonale, index) = sliceGegenDiagonale(this.matrix, -1, 5)
            dline(diagonale)
            dindex(index)

        [<Test>]
        member this.ObereDiagonale_6() =
            logger.Info("Test")
            let (diagonale, index) = sliceGegenDiagonale(this.matrix, -1, 6)
            dline(diagonale)
            dindex(index)
        
        //

        [<Test>]
        member this.UntereDiagonale_5() =
            logger.Info("Test")
            let (diagonale, index) =  sliceGegenDiagonale(this.matrix,  5, -1)
            dline(diagonale) 
            dindex(index)

        [<Test>]
        member this.UntereDiagonale_4() =
            logger.Info("Test")
            let (diagonale, index) = sliceGegenDiagonale(this.matrix, 4, -1)
            dline(diagonale) 
            dindex(index)

        [<Test>]
        member this.UntereDiagonale_3() =
            logger.Info("Test")
            let (diagonale, index) = sliceGegenDiagonale(this.matrix,  3, -1)
            dline(diagonale) 
            dindex(index)
    
        [<Test>]
        member this.UntereDiagonale_2() =
            logger.Info("Test")
            let (diagonale, index) = sliceGegenDiagonale(this.matrix,  2, -1)
            dline(diagonale) 
            dindex(index)

    // ----------------------------------------------------------------------------------------------------
    //  Test des Matrix Slicings 
    // ----------------------------------------------------------------------------------------------------
    [<TestFixture>]
    type BracketTests() = 

        [<DefaultValue>] val mutable bracket:Bracket

        [<SetUp>]
        member this.setUp() =            
            let matrix = 
                array2D [
                    [1;1;1;1;1;1;1];
                    [2;2;2;2;2;2;2];
                    [3;3;3;3;3;3;3];
                    [4;4;4;4;4;4;4];
                    [5;5;5;5;5;5;5];
                    [6;6;6;6;6;6;6];
                    ]
            this.bracket <- new Bracket(6,7)
            this.bracket.Initialize(matrix)
            dmatrix(matrix)

        [<Test>]
        member this.ObereHauptDiagonale_Alle() =
            for j = 0 to 5 do 
                let diagonale1 = this.bracket.GetDiagonale(0, j, Richtung.Haupt)
                dline(diagonale1)

        [<Test>]
        member this.UntereHauptDiagonale_Alle() =
            for i = 0 to 4 do 
                let diagonale1 = this.bracket.GetDiagonale(i, 0, Richtung.Haupt)
                dline(diagonale1)

        [<Test>]
        member this.ObereGegenDiagonale_Alle() =
            for i in 5 .. -1 .. 0 do 
                let diagonale1 = this.bracket.GetDiagonale(i, 6, Richtung.Gegen)
                dline(diagonale1)

        [<Test>]
        member this.UntereGegenDiagonale_Alle() =
            for j in 6 .. -1 .. 0  do 
                let diagonale1 = this.bracket.GetDiagonale(5, j, Richtung.Gegen)
                dline(diagonale1)

        [<Test>]
        member this.getHauptDiagonalen() =
            let diagonalen = this.bracket.GetHauptDiagonalen()
            for i in 0 .. diagonalen.Length-1 do 
                dline(diagonalen.[i])

        [<Test>]
        member this.getGegenDiagonalen() =
            let diagonalen = this.bracket.GetGegenDiagonalen()
            for i in 0 .. diagonalen.Length-1 do 
                dline(diagonalen.[i])