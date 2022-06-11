namespace ApplicationBase
//
//  WindowControl.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
// 

open GraficBase.CameraControl
open GraficBase.Camera
open Base.ObjectBase
open Base.LoggingSupport
open Base.ModelSupport
open Base.PrintSupport

open GraficBase.GraficController
open GraficBase.GraficWindow

open log4net

open SharpDX

open System 
open System.Windows.Forms

open WindowLayout 
open ScenarioSupport
 
// ----------------------------------------------------------------------------------------------------
//  Window-Actions
//  STD-Menüs
//  Steuern des Grafik-Fensters über Keys 
// ----------------------------------------------------------------------------------------------------   
module WindowControl = 

    let logger = LogManager.GetLogger("WindowControl")
    let logDebug = Debug(logger)
     
    type Keys = System.Windows.Forms.Keys

    let mutable lastMousePos = new System.Drawing.Point()
    let mutable zoomFactor = 0.5f 

    let DEFAULT_TESSELATION_AMOUNT = 1.0f
    let DEFAULT_RASTERIZATION_AMOUNT = 8 
   
    let clock = new System.Diagnostics.Stopwatch()

    // ----------------------------------------------------------------------------------------------------    
    //  Message output
    // ---------------------------------------------------------------------------------------------------- 
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

    // ----------------------------------------------------------------------------------------------------
    //  Tesselation
    // ----------------------------------------------------------------------------------------------------
    let InitTesselationFactor (newTessellationFactor) =
        MyController.Instance.TessellationFactor <- newTessellationFactor 

    let IncreaseTesselationFactor() =
        MyController.Instance.TessellationFactor <- MyController.Instance.TessellationFactor + DEFAULT_TESSELATION_AMOUNT
        writeToMessageWindow("Tesselation Factor: " + MyController.Instance.TessellationFactor.ToString())

    let DecreaseTesselationFactor() =
        MyController.Instance.TessellationFactor <- MyController.Instance.TessellationFactor - DEFAULT_TESSELATION_AMOUNT 
        writeToMessageWindow("Tesselation Factor: " + MyController.Instance.TessellationFactor.ToString())
    
    let IncreaseRasterizationFactor() =
        Shape.Raster <- Shape.Raster + DEFAULT_RASTERIZATION_AMOUNT     
        MyController.Instance.RefreshShapes()
        writeToMessageWindow("Rasterization Factor: " + Shape.Raster.ToString())

    let DecreaseRasterizationFactor() =
        Shape.Raster <- Shape.Raster - DEFAULT_RASTERIZATION_AMOUNT 
        MyController.Instance.RefreshShapes()
        writeToMessageWindow("Rasterization Factor: " + Shape.Raster.ToString())

    let ToggleRasterizerState() =
        MyController.Instance.ToggleRasterizerState()

    // ----------------------------------------------------------------------------------------------------    
    //  Drehungen zurücksetzen
    // ----------------------------------------------------------------------------------------------------
    let resetCamera() = 
        CameraController.Instance.RespositionCamera(MyController.Instance.StartCameraPosition, MyController.Instance.StartCameraTarget)  

    let TraceCamera() = 
        let text = CameraController.Instance.InitialSettings   
        clearOutputWindow()
        writeToOutputWindow(text)

    // ----------------------------------------------------------------------------------------------------    
    //  Key movements
    // ---------------------------------------------------------------------------------------------------- 
    let addStandardKeyMovements(form:MyWindow) =
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.PageDown  then Camera.Instance.Zoom true)
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.PageUp    then Camera.Instance.Zoom false)

        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.Up    then Camera.Instance.RotateVertical  (true)) 
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.Down  then Camera.Instance.RotateVertical  (false))
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.Right then Camera.Instance.RotateHorizontal(true))
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.Left  then Camera.Instance.RotateHorizontal(false)) 
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.End   then CameraController.Instance.ResetCamera())  

    let addStandardScenarioKeyMovements(form:MyWindow) =
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.N  then execNextScenario())
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.R  then execActiveScenario ()) 
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.P  then execPreviousScenario()) 
    
    // ----------------------------------------------------------------------------------------------------    
    //  Movements from mouse behaviour
    // ----------------------------------------------------------------------------------------------------
    let onMouseMove(evt:MouseEventArgs) = 
        let button= evt.Button
        let location = evt.Location
        if  button = System.Windows.Forms.MouseButtons.Left then
            let dx = MathUtil.DegreesToRadians(0.25f * ((float32)location.X - (float32)lastMousePos.X)) 
            let dy = MathUtil.DegreesToRadians(0.25f * ((float32)location.Y - (float32)lastMousePos.Y)) 

            if Math.Sign(dx) > 0 then
                Camera.Instance.RotateHorizontalStrength(true, 0.3f)
            else if Math.Sign(dx) < 0 then
                Camera.Instance.RotateHorizontalStrength(false, 0.3f)
 
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
        if deltaDistance > 0.0f then 
            Camera.Instance.Zoom true
        else         
            Camera.Instance.Zoom false
        
    // ----------------------------------------------------------------------------------------------------    
    //  Mouse Events
    // ---------------------------------------------------------------------------------------------------- 
    let addStandardMouseMovements(form:MyWindow) =
        graficWindow.MouseMove.Add(fun e  -> onMouseMove e) 
        graficWindow.MouseWheel.Add(fun e  -> onMouseWheel e)

    // ----------------------------------------------------------------------------------------------------
    // Menues
    // ----------------------------------------------------------------------------------------------------
    let fileSubmenueStandard =  
        let fileMenuItem = new ToolStripMenuItem("&File")
        let exitMenuItem = new ToolStripMenuItem("&Exit")
        fileMenuItem.DropDownItems.Add(exitMenuItem)|>ignore
        exitMenuItem.Click.Add(fun _ -> Application.Exit())
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
        zoomInMenuItem.Click.Add(fun _  -> Camera.Instance.Zoom true)
        zoomOutMenuItem.Click.Add(fun _ -> Camera.Instance.Zoom false)
        zoomMenuItem

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
        settMenuItem

    let mainMenueStandard () = 
        let mainMenu = new  MenuStrip() 
        mainMenu.Items.Add(fileSubmenueStandard)|>ignore 
        mainMenu.Items.Add(viewSubmenueStandard)|>ignore
        mainMenu.Items.Add(settingMenueStandard)|>ignore
        mainMenu

    // ----------------------------------------------------------------------------------------------------    
    // Display
    // ---------------------------------------------------------------------------------------------------- 
    let displayWindowsPosition(position) =
        OpenWindowAtPosition(position)  
        mainWindow |> ignore
        logDebug("GraficWindow bounds: " + graficWindow.Bounds.ToString())
        logDebug("MainWindow bounds: " + mainWindow.Bounds.ToString() + " " + mainWindow.Location.ToString())
        mainWindow.Show()

    // ---------------------------------------------------------------------------------------------------- 
    //  Text-Nachrichten Ausgabe
    // ---------------------------------------------------------------------------------------------------- 
    let writeReport(objekt:BaseObject) =
        newLineOutputWindow()
        writeToOutputWindow("Objekt....: "   + objekt.Name) 
        writeToOutputWindow("Position..: "   + formatVector3(objekt.Position))
        writeToOutputWindow("Scale.....: "   + formatVector3(objekt.Scale))
        writeToOutputWindow("Center....: "   + formatVector3(objekt.Center))
        writeToOutputWindow("Bounds.min: "   + formatVector3(objekt.BoundingBox.Minimum))
        writeToOutputWindow("Bounds.max: "   + formatVector3(objekt.BoundingBox.Maximum))
        for part in objekt.Display.Parts do
            writeToOutputWindow("  Shape......: "   + part.Shape.ToString())
            writeToOutputWindow("     Size....: "   + part.Shape.Size.ToString())
            writeToOutputWindow("     Min.....: "   + formatVector3(part.Shape.Minimum))
            writeToOutputWindow("     Max.....: "   + formatVector3(part.Shape.Maximum))
            writeToOutputWindow("     Diameter: "   + (Vector3.Distance(part.Shape.Maximum, part.Shape.Minimum)).ToString())
            writeToOutputWindow("  Material.: "   + part.Material.ToString())
            writeToOutputWindow("  Texture..: "   + part.Texture.ToString())
            writeToOutputWindow("  Visibilty: "   + part.Visibility.ToString())
        newLineOutputWindow()

    let writeReportObjects(displayables:BaseObject list) =        
        clearOutputWindow()
        for disp in displayables do
            writeReport(disp)

    let printScenario(scenarioName) =
        logInfo("Start Scenario: " + scenarioName) 
        mainWindow.Text <- "Scenario: " + scenarioName

    let print(applicationText) =
        logInfo(applicationText) 
        mainWindow.Text <- applicationText