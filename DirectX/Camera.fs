namespace DirectX
//
//  Camera.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open SharpDX

open Base.MathHelper

open GraficUtils

// ----------------------------------------------------------------------------------------------------
// Camera 
// ----------------------------------------------------------------------------------------------------
module Camera =

    let DEFAULT_ROT_HORIZONTAL = MathUtil.TwoPi / 200.0f           // Scrollamount horizontal
    let DEFAULT_ROT_VERTICAL   = MathUtil.TwoPi / 200.0f           // Scrollamount vertical

    let logger = LogManager.GetLogger("Camera")

    type Camera() =
        
        let mutable viewDirty   = true
        let mutable position    = Vector3.Zero
        let mutable right       = Vector3.UnitX 
        let mutable up          = Vector3.UnitY 
        let mutable look        = Vector3.UnitZ 
        let mutable target      = Vector3.Zero 
    
        let mutable nearZ:float32   = 0.0f
        let mutable farZ:float32    = 0.0f
        let mutable aspect:float32  = 0.0f
        let mutable fovY:float32    = 0.0f

        let mutable nearWindowHeight:float32 = 0.0f
        let mutable farWindowHeight:float32 = 0.0f
    
        let mutable view = Matrix.Identity 
        let mutable proj = Matrix.Identity 
        let mutable world = Matrix.Identity 

        let mutable phi:float32      = 0.0f
        let mutable theta:float32    = 0.0f
        let mutable radius:float32   = 0.0f

        let mutable amountHorizontal = DEFAULT_ROT_HORIZONTAL   
        let mutable amountVertical   = DEFAULT_ROT_VERTICAL
    
        do 
            fovY  <- MathUtil.PiOverFour  
            aspect <- 1.0f 
            nearZ  <- 1.0f 
            farZ   <- 1000.0f 
            nearWindowHeight <- 2.0f * 1.0f * MathHelper.Tanf(0.5f * fovY) 
            farWindowHeight <-  2.0f * 1000.0f * MathHelper.Tanf(0.5f * fovY)     
            proj <-  Matrix.PerspectiveFovLH(fovY, aspect, 1.0f, 1000.0f) 

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
            let msg = 
                "Camera"  
                + "\n" + "POS/TARGT/ASP " + position.ToString() + " " + target.ToString() + " " + aspect.ToString()
                + "\n" + "PHI/THETA/RAD " + phi.ToString() + " " + theta.ToString() + " " + radius.ToString()

            logger.Debug(msg)
    
        member this.NearZ
            with get() = nearZ
            and set(value) = nearZ <- value

        member this.FarZ
            with get() = farZ
            and set(value) = farZ <- value 

        member this.Aspect
            with get() = aspect
            and set(value) = aspect <- value

        member this.FovY
            with get() = fovY
            and set(value) = fovY <- value

        member this.FovX
            with get() = 
                let halfWidth = 0.5f * this.NearWindowWidth
                2.0f * MathHelper.Atanf(halfWidth / this.NearZ)

        member this.NearWindowHeight
            with get() = nearWindowHeight
            and set(value) = nearWindowHeight <- value 

        member this.NearWindowWidth =
            this.Aspect * this.NearWindowHeight 

        member this.FarWindowHeight
            with get() = farWindowHeight
            and set(value) = farWindowHeight <- value 

        member this.FarWindowWidth =
            this.Aspect * this.FarWindowHeight
        
        member this.Phi 
            with get() = phi
            and set(value) = phi <- value 

        member this.Theta 
            with get() = theta
            and set(value) = theta <- value 

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
            this.View * this.Proj

        member this.Frustum =
            new BoundingFrustum(this.ViewProj)

        member this.Radius 
            with get() = radius                
            and set(value) = radius <- value 

        member this.RotHorizontal
            with get() = amountHorizontal
            and set(value) = amountHorizontal <- value 

        member this.RotVertical
            with get() = amountVertical
            and set(value) = amountVertical <- value 
    
        member this.SetLens(fovY:float32, aspect:float32, zn:float32, zf:float32) =
            this.FovY   <- fovY  
            this.Aspect <- aspect 
            this.NearZ  <- zn 
            this.FarZ   <- zf 
    
            this.NearWindowHeight <- 2.0f * zn * MathHelper.Tanf(0.5f * fovY) 
            this.FarWindowHeight <-  2.0f * zf * MathHelper.Tanf(0.5f * fovY) 
    
            this.Proj <-  Matrix.PerspectiveFovLH(fovY, aspect, zn, zf)  
    
        member this.LookAt(pos:Vector3, target:Vector3, up:Vector3) =
            this.Position <- pos 
            this.Target <- target 
            this.Look <- Vector3.Normalize(target - pos) 
            this.Right <- Vector3.Normalize(Vector3.Cross(up, this.Look)) 
            this.Up <- Vector3.Cross(this.Look, this.Right) 
            this.Radius <- Vector3.Distance (this.Position, this.Target)
            viewDirty <- true 
    
        member this.Strafe(d:float32) =
            this.Position <- this.Position + this.Right * d
            this.View <- Matrix.LookAtLH(this.Position, this.Target, Vector3.Up)
    
        member this.Walk(d:float32) =
            this.Position <- this.Position + this.Look * d 
            this.View <- Matrix.LookAtLH(this.Position, this.Target, Vector3.Up)
    
        member this.Pitch(angle:float32) =
            // Rotate up and look vector about the right vector.
    
            let r = Matrix.RotationAxis(this.Right, angle) 
    
            this.Up <- Vector3.TransformNormal(this.Up, r) 
            this.Look <- Vector3.TransformNormal(this.Look, r) 
    
            viewDirty <- true 
            
        member this.Zoom (delta:float32) = 
            this.Radius <- this.Radius + delta
            this.FromRadians(phi, theta)
            this.View <- Matrix.LookAtLH(this.Position, this.Target, Vector3.Up)

        // ----------------------------------------------------------------------------------------------------
        // Rotate left / right
        // ----------------------------------------------------------------------------------------------------
        member this.RotateHorizontal(right:bool) =
            if right then 
                theta <- theta + amountHorizontal
            else
                theta <- theta - amountHorizontal
            theta <- adjustIIPi(theta)
            this.FromRadians(phi, theta)
            this.View <- Matrix.LookAtLH(this.Position, this.Target, Vector3.Up)

        member this.RotateVertical(up:bool) =
            if up then 
                phi <- phi - amountVertical
            else
                phi <- phi + amountVertical
            phi <- MathUtil.Clamp(phi, 0.1f, MathUtil.Pi - 0.1f)
            this.FromRadians(phi, theta)
            this.View <- Matrix.LookAtLH(this.Position, this.Target, Vector3.Up)

        member this.RotateHorizontalAmount(right:bool, strength) =
            if right then 
                theta <- theta + amountHorizontal * strength
            else
                theta <- theta - amountHorizontal * strength
            theta <- adjustIIPi(theta)
            this.FromRadians(phi, theta)
            this.View <- Matrix.LookAtLH(this.Position, this.Target, Vector3.Up)

        member this.RotateVerticalAmount(up:bool, strength) =
            if up then 
                phi <- phi - amountVertical * strength
            else
                phi <- phi + amountVertical * strength
            phi <- MathUtil.Clamp(phi, 0.1f, MathUtil.Pi - 0.1f)
            this.FromRadians(phi, theta)
            this.View <- Matrix.LookAtLH(this.Position, this.Target, Vector3.Up)

        // ----------------------------------------------------------------------------------------------------
        // Convert Spherical to Cartesian coordinates.
        // ----------------------------------------------------------------------------------------------------
        member this.FromRadians(phi, theta) =
            this.Theta  <- theta
            this.Phi    <- phi
            let x = this.Radius * MathHelper.Sinf(phi) * MathHelper.Cosf(theta) 
            let z = this.Radius * MathHelper.Sinf(phi) * MathHelper.Sinf(theta) 
            let y = this.Radius * MathHelper.Cosf(phi)
            this.Position <- Vector3(x,y,z)

        member this.ToRadians() = 
                this.Theta  <-  - MathHelper.Atan2f(this.Position.Z, (int64) this.Position.X) + MathUtil.Pi
                this.Phi    <-    MathHelper.Acosf(this.Position.Y / this.Radius)

        member this.UpdateViewMatrix() =

            if viewDirty then 
    
                // Keep camera's axes orthogonal to each other and of unit length.
                this.Look <- Vector3.Normalize(this.Look) 
                this.Up <- Vector3.Normalize(Vector3.Cross(this.Look, this.Right)) 
    
                // U, L already ortho-normal, so no need to normalize cross product.
                this.Right <- Vector3.Cross(this.Up, this.Look) 
    
                // Fill in the view matrix entries.
                let x = -Vector3.Dot(this.Position, this.Right) 
                let y = -Vector3.Dot(this.Position, this.Up) 
                let z = -Vector3.Dot(this.Position, this.Look) 
    
                this.View <- new Matrix(
                    this.Right.X, this.Up.X, this.Look.X, 0.0f,
                    this.Right.Y, this.Up.Y, this.Look.Y, 0.0f,
                    this.Right.Z, this.Up.Z, this.Look.Z, 0.0f,
                    x, y, z, 1.0f
                )    
                viewDirty <- false 
   
        member this.GetPickingRay(sp:Point, clientWidth:int, clientHeight:int) =
            
            let p = this.Proj 
    
            // Convert screen pixel to view space.
            let vx = (2.0f * (float32)sp.X / (float32)clientWidth - 1.0f) / p.M11 
            let vy = (-2.0f * (float32)sp.Y / (float32)clientHeight + 1.0f) / p.M22 
    
            let mutable ray = new Ray(Vector3.Zero, new Vector3(vx, vy, 1.0f)) 
            let v = this.View
            let invView = Matrix.Invert(v)    
            let toWorld = invView 
    
            ray <- new Ray(
                Vector3.TransformCoordinate(ray.Position, toWorld),
                Vector3.TransformNormal(ray.Direction, toWorld)) 
    
            ray

        static member init(cameraPosition, cameraTarget, aspectRatio, rotHorizontal, rotVertical) = 
            Camera.Instance.LookAt(cameraPosition, cameraTarget, Vector3.Up)
            Camera.Instance.SetLens(MathUtil.PiOverFour, aspectRatio, 1.0f, 1000.0f)
            Camera.Instance.RotHorizontal <- rotHorizontal
            Camera.Instance.RotVertical <- rotVertical
            Camera.Instance.UpdateViewMatrix()
            Camera.Instance.ToRadians()

        static member reset(cameraPosition, cameraTarget, aspectRatio) = 
            Camera.Instance.LookAt(cameraPosition, cameraTarget, Vector3.Up)
            Camera.Instance.SetLens(MathUtil.PiOverFour, aspectRatio, 1.0f, 1000.0f)
            Camera.Instance.View <- Matrix.LookAtLH(Camera.Instance.Position, Camera.Instance.Target, Vector3.Up)
            Camera.Instance.ToRadians()