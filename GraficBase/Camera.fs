namespace GraficBase
//
//  Camera.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open SharpDX

open Base.MathSupport
open Base.LoggingSupport
open Base.GlobalDefs

// ----------------------------------------------------------------------------------------------------
//  Camera 
//   Camera.Instance.ViewProj
//   Camera.Instance.World
// ----------------------------------------------------------------------------------------------------
module Camera =

    let logger = LogManager.GetLogger("Camera")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)
    let logWarn  = Warn(logger)

    let leftHanded(pos, target, up) =
        Matrix.LookAtLH(pos, target, up)

    let rightHanded(pos, target, up) =
        Matrix.LookAtRH(pos, target, up)

    let Matrix_lookAt(pos, target, up) =
        match ACTUAL_COORD_RULE with
        | CoordinatRule.RIGHT_HANDED -> rightHanded(pos, target, up)
        | CoordinatRule.LEFT_HANDED ->  leftHanded(pos, target, up)

    let persLeftHanded (fovY, aspectRatio, zNear, zFar) =
        Matrix.PerspectiveFovLH(fovY, aspectRatio, zNear, zFar)

    let persRightHanded (fovY, aspectRatio, zNear, zFar) =
        Matrix.PerspectiveFovRH(fovY, aspectRatio, zNear, zFar)

    let Matrix_Perspective (fovY, aspectRatio, zNear, zFar) =
        match ACTUAL_COORD_RULE with
        | CoordinatRule.RIGHT_HANDED -> persRightHanded (fovY, aspectRatio, zNear, zFar)
        | CoordinatRule.LEFT_HANDED -> Matrix.PerspectiveFovLH(fovY, aspectRatio, zNear, zFar)

    type Camera() =
        
        let mutable viewDirty   = true
        let mutable position    = Vector3.Zero
        let mutable right       = Vector3.UnitX 
        let mutable up          = Vector3.UnitY 
        let mutable look        = Vector3.UnitZ 
        let mutable target      = Vector3.Zero 
    
        let mutable nearZ:float32       = 0.0f
        let mutable farZ:float32        = 0.0f
        let mutable aspectRatio:float32 = 0.0f
        let mutable fovY:float32        = 0.0f

        let mutable nearWindowHeight:float32 = 0.0f
        let mutable farWindowHeight:float32 = 0.0f
    
        let mutable view  = Matrix.Identity 
        let mutable proj  = Matrix.Identity 
        let mutable world = Matrix.Identity 

        let mutable phi:float32      = 0.0f
        let mutable theta:float32    = 0.0f
        let mutable radius:float32   = 0.0f

        let mutable rotAmountHorizontal = 0.0f   
        let mutable rotAmountVertical   = 0.0f
        let mutable rotationStrength    = 0.0f
    
        do 
            fovY  <- MathUtil.PiOverFour  
            aspectRatio <- 1.0f 
            nearZ  <- 1.0f 
            farZ   <- 1000.0f 
            nearWindowHeight <- 2.0f * 1.0f * Tanf(0.5f * fovY) 
            farWindowHeight <-  2.0f * 1000.0f * Tanf(0.5f * fovY)     
            proj <-  Matrix_Perspective(fovY, aspectRatio, 1.0f, 1000.0f) 

        static let mutable instance = new Camera()
        static member Instance
            with get() = instance
            and set(value) = instance <- value
    
        member this.Position  
            with get() = position
            and set(value) = position <- value

        member this.Target
            with get() = target
            and set(value) = target <- value 

        member this.Right  
            with get() = right
            and set(value) = right <- value

        member this.Up
            with get() = up
            and set(value) = up <- value

        member this.Look
            with get() = look
            and set(value) = look <- value

        member this.Log() =
              "      " + "POS=" + position.ToString() + " TARG=" + target.ToString() + " ASP=" + aspectRatio.ToString()
            + "\n      " + "PHI=" + phi.ToString() + " THETA=" + theta.ToString() + " RAD=" + radius.ToString()
    
        member this.NearZ
            with get() = nearZ
            and set(value) = nearZ <- value

        member this.FarZ
            with get() = farZ
            and set(value) = farZ <- value 

        member this.AspectRatio
            with get() = aspectRatio
            and set(value) = aspectRatio <- value

        member this.FovY
            with get() = fovY
            and set(value) = fovY <- value

        member this.FovX
            with get() = 
                let halfWidth = 0.5f * this.NearWindowWidth
                2.0f * Atanf(halfWidth / this.NearZ)

        member this.NearWindowHeight
            with get() = nearWindowHeight
            and set(value) = nearWindowHeight <- value 

        member this.NearWindowWidth =
            this.AspectRatio * this.NearWindowHeight 

        member this.FarWindowHeight
            with get() = farWindowHeight
            and set(value) = farWindowHeight <- value 

        member this.FarWindowWidth =
            this.AspectRatio * this.FarWindowHeight
        
        member this.World
            with get() = world
            and set(value) = world <- value 
    
        member this.View
            with get() = view
            and set(value) = view <- value 

        member this.Proj
            with get() = proj
            and set(value) = proj <- value 
    
        member this.ViewProj =
            view * proj

        member this.Frustum =
            new BoundingFrustum(this.ViewProj)

        member this.Radius 
            with get() = radius                
            and set(value) = radius <- value 

        member this.RotAmountHorizontal
            with set(value) = rotAmountHorizontal <- value 

        member this.RotAmountVertical
            with set(value) = rotAmountVertical <- value 
    
        member this.SetLens(fovY:float32, aspect:float32, zn:float32, zf:float32) =
            this.FovY   <- fovY  
            this.AspectRatio <- aspect 
            this.NearZ  <- zn 
            this.FarZ   <- zf 
    
            this.NearWindowHeight <- 2.0f * zn * Tanf(0.5f * fovY) 
            this.FarWindowHeight <-  2.0f * zf * Tanf(0.5f * fovY) 
    
            this.Proj <-  Matrix_Perspective(fovY, aspect, zn, zf)  
    
        member this.LookAt(pos:Vector3, targ:Vector3, up:Vector3) =
            position <- pos 
            target <- targ 
            this.Look <- Vector3.Normalize(targ - pos) 
            this.Right <- Vector3.Normalize(Vector3.Cross(up, this.Look)) 
            this.Up <- Vector3.Cross(this.Look, this.Right) 
            radius <- Vector3.Distance (this.Position, this.Target)
            viewDirty <- true 
    
        member this.Strafe(d:float32) =
            this.Position <- this.Position + this.Right * d
            this.View <-  Matrix_lookAt(this.Position, this.Target, Vector3.Up)
    
        member this.Walk(d:float32) =
            this.Position <- this.Position + this.Look * d 
            this.View <-  Matrix_lookAt(this.Position, this.Target, Vector3.Up)
    
        member this.Pitch(angle:float32) =
            // Rotate up and look vector about the right vector.
    
            let r = Matrix.RotationAxis(this.Right, angle) 
    
            this.Up <- Vector3.TransformNormal(this.Up, r) 
            this.Look <- Vector3.TransformNormal(this.Look, r) 
    
            viewDirty <- true 
            
        member this.Zoom (delta:float32) = 
            radius <- radius + delta
            this.ToCartesian()
            this.View <-  Matrix_lookAt(this.Position, this.Target, Vector3.Up)

        // ----------------------------------------------------------------------------------------------------
        //  Rotate left / right
        // ----------------------------------------------------------------------------------------------------
        member this.RotateHorizontal(right:bool) =
            this.RotateHorizontalStrength(right, rotationStrength)

        member this.RotateVertical(up:bool) =
            this.RotateVerticalAmount(up, rotationStrength)

        member this.ChangeHorizontal(right:bool, theta, amount) =  
            if right then 
                match ACTUAL_COORD_RULE with
                | CoordinatRule.RIGHT_HANDED -> theta + amount
                | CoordinatRule.LEFT_HANDED  -> theta - amount
            else
                match ACTUAL_COORD_RULE with
                | CoordinatRule.RIGHT_HANDED -> theta - amount
                | CoordinatRule.LEFT_HANDED  -> theta + amount

        member this.RotateHorizontalStrength(right:bool, strength) =
            theta <- this.ChangeHorizontal(right, theta, rotAmountHorizontal * strength)
            theta <- MathUtil.Clamp(theta, -MathUtil.TwoPi, MathUtil.TwoPi)
            logDebug("Rotating horizontally" + "phi=" + phi.ToString() + " theta=" + theta.ToString())
            this.ToCartesian()
            view <- Matrix_lookAt(this.Position, this.Target, Vector3.Up)

        member this.RotateVerticalAmount(up:bool, strength) =
            let mutable direction = ""
            if up then 
                direction <- " up "
                phi <- phi - rotAmountVertical * strength
            else
                direction <- " down "
                phi <- phi + rotAmountVertical * strength
            phi <- MathUtil.Clamp(phi, 0.1f, MathUtil.Pi - 0.1f)
            logDebug("Rotating vertically" + direction + "phi=" + phi.ToString() + " theta=" + theta.ToString())
            this.ToCartesian()
            view <- Matrix_lookAt(this.Position, this.Target, Vector3.Up)

        // ----------------------------------------------------------------------------------------------------
        //  Conversion Spherical to Cartesian coordinates.
        // ----------------------------------------------------------------------------------------------------
        member this.ToCartesian() =
            position <- ToCartesian(phi, theta, radius)
            logDebug("Result position=" + position.ToString())

        member this.InitRadians() = 
            let (p, t) = ToRadians(position, radius)  
            phi   <- p
            theta <- t

        member this.Init(cameraPosition, cameraTarget, aspectRatio, horizontal, vertical, stren) = 
            this.LookAt(cameraPosition, cameraTarget, Vector3.Up)
            this.SetLens(MathUtil.PiOverFour, aspectRatio, 1.0f, 1000.0f)
            this.View <- Matrix_lookAt(position, target, Vector3.Up)
            rotAmountHorizontal <- horizontal
            rotAmountVertical <- vertical
            rotationStrength <- stren
            this.InitRadians()
            logInfo("Initialized \n" + this.Log())

        member this.Reset(cameraPosition, cameraTarget) =
            this.LookAt(cameraPosition, cameraTarget, Vector3.Up)
            this.SetLens(MathUtil.PiOverFour, aspectRatio, 1.0f, 1000.0f)
            this.View <- Matrix_lookAt(this.Position, this.Target, Vector3.Up)
            this.InitRadians()
            logInfo("Reset to \n" + this.Log())