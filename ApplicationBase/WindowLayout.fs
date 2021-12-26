namespace ApplicationBase
//
//  WindowLayout.fs
//
//  Created by Martin Luga on 01.02.19.
//  Copyright © 2019 Martin Luga. All rights reserved.
//

open Base.Framework
open Base.LoggingSupport
open GraficBase.GraficWindow
open log4net
open System.Drawing 
open System.Windows.Forms

// ----------------------------------------------------------------------------------------------------
// Standard Windows GUI
// ----------------------------------------------------------------------------------------------------
module WindowLayout =
    
    let mutable mainWindow = new Form()
    let mutable graficWindow:MyWindow = new MyWindow ()

    let mutable menueHeight  =   25
    let mutable leftWidth    =  200
    let mutable appHeight    = 1000
    let mutable middleWidth  = 1500
    let mutable rightWidth   =  200
    let mutable bottomHeight =   30

    let mainHeight () = bottomHeight + menueHeight + appHeight + bottomHeight + 10
    let mainWidth  () = leftWidth + middleWidth + rightWidth + 15
    let bottomWidth () = leftWidth + middleWidth + rightWidth

    let boundsMain ()   = (0, 0 , mainWidth(), mainHeight())
    let boundsLeft ()   = (0, menueHeight, leftWidth, appHeight)
    let boundsMiddle () = (leftWidth,  menueHeight , middleWidth, appHeight)
    let boundsRight ()  = (leftWidth+middleWidth,  menueHeight ,  rightWidth, appHeight)
    let boundsBottom()  = (0, menueHeight + appHeight , mainWidth(),  bottomHeight)

    let setControlBounds (control:Control , bounds) =
        control.SetBounds(first bounds, secnd bounds , third bounds, fourth bounds)   
        
    mainWindow.StartPosition <- FormStartPosition.Manual

    // ----------------------------------------------------------------------------------------------------    
    // DataSelection Window
    // Kann geerbt werden. Macht aber keinen Sinn
    // ---------------------------------------------------------------------------------------------------- 
    let dataSelectionWindow = new Panel()
    setControlBounds(dataSelectionWindow, boundsLeft()) 
    dataSelectionWindow.BorderStyle <- BorderStyle.Fixed3D

    // ----------------------------------------------------------------------------------------------------    
    // Output Window
    // Kann geerbt werden
    // ---------------------------------------------------------------------------------------------------- 
    let outputWindow = new Panel()
    outputWindow.BorderStyle <- BorderStyle.Fixed3D   
    let textBoxO = new RichTextBox(Dock=DockStyle.Fill) 
    textBoxO.BackColor <- Color.White
    textBoxO.Font <- new Font(FontFamily.GenericMonospace, 10.0f, FontStyle.Regular)
    outputWindow.Controls.Add(textBoxO) 

    // ----------------------------------------------------------------------------------------------------    
    // Message Window
    // Kann geerbt werden
    // ---------------------------------------------------------------------------------------------------- 
    let messageWindow = new Panel()
    messageWindow.BorderStyle <- BorderStyle.Fixed3D    
    let textBoxM = new RichTextBox(Dock=DockStyle.Fill) 
    textBoxM.BackColor <- System.Drawing.Color.White
    textBoxM.Font <- new Font(FontFamily.GenericSerif, 12.0f, FontStyle.Bold)
    messageWindow.Controls.Add(textBoxM)

    let SetSubwindowSizes(mh, lw , ah, mw, rw, bh) =
        menueHeight  <-   mh
        leftWidth    <-   lw
        appHeight    <-   ah
        middleWidth  <-   mw  
        rightWidth   <-   rw
        bottomHeight <-   bh

    let SetSubwindowWidth(lw , mw, rw) =
        leftWidth    <-   lw
        middleWidth  <-   mw  
        rightWidth   <-   rw

    let OpenWindowAtPosition(point) =        
        mainWindow.Location <- point 

    let GetGraficAspectRatio =
        graficWindow.AspectRatio

    // ----------------------------------------------------------------------------------------------------   
    // Start Display
    // ----------------------------------------------------------------------------------------------------
    let Setup(windowText) =   
        graficWindow.Boundery <- boundsMiddle()
        graficWindow.SetBackColor Color.Black
        setControlBounds(outputWindow, boundsRight())   
        setControlBounds(mainWindow, boundsMain())
        setControlBounds(messageWindow, boundsBottom()) 
        graficWindow.Parent <- mainWindow
        outputWindow.Parent <- mainWindow 
        messageWindow.Parent <- mainWindow
        mainWindow.Text <- windowText    