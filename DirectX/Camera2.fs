namespace DirectX
//
//  Camera.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open SharpDX

open D3DUtilities

open GraficUtils

// ----------------------------------------------------------------------------------------------------
// Camera 
// ----------------------------------------------------------------------------------------------------
module Camera2 =

    let logger = LogManager.GetLogger("Camera")

    // ----------------------------------------------------------------------------------------------------
    // Camera position : Achtung , linkshändiges Koor.-System, positve z nach hinten
    // ----------------------------------------------------------------------------------------------------
    type Camera() =

        let mutable initCameraPosition = new Vector3(0.0f, 0.0f, -20.0f)
        let mutable position = initCameraPosition
    
        let mutable horizDist = 0.0f
        let mutable vertkDist = 0.0f
        let mutable target = Vector3.Zero                      
        let mutable cameraUp = Vector3.UnitY 
        let mutable nearZ:float32   = 0.0f
        let mutable farZ:float32    = 0.0f
        let mutable aspect:float32  = 0.0f
        let mutable fovY:float32    = 0.0f        

        let mutable view = Matrix.LookAtLH(position, target, cameraUp)
        let mutable proj = Matrix.Identity 
        let mutable world = Matrix.Identity 

        let mutable initViewMatrix = Matrix.LookAtLH(position, target, cameraUp)
        let mutable lookAt= Quaternion.LookAtLH(position, target, cameraUp)

        let mutable rotHorizontal = MathUtil.TwoPi / 200.0f   
        let mutable rotVertical   = MathUtil.TwoPi / 200.0f

        let mutable amountHorizontal = MathUtil.TwoPi / 200.0f   
        let mutable amountVertical   = MathUtil.TwoPi / 200.0f

        do 
            fovY  <- MathUtil.PiOverFour  
            aspect <- 1.0f 
            nearZ  <- 1.0f 
            farZ   <- 1000.0f     
            proj <-  Matrix.PerspectiveFovLH(fovY, aspect, 1.0f, 1000.0f) 

        static let mutable instance = new Camera()

        member this.Position
            with get() = position
            and set(value) = position <- value

        member this.InitCameraPosition
            with get() = initCameraPosition
            and set(value) = initCameraPosition <- value

        member this.Target
            with get() = target
            and set(value) = target <- value

        member this.cameraNormal() = 
            let normal = position
            normal.Normalize() 
            normal

        member this.View
            with get() = view
            and set(value) = view <- value 

        member this.Proj
            with get() = proj
            and set(value) = proj <- value 

        member this.World
            with get() = world
            and set(value) = world <- value 
    
        member this.ViewProj =
            this.View * this.Proj

        member this.Zoom (distance:float32) = 
            let len = position.Length() + distance
            position <- this.cameraNormal() * len
            logger.Info("New Camera pos  : " + position.ToString())
            view <- Matrix.LookAtLH(position, target, cameraUp)

        member this.setCamera(initCameraPosition) = 
            position <- initCameraPosition
            initViewMatrix <- Matrix.LookAtLH(position, target, cameraUp)
            view <- Matrix.LookAtLH(position, target, cameraUp)
            logger.Info("Initial Camera pos: " + position.ToString()) 

        member this.resetCamera() = 
            rotHorizontal <- 0.0f
            rotVertical <- 0.0f
            this.setCamera(initCameraPosition) 

        member this.initCamera(cameraPosition) = 
            initCameraPosition <- cameraPosition
            target <- Vector3.Zero  
            this.setCamera(initCameraPosition) 
            this.resetCamera()

        member this.initCameraOnTarget(cameraPosition:Vector3, pCameraTarget:Vector3) =
            initCameraPosition <- cameraPosition
            target <- pCameraTarget
            this.setCamera(initCameraPosition) 
            this.resetCamera() 

        member this.rotate(initViewMatrix:Matrix, rotHorizontal, rotVertical) =
             let rh = rotationMatrixHor(rotHorizontal) 
             let rv = rotationMatrixVert(rotVertical) 
             let mat =  Matrix.Multiply(rh, initViewMatrix)
             Matrix.Multiply(rv, mat)

        member this.RotateHorizontal (right:bool) = 
            if right then 
                rotHorizontal <- rotHorizontal + amountHorizontal
            else
                rotHorizontal <- rotHorizontal - amountHorizontal

            rotHorizontal <- adjustIIPi(rotHorizontal) 
            view <- rotate(initViewMatrix, rotHorizontal, rotVertical)

        member this.RotateVertical (up:bool) =
            if up then 
                rotVertical <- rotVertical - amountVertical
            else
                rotVertical <- rotVertical + amountVertical

            rotVertical <- limitPIHalbe(rotVertical)
            view <- rotate(initViewMatrix, rotHorizontal, rotVertical)

        member this.Log() =
            let msg = 
                "Camera"  
                + "\n" + "POS/TARGT/ASP " + position.ToString() + " " + target.ToString() + " " + aspect.ToString()
                + "\n" + "PHI/THETA/RAD " + rotVertical.ToString() + " " + rotHorizontal.ToString() + " " + position.Length().ToString()

            logger.Debug(msg)

        static member Instance
            with get() = instance
            and set(value) = instance <- value

        static member reset(cameraPosition, cameraTarget, aspectRatio) = 
            Camera.Instance.resetCamera()

        static member init(cameraPosition, cameraTarget, aspectRatio, rotHorizontal, rotVertical) = 
            Camera.Instance.InitCameraPosition <- cameraPosition
            Camera.Instance.Target <- cameraTarget  
            Camera.Instance.setCamera(cameraPosition) 
            Camera.Instance.resetCamera()