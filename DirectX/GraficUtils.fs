namespace DirectX
//
//  Framework.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX
open SharpDX.Direct3D12
open SharpDX.DXGI
open SharpDX.Mathematics.Interop

// ----------------------------------------------------------------------------------------------------
// Utility 
// Bitmap und Texture
// STATUS: IN_ARBEIT
// ----------------------------------------------------------------------------------------------------
module GraficUtils = 

    type Device = SharpDX.Direct3D12.Device 
    type Resource = SharpDX.Direct3D12.Resource 

    let TextureWidth = 256 
    let TextureHeight = 256 
    let TexturePixelSize = 4    // The number of bytes used to represent a pixel in the texture.

    let textureDescription width height =
        ResourceDescription.Texture2D(
            Format.R8G8B8A8_UNorm,
            width,
            height
        ) 
    
    let ToTransparentColor(color:Color4) = 
        let mutable color4 = color
        color4.Alpha <- 0.5f
        color4

    let ToRawColor4(color:Color) = 
        let c4 = color.ToColor4() 
        let RED  = c4.Red
        let GREEN = c4.Green
        let BLUE = c4.Blue
        let ALPHA = c4.Alpha
        new RawColor4(RED, GREEN, BLUE, ALPHA)

    let ToRawColor4FromDrawingColor(color:System.Drawing.Color) = 
        let RED  = (float32)color.R
        let GREEN = (float32)color.G
        let BLUE = (float32)color.B
        let ALPHA = (float32)color.A
        new RawColor4(RED, GREEN, BLUE, ALPHA)

    let ToRawViewport(viewport:ViewportF) = 
        let mutable rvp = new RawViewportF()
        rvp.X <- viewport.X
        rvp.Y <- viewport.Y
        rvp.Height <- viewport.Height
        rvp.Width <- viewport.Width 
        rvp.MinDepth <- viewport.MinDepth
        rvp.MaxDepth <- viewport.MaxDepth
        rvp

    let ToRawRectangle(scissorRect:RectangleF) = 
        let mutable rr = RawRectangle()
        rr.Bottom <- (int)scissorRect.Bottom
        rr.Top <- (int)scissorRect.Top
        rr.Left <- (int)scissorRect.Left
        rr.Right <- (int)scissorRect.Right 
        rr
            
    // Copy the vertexes to the vertex buffer.
    let copyToBuffer(gpuBuffer:Resource, elements:'T[]) =
        let pDataBegin = gpuBuffer.Map(0) 
        Utilities.Write(pDataBegin, elements, 0, elements.Length) |> ignore
        gpuBuffer.Unmap(0) 

    // ----------------------------------------------------------------------------------------------------
    //  Builds a matrix that can be used to reflect vectors about a plane.
    // ----------------------------------------------------------------------------------------------------
    //  <param name="plane">The plane for which the reflection occurs. This parameter is assumed to be normalized.</param>
    //  <result>When the method completes, contains the reflection matrix.</result>
    let Reflection(plane:Plane) = 
        let mutable result = new Matrix() 
        let num1 = plane.Normal.X
        let num2 = plane.Normal.Y
        let num3 = plane.Normal.Z
        let num4 = -2.0f * num1
        let num5 = -2.0f * num2
        let num6 = -2.0f * num3
        result.M11 <- num4 *  num1 + 1.0f
        result.M12 <- num5 * num1
        result.M13 <- num6 * num1
        result.M14 <- 0.0f
        result.M21 <- num4 * num2
        result.M22 <- num5 * num2 + 1.0f
        result.M23 <- num6 * num2
        result.M24 <- 0.0f
        result.M31 <- num4 * num3
        result.M32 <- num5 * num3
        result.M33 <- num6 * num3 + 1.0f
        result.M34 <- 0.0f
        result.M41 <- num4 * plane.D
        result.M42 <- num5 * plane.D
        result.M43 <- num6 * plane.D
        result.M44 <- 1.0f
        result 

    // ----------------------------------------------------------------------------------------------------
    //  Creates a matrix that flattens geometry into a shadow.
    // ----------------------------------------------------------------------------------------------------
    //  <param name="light">The light direction. If the W component is 0, the light is directional light if the
    //  W component is 1, the light is a point light.</param>
    //  <param name="plane">The plane onto which to project the geometry as a shadow. This parameter is assumed to be normalized.</param>
    //  <result>When the method completes, contains the shadow matrix.</result>
    let Shadow(light:Vector4, plane:Plane) =
        let mutable result = new Matrix()  
        let num1 =  plane.Normal.X * light.X + plane.Normal.Y * light.Y + plane.Normal.Z * light.Z +  plane.D *  light.W 
        let num2 = -plane.Normal.X
        let num3 = -plane.Normal.Y
        let num4 = -plane.Normal.Z
        let num5 = -plane.D
        result.M11 <- num2 * light.X + num1
        result.M21 <- num3 * light.X
        result.M31 <- num4 * light.X
        result.M41 <- num5 * light.X
        result.M12 <- num2 * light.Y
        result.M22 <- num3 * light.Y + num1
        result.M32 <- num4 * light.Y
        result.M42 <- num5 * light.Y
        result.M13 <- num2 * light.Z
        result.M23 <- num3 * light.Z
        result.M33 <- num4 * light.Z + num1
        result.M43 <- num5 * light.Z
        result.M14 <- num2 * light.W
        result.M24 <- num3 * light.W
        result.M34 <- num4 * light.W
        result.M44 <- num5 * light.W + num1
        result 