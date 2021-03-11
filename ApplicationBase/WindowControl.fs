namespace ApplicationBase
//
//  WindowControl.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 

open System 
open System.Windows.Forms
open System.Diagnostics

open log4net
 
open SharpDX


open DirectX.Camera

open GPUModel.MyGraphicWindow

open Shader.FrameResources
open Shader.ShaderSupport

open WindowLayout 
open GraficSystem
open ScenarioSupport
 
/// <summary>
//  Window-Actions
//  STD-Menüs
//  Steuern des Grafik-Fensters über Keys 
/// </summary>   
module WindowControl = 

    type Keys = System.Windows.Forms.Keys

    type DirectionalLight = CookBook.DirectionalLight

    let logger = LogManager.GetLogger("WindowControl")

    let mutable lastMousePos = new System.Drawing.Point()

    let mutable zoomFactor = 0.5f 

    let DEFAULT_TESSELATION_AMOUNT = 1.0f
    let DEFAULT_RASTERIZATION_AMOUNT = 1.0f

    let mutable x, y, clientWidth, clientHeight = boundsMiddle () 

    let mutable aspectRatio = (clientWidth |> float32) / (clientHeight |> float32)

    let mutable startCameraPosition = Vector3.Zero
    let mutable startCameraTarget = Vector3.Zero
    let mutable startAspectRatio = 1.0f

    let mutable eventCount = 0

    let mutable worldMatrix = Matrix.Identity  
    let mutable lightDir = Vector4.Zero
    let mutable frameLight=DirectionalLight(Color.White.ToColor4(), Vector3.Zero)

    let clock = new Stopwatch()
    
    /// <summary>    
    /// Message output
    /// </summary> 
    let writeToOutputWindow(text)=
        textBoxO.Text <- 
            textBoxO.Text + "\n" + text

    let newLineOutputWindow() =
        textBoxO.Text <- 
            textBoxO.Text + "\n"

    let clearOutputWindow()=
        textBoxO.Text <- ""

    let writeToMessageWindow(text) =
        textBoxM.Text <-  text

    let clearMessageWindow()=
        textBoxM.Text <- ""

    /// <summary>
    /// Tesselation
    /// </summary>
    let InitTesselationFactor (newTessellationFactor) =
        MySystem.Instance.TessellationFactor <- newTessellationFactor

    let IncreaseTesselationFactor() =
        MySystem.Instance.TessellationFactor <- MySystem.Instance.TessellationFactor + DEFAULT_TESSELATION_AMOUNT
        writeToMessageWindow("Tesselation Factor: " + MySystem.Instance.TessellationFactor.ToString())

    let DecreaseTesselationFactor() =
        MySystem.Instance.TessellationFactor <- MySystem.Instance.TessellationFactor - DEFAULT_TESSELATION_AMOUNT 
        writeToMessageWindow("Tesselation Factor: " + MySystem.Instance.TessellationFactor.ToString())
    
    let IncreaseRasterizationFactor() =
        MySystem.Instance.RasterizationFactor <- MySystem.Instance.RasterizationFactor + DEFAULT_RASTERIZATION_AMOUNT
        writeToMessageWindow("Rasterization Factor: " + MySystem.Instance.RasterizationFactor.ToString())

    let DecreaseRasterizationFactor() =
        MySystem.Instance.RasterizationFactor <- MySystem.Instance.RasterizationFactor - DEFAULT_RASTERIZATION_AMOUNT 
        writeToMessageWindow("Rasterization Factor: " + MySystem.Instance.RasterizationFactor.ToString())

    let setPixelShader (pstype: ShaderClass) = 
        writeToOutputWindow("PixelShader is now: " + pstype.ToString())
        MySystem.Instance.SetPixelShader(pstype)

    let ToggleRasterizerState() =
        MySystem.Instance.ToggleRasterizerState()

    let SetRasterizerState(irasterizerState:RasterType) =
        MySystem.Instance.SetRasterizerState(irasterizerState)

    let SetBlendState(blendType:BlendType) =
        MySystem.Instance.SetBlendType(blendType)

    /// <summary>
    /// Light
    /// </summary>
    let initLight(dir:Vector3, color: Color) = 
        lightDir <- Vector3.Transform(dir, worldMatrix)
        frameLight <- new DirectionalLight(color.ToColor4(), new Vector3(lightDir.X, lightDir.Y, lightDir.Z))
        MySystem.Instance.FrameLight <- frameLight
        MySystem.Instance.LightDir <- lightDir

    /// <summary>    
    /// Initialisierung der Camera-Klasse
    /// </summary>
    let initCamera(cameraPosition, cameraTarget, aspectRatio, rotHorizontal, rotVertical) = 
        startCameraPosition <- cameraPosition
        startCameraTarget   <- cameraTarget
        startAspectRatio    <- aspectRatio
        Camera.init(cameraPosition, cameraTarget, aspectRatio, rotHorizontal, rotVertical)

    let repositionCamera(cameraPosition, cameraTarget) = 
        startCameraPosition <- cameraPosition
        startCameraTarget   <- cameraTarget
        Camera.reset(cameraPosition, cameraTarget, aspectRatio)
        
    /// <summary>    
    /// Drehungen zurücksetzen
    /// </summary>
    let resetCamera() = 
        Camera.reset(startCameraPosition, startCameraTarget, startAspectRatio)   

    /// <summary>    
    /// Zoom durch Verändern des Abstands . Zoomfactor positiv/negativ Vergrößern/verkleinern
    /// </summary>
    let zoom(deltaDistance) =
        Camera.Instance.Zoom deltaDistance

    /// <summary>    
    /// Rotation um das Target
    /// </summary>
    let rotateHorizontal(right) = 
        Camera.Instance.RotateHorizontal(right)

    let rotateVertical(up) = 
        Camera.Instance.RotateVertical(up)

    /// <summary>    
    /// Key movements
    /// </summary> 
    let addStandardKeyMovements(form:MyWindow) =
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.Subtract  then zoom   zoomFactor)
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.Add       then zoom  -zoomFactor)

        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.Up    then rotateVertical  (true)) 
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.Down  then rotateVertical  (false))
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.Right then rotateHorizontal(true))
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.Left  then rotateHorizontal(false)) 
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.End   then resetCamera())  

    let addScenarioKeyMovements(form:MyWindow) =
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.N  then execNextScenario())  
    
    /// <summary>    
    /// Movements from mouse behaviour
    /// </summary>
    let onMouseMove(evt:MouseEventArgs) = 
        let button= evt.Button
        let location = evt.Location
        if  button = System.Windows.Forms.MouseButtons.Left then
            let dx = MathUtil.DegreesToRadians(0.25f * ((float32)location.X - (float32)lastMousePos.X)) 
            let dy = MathUtil.DegreesToRadians(0.25f * ((float32)location.Y - (float32)lastMousePos.Y)) 

            if Math.Sign(dx) > 0 then
                Camera.Instance.RotateHorizontalAmount(true, 0.3f)
            else if Math.Sign(dx) < 0 then
                Camera.Instance.RotateHorizontalAmount(false, 0.3f)
 
            if Math.Sign(dy) > 0 then
                Camera.Instance.RotateVerticalAmount(true, 0.3f)
            else if Math.Sign(dy) < 0 then
                Camera.Instance.RotateVerticalAmount(false, 0.3f)
            lastMousePos <- location 

        else if button = System.Windows.Forms.MouseButtons.Right then            
            let dx = MathUtil.DegreesToRadians(0.25f * ((float32)location.X - (float32)lastMousePos.X)) 
            let dy = MathUtil.DegreesToRadians(0.25f * ((float32)location.Y - (float32)lastMousePos.Y)) 
            if Math.Sign(dx) > 0 || Math.Sign(dx) < 0  then
                Camera.Instance.Strafe(dx)

    let onMouseWheel(evt:MouseEventArgs) = 
        let deltaDistance = (float32)evt.Delta  * 0.005f 
        Camera.Instance.Zoom deltaDistance
        
    /// <summary>    
    /// Mouse Events
    /// </summary> 
    let addStandardMouseMovements(form:MyWindow) =
        graficWindow.MouseMove.Add(fun e  -> onMouseMove e) 
        graficWindow.MouseWheel.Add(fun e  -> onMouseWheel e)

    /// <summary>
    ///Menues
    /// </summary>
    let fileSubmenueStandard =  
        let fileMenuItem = new ToolStripMenuItem("&File")
        let exitMenuItem = new ToolStripMenuItem("&Exit")
        fileMenuItem.DropDownItems.Add(exitMenuItem)|>ignore
        exitMenuItem.Click.Add(fun _ -> MySystem.Instance.Stop(); graficWindow.Close(); mainWindow.Close())
        fileMenuItem

    let tesselationSubmenue =  
        let tesselationMenuItem = new ToolStripMenuItem("&Tesselation")
        let addIncreaseMenuItem = new ToolStripMenuItem("&Increase")
        let addDecreaseMenuItem = new ToolStripMenuItem("&Decrease")
        tesselationMenuItem.DropDownItems.Add(addIncreaseMenuItem)|>ignore
        tesselationMenuItem.DropDownItems.Add(addDecreaseMenuItem)|>ignore
        addIncreaseMenuItem.Click.Add(fun _ -> IncreaseTesselationFactor())
        addDecreaseMenuItem.Click.Add(fun _ -> DecreaseTesselationFactor())
        tesselationMenuItem

    let rasterizationSubmenue =  
        let rasterizationMenuItem = new ToolStripMenuItem("&Rasterization")
        let addIncreaseMenuItem = new ToolStripMenuItem("&Increase")
        let addDecreaseMenuItem = new ToolStripMenuItem("&Decrease")
        rasterizationMenuItem.DropDownItems.Add(addIncreaseMenuItem)|>ignore
        rasterizationMenuItem.DropDownItems.Add(addDecreaseMenuItem)|>ignore
        addIncreaseMenuItem.Click.Add(fun _ -> IncreaseRasterizationFactor())
        addDecreaseMenuItem.Click.Add(fun _ -> DecreaseRasterizationFactor())
        rasterizationMenuItem

    let zoomSubmenueStandard =     
        let zoomFactor = 0.5f
        let zoomMenuItem = new ToolStripMenuItem("&Zoom")
        let zoomInMenuItem = new ToolStripMenuItem("&Zoom In")
        let zoomOutMenuItem = new ToolStripMenuItem("&Zoom Out")
        zoomMenuItem.DropDownItems.Add(zoomInMenuItem)|>ignore
        zoomMenuItem.DropDownItems.Add(zoomOutMenuItem)|>ignore
        zoomInMenuItem.Click.Add(fun _ -> zoom -zoomFactor)
        zoomOutMenuItem.Click.Add(fun _ -> zoom zoomFactor)
        zoomMenuItem

    let shaderSubmenue = 
        let shaderMenuItem      = new ToolStripMenuItem("&Shaders")
        let simpleMenuItem      = new ToolStripMenuItem("&Simple")
        let lambertMenuItem     = new ToolStripMenuItem("&Lambert")
        let phongMenuItem       = new ToolStripMenuItem("&Phong")
        let blinnPhongMenuItem  = new ToolStripMenuItem("&BlinnPhong")
        shaderMenuItem.DropDownItems.Add(simpleMenuItem)|>ignore
        shaderMenuItem.DropDownItems.Add(lambertMenuItem)|>ignore
        shaderMenuItem.DropDownItems.Add(phongMenuItem)|>ignore
        shaderMenuItem.DropDownItems.Add(blinnPhongMenuItem)|>ignore
        simpleMenuItem.Click.Add(fun _ -> setPixelShader  ShaderClass.SimplePSType)
        lambertMenuItem.Click.Add(fun _ -> setPixelShader  ShaderClass.LambertPSType)
        phongMenuItem.Click.Add(fun _ -> setPixelShader  ShaderClass.PhongPSType)
        blinnPhongMenuItem.Click.Add(fun _ -> setPixelShader  ShaderClass.BlinnPhongPSType)
        shaderMenuItem
        
    let viewSubmenueStandard =
        let viewMenuItem = new ToolStripMenuItem("&View")
        let toggleStateMenuItem = new ToolStripMenuItem("&Toggle Rasterization")
        viewMenuItem.DropDownItems.Add(zoomSubmenueStandard)|>ignore
        viewMenuItem.DropDownItems.Add(tesselationSubmenue)|>ignore
        viewMenuItem.DropDownItems.Add(rasterizationSubmenue)|>ignore
        viewMenuItem.DropDownItems.Add(toggleStateMenuItem)|>ignore
        toggleStateMenuItem.Click.Add(fun _ -> ToggleRasterizerState())
        viewMenuItem

    let settingMenueStandard =
        let settMenuItem = new ToolStripMenuItem("&Settings")
        settMenuItem.DropDownItems.Add(shaderSubmenue)|>ignore
        settMenuItem

    let mainMenue () = 
        let mainMenu = new  MenuStrip() 
        mainMenu.Items.Add(fileSubmenueStandard)|>ignore 
        mainMenu.Items.Add(viewSubmenueStandard)|>ignore
        mainMenu.Items.Add(settingMenueStandard)|>ignore
        mainMenu

    /// <summary>    
    ///Display
    /// </summary> 
    let displayWindows() =
        mainWindow.Show()