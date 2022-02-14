namespace Base
//
//  Framework.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open System
open System.Collections.Generic
open System.Windows.Forms
open System.Diagnostics
open System.Threading
open System.Drawing
open System.IO
open log4net
open LoggingSupport

open SharpDX

// ----------------------------------------------------------------------------
// Immer benötigte Basis Funktionen
// Dateifunktionen
// String
// Logging
// ----------------------------------------------------------------------------
module Framework = 

    let logger = LogManager.GetLogger("Framework") 
    let logDebug = Debug(logger)

    let random = Random(0)
    
    let SYSTEMTIME = new Stopwatch()

    let getSystemTime() =
        SYSTEMTIME.ElapsedMilliseconds

    // ----------------------------------------------------------------------------------------------------
    //  Conversion
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------
    // type conversion
    // ----------------------------------------------------------------------------
    type System.Int32 with
        static member FromFloat(f:float32) =  Math.Round(float f) |> int 
        
    type System.Single with
        member x.ToInt() = Math.Round(float x) |> int 

    // ----------------------------------------------------------------------------------------------------
    //  String
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------
    // Char aus String entfernen
    // ----------------------------------------------------------------------------
    let stripchars chars str =
        Seq.fold (fun (str: string) chr -> str.Replace(chr |> Char.ToUpper |> string, "").Replace(chr |> Char.ToLower |> string, ""))
            str chars

    let stripcharsnl str =
        str |> stripchars System.Environment.NewLine 

    let mutable keyToggles = new Dictionary<Keys, bool>() 
    keyToggles.Item(Keys.Z) <- false
    keyToggles.Item(Keys.F) <- false

    let everyNth n elements =
        elements
        |> Seq.mapi (fun i e -> if i % n = n - 1 then Some(e) else None)
        |> Seq.choose id

    // ----------------------------------------------------------------------------------------------------
    //  Tuple
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------
    // First, second,.. for a 4-Tuple
    // ---------------------------------------------------------------------------- 
    let first  (c, _, _, _) = c
    let secnd  (_, c, _, _) = c
    let third  (_, _, c, _) = c
    let fourth (_, _, _, c) = c

    let asTuples list =
        let chunkSize = 3
        List.chunkBySize chunkSize list

     // 
    // Pretty print list
    // 
    let printList(list:float32 list) =
        List.fold (fun rstring (litem:float32) -> rstring + litem.ToString()) "" list

    let printArray(array:'T[]) =
        (Array.fold (fun rstring (litem:'T) -> rstring + " " + litem.ToString()) "" array).TrimStart()

    let formatVector (v: Vector3) =
        let xs = sprintf "%6.2f" v.X
        let ys = sprintf "%6.2f" v.Y
        let zs = sprintf "%6.2f" v.Z
        "X:" + xs + " Y:" + ys + " Z:" + zs  

    let formatVector2 (v: Vector2) =
        let xs = sprintf "%6.2f" v.X
        let ys = sprintf "%6.2f" v.Y
        "X:" + xs + " Y:" + ys   

    let fromArray3(x:float32[]) =
        Vector3( x.[0],   x.[1],   x.[2])

    let fromArray2(x:float32[]) =
        Vector2( x.[0], x.[1])

    // ----------------------------------------------------------------------------------------------------
    //  Dictionary
    // ----------------------------------------------------------------------------------------------------
    type Dictionary<'TKey, 'TValue> with  
        member this.Replace(key:'TKey, value:'TValue) =
            if this.ContainsKey(key) then
                this.Remove(key)|> ignore
            this.Add(key, value)
        member this.TryItem(key:'TKey, value:'TValue) =
            if this.ContainsKey(key) then
                this.Item(key)
            else
                this.Add(key, value)
                this.Item(key)

    let keyNotInDict(dict:Dictionary<'TKey, 'TValue>, key:'TKey) =
        if dict.ContainsKey(key) then
            key
        else 
            null

    let toSeq d = d |> Seq.map (fun (KeyValue(k,v)) -> (k,v))
    let toArray (d:IDictionary<_,_>) = d |> toSeq |> Seq.toArray
    let toList (d:IDictionary<_,_>) = d |> toSeq |> Seq.toList
    let ofMap (m:Map<'k,'v>) = new Dictionary<'k,'v>(m) :> IDictionary<'k,'v>
    let ofList (l:('k * 'v) list) = new Dictionary<'k,'v>(l |> Map.ofList) :> IDictionary<'k,'v>
    let ofSeq (s:('k * 'v) seq) = new Dictionary<'k,'v>(s |> Map.ofSeq) :> IDictionary<'k,'v>
    let ofArray (a:('k * 'v) []) = new Dictionary<'k,'v>(a |> Map.ofArray) :> IDictionary<'k,'v>

    let SignBool(d:float32) = if Math.Sign(d) > 0 then true else false

    // ----------------------------------------------------------------------------------------------------
    //  Random properties
    // ----------------------------------------------------------------------------------------------------
    let randPosInXZatY (min:Vector3, max:Vector3, fromHeight) = 
        Vector3(
            (random.Next((int)min.X,(int)max.X) |> float32 )  ,
            (fromHeight )  ,
            (random.Next((int)min.Z,(int)max.Z) |> float32 )  )

    let randomPositionFromTo(min:Vector3, max:Vector3) = 
        Vector3(
            (random.Next((int)min.X,(int)max.X) |> float32 )  ,
            (random.Next((int)min.Y,(int)max.Y) |> float32 )  ,
            (random.Next((int)min.Z,(int)max.Z) |> float32 )  )

    let randomDirection(range:float32) = 
        let v = 
            Vector3(
                random.NextFloat(-range, range) ,
                random.NextFloat(-range, range) ,
                random.NextFloat(-range, range) 
            )
        v.Normalize()
        v

    let randomDirectionFromTo(min:Vector3, max:Vector3) = 
        Vector3(
            (random.Next((int)min.X,(int)max.X) |> float32 )  ,
            (random.Next((int)min.Y,(int)max.Y) |> float32 )  ,
            (random.Next((int)min.Z,(int)max.Z) |> float32 )  )

    let randomSpeed(range:int) = 
            (random.Next(1,range) |> float32 ) / 100.0f

    let randomColor() = 
        let red = (random.Next(0,255) |> byte)  
        let green = (random.Next(0,255) |> byte)  
        let blue  = (random.Next(0,255) |> byte)  
        new Color(red ,green, blue, 1uy)

    let randDirV3() = 
        Vector3(
            (random.Next(-50,50) |> float32 ) / 100.0f,
            (random.Next(-50,50) |> float32 ) / 100.0f,
            (random.Next(-50,50) |> float32 ) / 100.0f)

    let randSpeed() = 
            (random.Next(1,10) |> float32 ) / 100.0f

    let randColor() = 
        let red = (random.Next(0,255) |> byte)  
        let green = (random.Next(0,255) |> byte)  
        let blue  = (random.Next(0,255) |> byte)  
        new Color(red ,green, blue, 1uy)

    //-----------------------------------------------------------------------------------------------------
    // Dicht beieinander
    //-----------------------------------------------------------------------------------------------------  
    let approximatelyEqual(a:float32, b:float32) =
        abs(b - a) < 1.0f   

    let approximatelySame (a:Vector3, b:Vector3) =
        Vector3.Distance (a,b) < 0.1f

    type Clock() =    
        let mutable _physicsTimer = new Stopwatch()
        let mutable _renderTimer = new Stopwatch() 
        let mutable _frameTimer = new Stopwatch() 
        let mutable _frameCount = 0

        member this.FrameCount 
            with get() = _frameCount
            and set(value) =  _frameCount <- value

        member this.PhysicsAverage =
            if (this.FrameCount = 0) then
                0.0 
            else
                (float) (_physicsTimer.ElapsedTicks / Stopwatch.Frequency)
                / (float) (this.FrameCount * 1000 ) 

        member this.RenderAverage        
            with get() =            
                if (this.FrameCount = 0) then
                    0.0
                else
                    (float)(_renderTimer.ElapsedTicks / Stopwatch.Frequency)
                    / (float) (this.FrameCount * 1000 )  

    type Dimensions = | XZ | XY | YZ

    type Interval =
        struct
            val Dim: Dimensions
            val Min1: float32
            val Min2: float32
            val Max1: float32
            val Max2: float32

            new(dim, minx, miny, maxx, maxy) =
                { Dim = dim
                  Min1 = minx
                  Min2 = miny
                  Max1 = maxx
                  Max2 = maxy }
        end 

    let printInterval(i:Interval) =
       "I [" + sprintf "%6.2f" i.Min1 + sprintf "%6.2f" i.Max1 + "]"  + " [" + sprintf "%6.2f" i.Min2 + sprintf "%6.2f" i.Max2 + "]"

    let inInterval1(value:float32, interval:Interval) = 
         (value >= interval.Min1) && (value <= interval.Max1) 

    let inInterval2(value:float32, interval:Interval) = 
        (value >= interval.Min2) && (value <= interval.Max2) 

    let maxVecInY (vec1:Vector3) (vec2:Vector3) =
        if vec1.Y > vec2.Y then vec1 else vec2

    let maxInY (points: Vector3 list)  = 
        points
        |> Seq.reduce maxVecInY 

    let _Boundaries (objectPosition: Vector3) =
        let defaultSize = 3.0f

        let Minimum =
            Vector3(objectPosition.X - defaultSize, objectPosition.Y - defaultSize, objectPosition.Z - defaultSize)

        let Maximum =
            Vector3(objectPosition.X + defaultSize, objectPosition.Y + defaultSize, objectPosition.Z + defaultSize)

        Minimum, Maximum 

    let _BoundingBox (objectPosition: Vector3) =
        let mutable box = BoundingBox()
        box.Minimum <- fst (_Boundaries (objectPosition))
        box.Maximum <- snd (_Boundaries (objectPosition))
        box

    let hitPoint(myPosition, anotherPosition) = 
        let mutable bb = _BoundingBox(myPosition)
        let mutable p = anotherPosition
        let mutable h = Vector3.Zero
        Collision.ClosestPointBoxPoint(&bb, &p, &h)
        h

    //-----------------------------------------------------------------------------------------------------
    // Thread
    //-----------------------------------------------------------------------------------------------------  
    let inThread(worker:unit->unit) =
        let task = new Thread(new ThreadStart(worker))
        task.Start()
        task

    let Delayer() = 
        for i= 0 to 10 do
            let line = sprintf  "--> %O  %i " "Delay: " i 
            logDebug(line)
            Thread.Sleep(10) 

    let delayed(worker:unit->unit) =
        // Start Delay task in new thread
        let delayTask = new Thread(new ThreadStart(Delayer))
        delayTask.Start()
        Thread.Sleep(0) 
        worker()
        // Wait for delay end and continue main task
        delayTask.Join()

    // Alle gleich i
    let ForAll seq i =
       if Seq.forall(fun num -> num = i) seq then true
       else false 
       
    // Alle ungleich i
    let ForAllNot seq i =
       if Seq.forall(fun num -> num <> i) seq then true
       else false

    // Alle den ersten = i
    // Gib den Index zurück
    let First (seq:int[]) i =
        let ind = seq |> Seq.findIndex(fun num -> num = i)
        seq.Length - 1  - ind 

    // Alle ungleich i
    let FirstNot (seq:int[]) i =
       seq.Length  - 1 -
           (seq |> Seq.findIndex(fun num -> num <> i))

    let ErsterFreie(spalte:int[]) =
        let mutable ind = 0
        try 
            ind <- spalte |> Seq.findIndexBack(fun num -> num = 0)
            ind
        with :? KeyNotFoundException -> - 1

    // Scan eine row
    // wenn 4 hintereinander gefunden werden
    // gib index des ersten zurück
    let findeVierHintereinander(rowOrColumn:int[], nr:int) =
        assert (rowOrColumn.Length >= 4 )
        let mutable found = false
        let mutable worker = Array.create 4 0  
        for i in 0 .. (rowOrColumn.Length - 4) do
            worker <- rowOrColumn.[i..i+3]
            if (ForAll worker nr) then
                found <- true 
        found 

    let Increment(summe: byref<float32>, amount:float32) =
        summe <- summe + amount

    let ByteArrayToImage (buffer: byte[], offset:int, count:int) =
        let ms = new MemoryStream(buffer, offset, count)
        ms.Position <- 0
        Image.FromStream(ms , true, false)  

    let ByteArrayToArray (buffer: byte[], offset:int, count:int) =
        let ms = new MemoryStream(buffer, offset, count)
        ms.Position <- 0
        ms.ToArray() 