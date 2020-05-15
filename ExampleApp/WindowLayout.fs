namespace ExampleApp
//
//  WindowLayout.fs
//
//  Created by Martin Luga on 01.02.19.
//  Copyright © 2019 Martin Luga. All rights reserved.
//

open System.Windows.Forms

open ApplicationBase.WindowLayout

// ----------------------------------------------------------------------------------------------------
// Layout für die Beispiel Applikation
//
// MainWindow
//
//  Links -  Beispiel-Datenselektionswindow  
//  Mitte -  Das Grafikwindow zum Anzeigen von Beispiel-Situationen
// ----------------------------------------------------------------------------------------------------
module WindowLayout =

    // ----------------------------------------------------------------------------------------------------    
    // DataSelection Window
    // ---------------------------------------------------------------------------------------------------- 
    let exampleDataSelectionWindow = new Panel()
    setControlBounds(exampleDataSelectionWindow, boundsLeft()) 
    exampleDataSelectionWindow.BorderStyle <- BorderStyle.Fixed3D
    let label = new Label() 
    label.Text <- "Daten Auswahl:" 
    label.BorderStyle <- BorderStyle.Fixed3D
    exampleDataSelectionWindow.Controls.Add(label)        
    let textBox = new  TextBox()
    textBox.Dock <- System.Windows.Forms.DockStyle.Fill
    textBox.Text <- "Eine Auswahl treffen"
    exampleDataSelectionWindow.Controls.Add(textBox)

    // ----------------------------------------------------------------------------------------------------   
    // Setup
    // ----------------------------------------------------------------------------------------------------
    let Setup(windowText) =
        SetSubwindowWidth(200, 1100, 250)
        mainTitle   <-  windowText
        exampleDataSelectionWindow.Parent <- mainWindow 
        ApplicationBase.WindowLayout.Setup(windowText)

