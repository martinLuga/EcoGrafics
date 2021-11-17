namespace Base
//
//  Framework.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open GlobalDefs
open log4net
open LoggingSupport
open SharpDX
open System
open System.Collections.Generic

// ----------------------------------------------------------------------------
// Mathematische Funktionen
// Vektor
// Matrix
// Geometric
// ----------------------------------------------------------------------------
module MathSupport =

    let logger = LogManager.GetLogger("Framework") 
    let logDebug = Debug(logger)

    let AngleBetween180(vector1:Vector3, vector2:Vector3) =
        let dot = Vector3.Dot(vector1, vector2)

        let v1bet = vector1.Length()
        let v2bet = vector1.Length()

        let cosphi = dot / (v1bet * v2bet)

        acos(cosphi) 

    let AngleBetween(vector1:Vector3, vector2:Vector3) =
        let angle = (atan2 vector2.Z vector2.X) - (atan2 vector1.Z  vector1.X)
        if angle < 0.0f then angle + MathUtil.TwoPi else angle

    // Die Normale als Kreuzprodukt von 2 Vektoren 
    // Bei 3 Punkten
    // Kreuzprodukt ist RH 
    // Händigkeit beachten
    let createNormal p1 p2 p3 =   
        let u = p3 - p1
        let v = p3 - p2
        let norm = Vector3.Cross(u,v)
        norm.Normalize()
        match ACTUAL_COORD_RULE with
        | CoordinatRule.RIGHT_HANDED -> norm
        | CoordinatRule.LEFT_HANDED -> -norm

    let Sinf(a:float32) = float32 (Math.Sin(float a))
    let Cosf(d:float32) = float32 (Math.Cos(float d))
    let Tanf(a:float32) = float32 (Math.Tan(float a))
    let Atanf(d:float32) =  float32 (Math.Atan(float d))
    let Atan2f(y:float32, x:Int64) =  float32 (Math.Atan2(float y, float x))
    let Acosf(d:float32) =  float32 (Math.Acos(float d))
    let Expf(d:float32) =  float32 (Math.Exp(float d))
    let Sqrtf(d:float32) =  float32 (Math.Sqrt(float d))
    
    // Polarkoordinaten gehen vom Ursprung aus
    let ToRadians(position:Vector3, radius) = 
        let phi   =    Acosf(position.Y / radius)
        let theta =  - Atan2f(position.Z, (int64) position.X) + MathUtil.Pi
        (phi, theta)
    
    // polar angle θ
    // azimuthal angle φ 
    // Polarkoordinaten gehen vom Ursprung aus
    let pointToRadians(point:Vector3) =
        let x  = point.X * point.X
        let y  = point.Y * point.Y
        let z  = point.Z * point.Z
        let r = sqrt (x + y + z) 
        let xy = sqrt (x + y) 
        let phi = atan (xy / point.Z)
        let theta = acos (point.Z / r)
        (phi, theta, r)
 
    let ToCartesian(phi:float32, theta:float32, radius:float32) =
        let x = - radius * sin phi * cos theta 
        let y =   radius * cos phi 
        let z =   radius * sin phi * sin theta  
        Vector3(x,y,z)

    // 
    // Minimum / Maximum
    // 
    let maxVector3 (vec1:Vector3) (vec2:Vector3) =
        Vector3.Max(vec1,vec2)

    let minVector3 (vec1:Vector3) (vec2:Vector3) =
        Vector3.Min(vec1,vec2)

    let computeCenter (minV:Vector3) (maxV:Vector3)  =
        Vector3(
            minV.X + (maxV.X - minV.X) / 2.0f,
            minV.Y+  (maxV.Y - minV.Y) / 2.0f,
            minV.Z+  (maxV.Z - minV.Z) / 2.0f 
        )

    let computeMinimum (points: Vector3 list) =
        if points.Length = 0 then Vector3.Zero
        else 
            let min = points |> List.reduce minVector3  
            min

    let computeMaximum (points: Vector3 list) =
        if points.Length = 0 then Vector3.Zero
        else 
            let max = points |> List.reduce maxVector3 
            max

    let computeSchwerpunkt(vektoren:Vector3 list) =
        let summe = 
            vektoren
            |> Seq.reduce (fun v1 v2 -> Vector3.Add(v1, v2))
        let einsdurchM = 1.0f / (float32) vektoren.Length
        summe * einsdurchM

    let  _random = new Random() 
    let Rand(minValue:int, maxValue:int) = _random.Next(minValue, maxValue)
    let Randf() = _random.NextFloat(0.0f, 1.0f)
    let RandfBetween(minValue:float32, maxValue:float32) = _random.NextFloat(minValue, maxValue) 
    
    //-----------------------------------------------------------------------------------------------------
    // Dicht beieinander
    //-----------------------------------------------------------------------------------------------------  
    let approximatelyEqual(a:float32, b:float32) =
        abs(b - a) < 0.9f   

    let approximatelySame (a:Vector3, b:Vector3) =
        Vector3.Distance (a,b) < 0.1f

    //-----------------------------------------------------------------------------------------------------
    // Array sliceing
    //----------------------------------------------------------------------------------------------------- 
    // return: die Elemente, die Indexe in der Matrix
    let sliceVertical(matrix:int[,], spalte) =
        let resultIndex:List<int[]> = new List<int[]>()
        
        for i in 0 .. matrix.GetLength(1) - 1 do 
            resultIndex.Add([|spalte;i|])

        let result = matrix.[*,spalte]
        (result, resultIndex)
                
    // return: die Elemente, die Indexe in der Matrix
    let sliceHorizontal(matrix:int[,], zeile) =
        let resultIndex:List<int[]> = new List<int[]>()
        
        for j in 0 .. matrix.GetLength(0) - 1 do 
            resultIndex.Add([|zeile;j|])

        let result = matrix.[zeile, *]
        (result, resultIndex)

    // Ermitteln der Diagonalen parallel zur Hauptdiagonalen
    // Start bei spaltenindex
    // return: die elemente der Diagonalen, die Indixe in der Matrix
    let sliceHauptDiagonale(matrix:int[,], zeile, spalte) =
        logger.Debug("Zeile=" + zeile.ToString() + " Spalte=" + spalte.ToString())
        let rowDim = matrix.GetLength(0) 
        let colDim = matrix.GetLength(1)
        let resultDim = min rowDim colDim
        let resultIndex:List<int[]> = new List<int[]>()
        let mutable result:int[] =  Array.create resultDim 0

        assert(rowDim > 1)
        assert(colDim > 1)
        assert(not((zeile > 1) && (spalte > 1)))

        if spalte >= 0 then
            result <- Array.create resultDim 0
            let mutable i = 0
            for j in spalte-1 .. colDim - 1 do 
                if i <= rowDim - 1 then
                    result.[i] <- matrix.[i, j]
                    resultIndex.Add([|i; j|])
                    i <- i + 1
        else
            if zeile >= 0 then
                result <- Array.create resultDim 0
                let mutable j = 0
                for i in zeile-1 .. rowDim - 1 do 
                    if j <= colDim - 1 then
                        result.[j] <- matrix.[i, j]
                        resultIndex.Add([|i; j|])
                        j <- j + 1
            else 
                Array.create resultDim 0|> ignore
        (result, resultIndex)

    // Ermitteln der Diagonalen parallel zur Gegendiagonalen
    // Start bei Zeilenindex
    // return: die elemente der Diagonalen, die Indixe in der Matrix
    let sliceGegenDiagonale(matrix:int[,], zeile, spalte) =
        logger.Debug("Zeile=" + zeile.ToString() + " Spalte=" + spalte.ToString())
        let rowDim = matrix.GetLength(0) 
        let colDim = matrix.GetLength(1)
        let resultDim = min rowDim colDim
        assert(rowDim > 1)
        assert(colDim > 1)
        assert(not((zeile > 1) && (spalte > 1)))
        let mutable iresult = 0        
        let resultIndex:List<int[]> = new List<int[]>()
        let mutable result:int[] =  Array.create resultDim 0

        if spalte >= 0 then
            result <-  Array.create resultDim 0
            iresult <- 0
            let mutable i = rowDim - 1
            for j in spalte-1 .. colDim - 1 do 
                if i >= 0 then
                    result.[iresult] <- matrix.[i, j]
                    resultIndex.Add([|i; j|])
                    iresult <- iresult + 1
                    i <- i - 1 
        else
            if zeile >= 0 then
                result <-  Array.create resultDim 0
                iresult <- 0
                let mutable j = 0
                for i in zeile-1 .. -1 .. 0 do 
                    if j >= 0 then
                        result.[iresult] <- matrix.[i, j]
                        resultIndex.Add([|i; j|])
                        iresult <- iresult + 1
                        j <- j + 1 
            else 
                Array.create resultDim 0|> ignore
        (result, resultIndex)

