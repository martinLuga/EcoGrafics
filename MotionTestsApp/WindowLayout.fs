namespace MotionTests
//
//  WindowLayout.fs
//
//  Created by Martin Luga on 01.02.19.
//  Copyright © 2019 Martin Luga. All rights reserved.
//

open System.Windows.Forms
open System.Drawing 

open ApplicationBase.WindowLayout

// ----------------------------------------------------------------------------------------------------
//
// Layout für die Simulation Applikation
//
// MainWindow
//
//  Links -  Kontrollwindow zum Steuern der Simulation
//  Mitte -  Das Grafikwindow zum Anzeigen der Simulation
//  Rechts - Outputwindow  
//
// ----------------------------------------------------------------------------------------------------
module WindowLayout =

    let antDataSelectionWindow = new Panel()
    setControlBounds(antDataSelectionWindow, boundsLeft()) 

    // Object speed
    let objectSpeedLabel = new Label()  
    objectSpeedLabel.Location <- new Point(10, 73) 
    objectSpeedLabel.Size <- new Size(70, 20)
    objectSpeedLabel.Text <- "Velocity:"
    objectSpeedLabel.BorderStyle <- BorderStyle.None
    antDataSelectionWindow.Controls.Add(objectSpeedLabel)

    let objectSpeedTrackBar = new TrackBar()  
    objectSpeedTrackBar.Location <- new Point(80, 70) 
    objectSpeedTrackBar.Size <- new Size(190, 50)
    antDataSelectionWindow.Controls.Add(objectSpeedTrackBar)

    // ----------------------------------------------------------------------------------------------------   
    // Start Display
    // ----------------------------------------------------------------------------------------------------
    let Setup(windowText) =
        SetSubwindowWidth(300, 1400, 0)
        mainTitle   <-  windowText
        antDataSelectionWindow.Parent <- mainWindow  
        ApplicationBase.WindowLayout.Setup(windowText)