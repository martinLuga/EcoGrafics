namespace DirectX
//
//  Kugel.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.

open System
open System.IO
open System.Threading
open SharpDX.XAudio2
open SharpDX.Multimedia

// ----------------------------------------------------------------------------------------------------
// Elemente und Aufbau des Flippers
// ----------------------------------------------------------------------------------------------------
module SoundSupport =

    let PLaySoundFile(fileName:string) =
        let device = new XAudio2()
        let masteringVoice = new MasteringVoice(device)
        device.StartEngine()
        let soundStream = new SoundStream(File.OpenRead(fileName))
        let mutable waveFormat = soundStream.Format
        let buffer = 
            new AudioBuffer(                         
                Stream = soundStream.ToDataStream(),
                AudioBytes = (int) soundStream.Length,
                Flags = BufferFlags.EndOfStream
            )        
                         
        let sourceVoice = new SourceVoice(device, waveFormat)
        sourceVoice.SubmitSourceBuffer(buffer, soundStream.DecodedPacketsInfo)
        sourceVoice.Start()

        let mutable count = 0
        while (sourceVoice.State.BuffersQueued > 0 && count < 30) do        
            Thread.Sleep(2)
            count <- count + 1

        sourceVoice.DestroyVoice()
        sourceVoice.Dispose()
        buffer.Stream.Dispose()

    type WavePlayer() =
        let device = new XAudio2()
        let masteringVoice = new MasteringVoice(device)
        let mutable sourceVoice:SourceVoice = null
        let mutable buffer:AudioBuffer = null

        member this.Play(fileName:string) =        
            device.StartEngine()
            let soundStream = new SoundStream(File.OpenRead(fileName))
            let mutable waveFormat = soundStream.Format
            buffer <- 
                new AudioBuffer(                         
                    Stream = soundStream.ToDataStream(),
                    AudioBytes = (int) soundStream.Length,
                    Flags = BufferFlags.EndOfStream
                )        
                         
            sourceVoice <- new SourceVoice(device, waveFormat)
            sourceVoice.SubmitSourceBuffer(buffer, soundStream.DecodedPacketsInfo)
            sourceVoice.Start()

        member this.Destroy() =
            sourceVoice.DestroyVoice()
            sourceVoice.Dispose()
            buffer.Stream.Dispose()


