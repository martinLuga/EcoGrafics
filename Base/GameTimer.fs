namespace Base
//
//  GameTimer.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Diagnostics

module GameTimer = 

    type GameTimer() =
    
        let mutable _secondsPerCount:double = 0.0 
        let mutable _deltaTime:double =  -1.0 

        let mutable _baseTime:int64   =  0L
        let mutable _pausedTime:int64 =  0L
        let mutable _stopTime:int64 =  0L
        let mutable _prevTime:int64 =  0L
        let mutable _currTime:int64 =  0L

        let mutable  _stopped = false

        do        
            Debug.Assert(Stopwatch.IsHighResolution, "System does not support high-resolution performance counter")
            _secondsPerCount <- 0.0
            _deltaTime <- -1.0
            _baseTime <- 0
            _pausedTime <- 0
            _prevTime <- 0
            _currTime <- 0
            _stopped <- false
            let countsPerSec = double Stopwatch.Frequency
            let inv = 1.0 / countsPerSec
            _secondsPerCount <- double (inv) 

        member this.TotalTime         
            with get() =            
                if (_stopped) then
                    (float32)(((double _stopTime - double _pausedTime) - double _baseTime) * _secondsPerCount)
                else
                    (float32)(((double _currTime - double _pausedTime) - double _baseTime) * _secondsPerCount)

        member this.DeltaTime =
            (float32)_deltaTime

        member this.Reset() =        
            let curTime = Stopwatch.GetTimestamp()
            _baseTime <- curTime
            _prevTime <- curTime
            _stopTime <- 0
            _stopped <- false        

         member this.Start() =        
            let startTime = Stopwatch.GetTimestamp()
            if (_stopped) then            
                _pausedTime <- _pausedTime + (startTime - _stopTime)
                _prevTime <- startTime
                _stopTime <- 0
                _stopped <- false

          member this. Stop() =        
            if not _stopped then            
                let curTime = Stopwatch.GetTimestamp()
                _stopTime <- curTime
                _stopped <- true

         member this.Tick() =        
            if _stopped  then            
                _deltaTime <- 0.0
            else
                let curTime = Stopwatch.GetTimestamp()
                _currTime <- curTime
                _deltaTime <- (double _currTime - double _prevTime) * (_secondsPerCount)

                _prevTime <- _currTime

                if _deltaTime < 0.0 then
                    _deltaTime <- 0.0