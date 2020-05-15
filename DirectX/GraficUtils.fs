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

    let ToRadians(v:Vector3) =
        let x  = v.X * v.X
        let y  = v.Y * v.Y
        let z  = v.Z * v.Z
        let r = sqrt (x + y + z) 
        let phi = atan (v.Y / v.X)
        let theta = acos (v.Z / r)
        (r, phi, theta)

    let ToCartesian(phi:float32, theta:float32, r:float32) =
        let x = r * cos phi * sin theta
        let y = r * sin phi * sin theta
        let z = r * cos theta
        new Vector3(x,y,z)

    // ----------------------------------------------------------------------------------------------------
    // Lineare Algebra Rotation
    // ----------------------------------------------------------------------------------------------------

    // ----------------------------------------------------------------------------------------------------
    //  Rotationen Yaw pitch roll 
    // ----------------------------------------------------------------------------------------------------
    // Yaw pitch roll -> Quaternion 
    let rotation (pitch:float32, yaw:float32, roll:float32) = 
        Quaternion.RotationYawPitchRoll (yaw, pitch, roll)

    //  Quaternion -> Yaw pitch roll 
    let toPitchYawRoll(q:Quaternion) =
        let mutable pitch = 0.0f
        let mutable pitchB = 0.0f
        let mutable yaw = 0.0f

        // roll (x-axis rotation)
        let sinr_cosp = 2.0f * (q.W  * q.X + q.Y * q.Z) 
        let cosr_cosp = 1.0f - 2.0f * (q.X * q.X + q.Y * q.Y)
        let roll = atan2 sinr_cosp cosr_cosp  
        
        // pitch (y-axis rotation)
        let sinp = +2.0f * (q.W * q.Y - q.Z * q.X) 
        if  abs(sinp) >= 1.0f then
            let s = sign(sinp) 
            pitch <- MathUtil.PiOverTwo * (float32) s       // use 90 degrees if out of range
        else
            pitch <- asin(sinp) 
        
        // yaw (z-axis rotation)
        let siny_cosp = 2.0f * (q.W * q.Z + q.X * q.Y) 
        let cosy_cosp = 1.0f - 2.0f * (q.Y * q.Y + q.Z * q.Z)   
        yaw <- atan2 siny_cosp cosy_cosp  
        (pitch, yaw, roll)

    //-----------------------------------------------------------------------------------------------------
    // Rotationen um die Achsen
    // TODO: Target noch nicht berücksichtigt
    //-----------------------------------------------------------------------------------------------------  
    let rotationMatrixYPR(rotHorizontal, rotVertical) = 
        let oRotationQuat = Quaternion.RotationYawPitchRoll(rotHorizontal, rotVertical, 0.0f) 
        Matrix.RotationQuaternion(oRotationQuat) 

    // Generate a rotation matrix for rotation around Y-axis
    let rotationMatrixHor(rotHorizontal) = 
        let oRotationQuat = Quaternion.RotationAxis(Vector3.UnitY, rotHorizontal)
        Matrix.RotationQuaternion(oRotationQuat) 

    // Generate a rotation matrix for rotation around X-axis
    let rotationMatrixVert(rotVertical) = 
        let oRotationQuat = Quaternion.RotationAxis(Vector3.UnitX, rotVertical)
        Matrix.RotationQuaternion(oRotationQuat) 

    let rotate(initViewMatrix:Matrix, rotHorizontal, rotVertical) =
         let rh = rotationMatrixHor(rotHorizontal) 
         let rv = rotationMatrixVert(rotVertical) 
         let mat =  Matrix.Multiply(rh, initViewMatrix)
         Matrix.Multiply(rv, mat)

    //-----------------------------------------------------------------------------------------------------
    // Drehung eines Objekts, so dass es zwischen Punkt p1 und p2 liegt
    //-----------------------------------------------------------------------------------------------------     
    let rotateBetween(p1:Vector3, p2:Vector3) =   
        p1.Normalize()
        p2.Normalize()
        let mutable v = Vector3.Cross(p1,p2)            // Vektor, senkrecht zu beiden
        let angle = acos(Vector3.Dot(p1, p2))           // Winkel zwischen v1, v2 
        Quaternion.RotationAxis(v, angle)               // Drehung um die Achse v mit Winkel w  

    let euler(v1: Vector3, v2: Vector3) =            
        let q = rotateBetween(v1, v2)                      // Winkel zwischen v1, v2  
        toPitchYawRoll(q)     

    //-----------------------------------------------------------------------------------------------------
    // Die Euler-Winkel bestimmen die Lage des Objekts im Raum 
    //
    // Pitch (the x component) is the rotation about the node’s x-axis.
    // Yaw (the y component) is the rotation about the node’s y-axis.
    // Roll (the z component) is the rotation about the node’s z-axis
    // Implementierung 2
    //-----------------------------------------------------------------------------------------------------
    let eulerAngle( p1:Vector3, p2:Vector3) =            
        let w = p2 - p1        
        let l = w.Length
        let lxz = sqrt( w.X * w.X +  w.Z * w.Z)
            
        // PITCH
        let mutable pitch = 0.0f
        let mutable pitchB = 0.0f
        if w.Y < 0.0f then
            pitchB <- MathUtil.Pi - asin(lxz) 
        else  
            pitchB <- asin(lxz)
             
        if w.Z = 0.0f then
            pitch <- pitchB
        else 
            pitch <- pitchB * (float32) (sign(w.Z))  
            
        // YAW
        let mutable yaw = 0.0f
        if w.X = 0.0f && w.Z = 0.0f then
            yaw <- 0.0f
        else 
            let mutable inner =  w.X  / sin (pitch) 
            if inner > 1.0f then
                yaw <- MathUtil.PiOverTwo
            else 
                if inner < -1.0f then
                    yaw <- MathUtil.PiOverTwo
                else  
                    yaw <- asin(inner)
        (yaw, pitch, 0.0f)

    //-----------------------------------------------------------------------------------------------------
    // Begrenzungen, um die Singulatitäten bei Drehungen im Kreis zu vermeiden
    // Deprecated jetzt durch MatUtil.Clamp
    //-----------------------------------------------------------------------------------------------------  
    let adjustIIPi(angle) =        
        if angle > MathUtil.TwoPi then
            angle - MathUtil.TwoPi
        else if angle < -MathUtil.TwoPi then
            angle + MathUtil.TwoPi
        else angle

    let limitPIHalbe(angle) =        
        if angle >= MathUtil.PiOverTwo then
            MathUtil.PiOverTwo  - 0.01f
        else if angle <= -MathUtil.PiOverTwo then
            -MathUtil.PiOverTwo + 0.01f
        else angle