namespace FrameworkTests
//
//  WindowLayout.fs
//
//  Created by Martin Luga on 01.02.19.
//  Copyright © 2019 Martin Luga. All rights reserved.
//

open System.Windows.Forms
open System.Drawing 

open ApplicationBase.WindowControl
open ApplicationBase.WindowLayout

// ----------------------------------------------------------------------------------------------------
// Das Windows GUI für diese App
// ----------------------------------------------------------------------------------------------------
module WindowLayout =

    // ----------------------------------------------------------------------------------------------------    
    // DataSelection Window
    // ---------------------------------------------------------------------------------------------------- 
    let dataSelectionWindow = new Panel()
    dataSelectionWindow.BorderStyle <- BorderStyle.Fixed3D
    setControlBounds(dataSelectionWindow, boundsLeft()) 
    let label = new Label() 
    label.Text <- "Daten Auswahl:" 
    label.BorderStyle <- BorderStyle.Fixed3D
    dataSelectionWindow.Controls.Add(label)        
    let textBox = new  TextBox()
    textBox.Dock <- System.Windows.Forms.DockStyle.Fill
    textBox.Text <- "Eine Auswahl treffen"
    dataSelectionWindow.Controls.Add(textBox)

    // ----------------------------------------------------------------------------------------------------    
    // Output Window
    // ---------------------------------------------------------------------------------------------------- 
    let outputWindow = new Panel()
    outputWindow.BorderStyle <- BorderStyle.Fixed3D 
    setControlBounds(outputWindow, boundsRight())     
    let textBoxO = new RichTextBox(Dock=DockStyle.Fill) 
    textBoxO.BackColor <- Color.White
    outputWindow.Controls.Add(textBoxO) 

    // ----------------------------------------------------------------------------------------------------    
    // Output Window
    // ---------------------------------------------------------------------------------------------------- 
    //graficWindow.Boundery <- boundsMiddle()

    // ----------------------------------------------------------------------------------------------------   
    // Start Display
    // ----------------------------------------------------------------------------------------------------
    let init(windowText) =
        dataSelectionWindow.Parent <- mainWindow  
        graficWindow.Parent <- mainWindow
        outputWindow.Parent <- mainWindow 
        mainTitle  <- windowText        
        mainWindow.MainMenuStrip <- mainMenue()
        mainWindow.Controls.Add(mainWindow.MainMenuStrip)
        displayWindows()