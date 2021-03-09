namespace ExampleApp
//
//  Control.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Windows.Forms

open log4net

open ApplicationBase.WindowControl
open ApplicationBase.WindowLayout
open ApplicationBase.GraficSystem  

open DirectX.Assets

open GPUModel.MyGraphicWindow

open MoleculeDrawing.MoleculeDraw

open Shader.ShaderSupport

// ----------------------------------------------------------------------------------------------------
// Simple Steuerung der Elemente
// ----------------------------------------------------------------------------------------------------

type ZoomDir = | Nearer  | Farther 

module Control = 

    let logger = LogManager.GetLogger("Control")

    let mutable shape = Shape.Pyramid

    let initShape (ishape:Shape) =
        shape <- ishape
        clearOutputWindow()
        writeToOutputWindow("Shape is: " + ishape.ToString()) 

    let initDisplayables () =
        let displayables = ExampleObjects.getDisplayables (shape)
        MySystem.Instance.InitObjects(displayables)

    let changeShape(ishape) =
        logger.Info("\nchangeShape to "+ ishape.ToString() + "\n")
        initShape(ishape)
        initDisplayables()
        writeToMessageWindow("Changed shape")

    // ----------------------------------------------------------------------------------------------------    
    // Menue actions
    // ---------------------------------------------------------------------------------------------------- 
    let geometryMenue = 
        let geometryMenuItem = new ToolStripMenuItem("&Geometry")
        let sphereMenuItem = new ToolStripMenuItem("&Sphere")
        let cubeMenuItem = new ToolStripMenuItem("&Cube")
        let adobeMenuItem = new ToolStripMenuItem("&Adobe")
        let pyramidMenuItem = new ToolStripMenuItem("&Pyramid")
        let cylinderMenuItem = new ToolStripMenuItem("&Cylinder")
        let skullMenuItem = new ToolStripMenuItem("&Skull")
        let carMenuItem = new ToolStripMenuItem("&Car")
        let atomBondMenuItem = new ToolStripMenuItem("&Atom Bond")
        let atomBuilderMenuItem = new ToolStripMenuItem("&Atombuilder")
        let korpusMenuItem = new ToolStripMenuItem("&Korpus")
        let groundPlaneMenuItem = new ToolStripMenuItem("&Plane")
        let icosahedronMenuItem = new ToolStripMenuItem("&Icosahedron")
        let manyObjectsMenuItem = new ToolStripMenuItem("&Many Objects")
        let twoDMenuItem = new ToolStripMenuItem("&2D")

        geometryMenuItem.DropDownItems.Add(cubeMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(sphereMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(pyramidMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(adobeMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(cylinderMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(skullMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(carMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(atomBondMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(atomBuilderMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(korpusMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(groundPlaneMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(icosahedronMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(manyObjectsMenuItem)|>ignore
        geometryMenuItem.DropDownItems.Add(twoDMenuItem)|>ignore

        sphereMenuItem.Click.Add(fun _ -> changeShape(Shape.Sphere))
        cubeMenuItem.Click.Add(fun _ -> changeShape(Shape.Cube))
        pyramidMenuItem.Click.Add(fun _ -> changeShape(Shape.Pyramid ))
        adobeMenuItem.Click.Add(fun _ -> changeShape(Shape.Adobe ))
        cylinderMenuItem.Click.Add(fun _ -> changeShape(Shape.Cylinder))
        skullMenuItem.Click.Add(fun _ -> changeShape(Shape.Skull))
        carMenuItem.Click.Add(fun _ -> changeShape(Shape.Car))
        atomBondMenuItem.Click.Add(fun _ -> changeShape(Shape.AtomBond ))
        atomBuilderMenuItem.Click.Add(fun _ -> changeShape(Shape.AtomBuilder))
        korpusMenuItem.Click.Add(fun _ -> changeShape(Shape.Korpus))
        groundPlaneMenuItem.Click.Add(fun _ -> changeShape(Shape.GroundPlane))
        icosahedronMenuItem.Click.Add(fun _ -> changeShape(Shape.Icosahedron))
        manyObjectsMenuItem.Click.Add(fun _ -> changeShape(Shape.ManyObjects))
        twoDMenuItem.Click.Add(fun _ -> changeShape(Shape.TwoD))
        geometryMenuItem

    let settingMenue =
        let settMenuItem = settingMenueStandard
        settMenuItem.DropDownItems.Add(geometryMenue)|>ignore
        settMenuItem

    let mainMenue = 
        let mainMenu = new  MenuStrip() 
        mainMenu.Items.Add(fileSubmenueStandard)    |>ignore 
        mainMenu.Items.Add(viewSubmenueStandard)    |>ignore
        mainMenu.Items.Add(settingMenue)            |>ignore
        mainMenu

    let addProteinKeyMovements(form:MyWindow) =
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.R then readResidue()) 
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.N then nextResiduum()) 
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.D then drawResiduum())
        form.KeyDown.Add(fun e -> if e.KeyCode = Keys.H then hiliteCurrentResiduum())

    // ----------------------------------------------------------------------------------------------------    
    // Inits GUI, GraficSystem
    // ---------------------------------------------------------------------------------------------------- 
    let Init() =   
        InitTesselationFactor(4.0f)
        initShape(Shape.AtomBond)   
        setPixelShader(ShaderClass.LambertPSType) 
        SetRasterizerState(RasterType.Wired) 
        SetBlendState(BlendType.Opaque)
        mainWindow.MainMenuStrip <- mainMenue
        mainWindow.Controls.Add(mainWindow.MainMenuStrip)    
        addStandardKeyMovements(graficWindow)
        addProteinKeyMovements(graficWindow)
        addStandardMouseMovements(graficWindow)

    let Start() = 
        logger.Info("\nStart")
        initDisplayables() 
        MySystem.Instance.Start()
        writeToMessageWindow("Application started")