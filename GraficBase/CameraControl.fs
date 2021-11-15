namespace GraficBase
//
//  CameraControl.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 
open Shader.FrameResources
 
open SharpDX

open DirectX.Camera
 
// ----------------------------------------------------------------------------------------------------
//  Singleton zur Verwendung der Kamera
//  Behält die Startwerte
// ----------------------------------------------------------------------------------------------------  

type DirectionalLight = CookBook.DirectionalLight

module CameraControl = 

    let DEFAULT_ROT_HORIZONTAL = MathUtil.TwoPi / 200.0f           // Scrollamount horizontal
    let DEFAULT_ROT_VERTICAL   = MathUtil.TwoPi / 200.0f           // Scrollamount vertical
    let DEFAULT_CAMERA_POS     = new Vector3(0.0f, 5.0f, -20.0f)   // Default Viewpoint
    let DEFAULT_TARGET_POS     = Vector3.Zero                      // Default Target
    let DEFAULT_STRENGTH       = 1.0f                              // Default rotation strength 
    let DEFAULT_ASPECT_RATIO   = 1.0f                              // Default aspect ratio 

    [<AllowNullLiteral>]
    type CameraController(cameraPosition, cameraTarget, aspectRatio) =
        static let mutable instance = null 
        let mutable startCameraPosition = cameraPosition
        let mutable startCameraTarget = cameraTarget
        let mutable startAspectRatio = aspectRatio
        let mutable aspectRatio = aspectRatio

        new() = new CameraController(Vector3.Zero, Vector3.Zero, 1.0f)

        static member Instance
            with get() = 
                if instance = null then 
                    instance <- new CameraController()
                instance

        // ----------------------------------------------------------------------------------------------------    
        //  Initialisierung der Camera-Klasse        
        //  <param name="cameraPosition">Position, von der aus die Kamera auf die Welt schaut</param> 
        //  <param name="cameraTarget">Zentrum der Welt</param> 
        //  <param name="aspectRatio">Seitenverhälnis des Fensters</param> 
        //  <param name="rotHorizontal">Einheit bei horizontaler Drehung</param> 
        //  <param name="rotVertical">Einheit bei vertikaler Drehung</param> 
        //  <param name="rotStrength">Stärke der Drehung</param> 
        // ----------------------------------------------------------------------------------------------------
        member this.ConfigureCamera(cameraPosition, cameraTarget, aspectRatio, rotHorizontal, rotVertical, rotStrength) = 
            startCameraPosition <- cameraPosition
            startCameraTarget   <- cameraTarget
            startAspectRatio    <- aspectRatio
            Camera.Instance.Init(cameraPosition, cameraTarget, aspectRatio, rotHorizontal, rotVertical, rotStrength) 

        // ----------------------------------------------------------------------------------------------------    
        //  Initialisierung der Camera-Klasse        
        //  <param name="cameraPosition">Neu Kamera-Position</param> 
        //  <param name="cameraTarget">Neues Zentrum</param> 
        // ----------------------------------------------------------------------------------------------------
        member this.RespositionCamera(cameraPosition, cameraTarget) =
            startCameraPosition <- cameraPosition
            Camera.Instance.Reset(cameraPosition, cameraTarget)
            startCameraTarget <- cameraTarget

        // ----------------------------------------------------------------------------------------------------    
        //  Camera-Klasse auf vorher gesetzte Initialwerte zurück
        // ----------------------------------------------------------------------------------------------------
        member this.ResetCamera() = 
            Camera.Instance.Reset(startCameraPosition, startCameraTarget)

        member this.InitialSettings =
                "Camera\n" 
                + " pos "   + startCameraPosition.ToString()+ "\n" 
                + " targ "  + startCameraTarget.ToString()  + "\n" 
                + " ratio"  + startAspectRatio.ToString()   + "\n" 