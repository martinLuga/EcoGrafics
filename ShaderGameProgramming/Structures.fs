namespace ShaderGameProgramming
//
//  Structures.fs
//
//  Created by Martin Luga on 16.03.22.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Runtime.InteropServices

open System.Linq

open SharpDX

// ----------------------------------------------------------------------------------------------------
// Strukturen
// Luna, Frank D. Introduction To 3D Game Programming With Direct X 12
// ----------------------------------------------------------------------------------------------------
module Structures =

    let MAXLIGHTS = 16

    [<type: StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type Light =
        struct
            val mutable Strength: Vector3
            val mutable FalloffStart: float32 // Point/spot light only.
            val mutable Direction: Vector3 // Directional/spot light only.
            val mutable FalloffEnd: float32 // Point/spot light only.
            val mutable Position: Vector3 // Point/spot light only.
            val mutable SpotPower: float32 // Spot light only.

            new(strength, falloffStart, direction, falloffEnd, position, spotPower) =
                { Strength = strength
                  FalloffStart = falloffStart
                  Direction = direction
                  FalloffEnd = falloffEnd
                  Position = position
                  SpotPower = spotPower }
        end

    let DefaultLight =
        new Light(new Vector3(0.5f), 1.0f, Vector3.UnitY, 10.0f, Vector3.Zero, 64.0f)

    let DefaultLightArray: Light [] =
        Enumerable
            .Repeat(DefaultLight, MAXLIGHTS)
            .ToArray()

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type ObjectConstants =
        struct
            val mutable gView: Matrix
            val mutable gInvView: Matrix
            val mutable gProj: Matrix
            val mutable gInvProj: Matrix
            val mutable gViewProj: Matrix
            val mutable gInvViewProj: Matrix
            val mutable EyePosW: Vector3
            val mutable cbPerObjectPad1: float32
            val mutable gRenderTargetSize: Vector2
            val mutable gInvRenderTargetSize: Vector2
            val mutable gNearZ: float32
            val mutable gFarZ: float32
            val mutable gTotalTime: float32
            val mutable gDeltaTime: float32
            val mutable gAmbientLight: Color4
            val mutable gLights: Light []
        end

    [<type: StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type MaterialConstants =
        struct
            val DiffuseAlbedo: Vector4
            val FresnelR0: Vector3
            val Roughness: float32
            val MatTransform: Matrix // Used in texture mapping.

            new(diffuseAlbedo, fresnelR0, roughness, matTransform) =
                { DiffuseAlbedo = diffuseAlbedo
                  FresnelR0 = fresnelR0
                  Roughness = roughness
                  MatTransform = matTransform }
        end

    let DefaultMaterialConstants =
        new MaterialConstants(Vector4.One, new Vector3(0.01f), 0.25f, Matrix.Identity)