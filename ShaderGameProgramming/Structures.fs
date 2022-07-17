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

    type Material
        (
            name: string,
            diffuseAlbedo: Vector4,
            fresnelR0: Vector3,
            roughness: float32,
            matTransform: Matrix,
            matCBIndex,
            normalSrvHeapIndex,
            diffuseSrvHeapIndex
        ) =
        let mutable name = name

        let mutable diffuseAlbedo = diffuseAlbedo
        let mutable fresnelR0 = fresnelR0
        let mutable roughness = roughness
        let mutable matTransform = matTransform

        let mutable matCBIndex = -1
        let mutable normalSrvHeapIndex = -1
        let mutable diffuseSrvHeapIndex = -1
        let mutable numFramesDirty = -1

        // Unique material name for lookup.
        member this.Name
            with get () = name
            and set (value) = name <- value

        // Index into constant buffer corresponding to this material.
        member this.MatCBIndex
            with get () = matCBIndex
            and set (value) = matCBIndex <- value

        // Index into SRV heap for diffuse texture.
        member this.DiffuseSrvHeapIndex
            with get () = diffuseSrvHeapIndex
            and set (value) = diffuseSrvHeapIndex <- value

        // Index into SRV heap for normal texture.
        member this.NormalSrvHeapIndex
            with get () = normalSrvHeapIndex
            and set (value) = normalSrvHeapIndex <- value

        // Dirty flag indicating the material has changed and we need to update the constant buffer.
        // Because we have a material constant buffer for each FrameResource, we have to apply the
        // update to each FrameResource. Thus, when we modify a material we should set
        // NumFramesDirty = NumFrameResources so that each frame resource gets the update.
        member this.NumFramesDirty
            with get () = numFramesDirty
            and set (value) = numFramesDirty <- value

        // Material constant buffer data used for shading.
        member this.DiffuseAlbedo
            with get () = diffuseAlbedo
            and set (value) = diffuseAlbedo <- value

        member this.FresnelR0
            with get () = fresnelR0
            and set (value) = fresnelR0 <- value

        member this.Roughness
            with get () = roughness
            and set (value) = roughness <- value

        member this.MatTransform
            with get () = matTransform
            and set (value) = matTransform <- value
