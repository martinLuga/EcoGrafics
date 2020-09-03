namespace FrameworkTests
//
//  Initializations.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 

open log4net

open SharpDX
open SharpDX.Direct3D
open SharpDX.DXGI 

open Geometry.GeometricModel

open ApplicationBase.MoveableObject
open ApplicationBase.DisplayableObject

module Initializations = 

    type Texture = Geometry.GeometricModel.Texture

    let logger = LogManager.GetLogger("Tests")
    
    let device = new Direct3D12.Device(null, FeatureLevel.Level_11_0) 

    let LogOutputDisplayModes(output:Output, format:Format) =      
        let modeList = output.GetDisplayModeList(format, DisplayModeEnumerationFlags.Interlaced)             
        for x in modeList do 
            let n = x.RefreshRate.Numerator  
            let d = x.RefreshRate.Denominator  
            let text =  "Width = " +  x.Width.ToString() + " " + "Height = " + x.Height.ToString() + " " + "Refresh = " +  n.ToString() + "/" + d.ToString() + "\n" 
            logger.Debug(text)

    let LogAdapterOutputs(adapter:Adapter) =
        for out in adapter.Outputs do 
            let desc = out.Description 
            let text = "***Output: " + desc.DeviceName  + "\n" 
            logger.Info(text)
            LogOutputDisplayModes(out, Format.B8G8R8A8_UNorm)      

    let getGraphicObjects() =
        let sphere1 = 
            new Moveable(
                name="sphere1",
                geometry=Kugel("sphere1", 2.0f, Color.Transparent),
                surface=Surface(
                    Texture(
                       "bricks",
                       "BasicApp",
                       "textures",
                       "bricks.dds"
                    ),
                    Material(  
                       "woodCrate",
                        Color.White.ToVector4(),
                        new Vector3(0.05f),
                        0.2f
                    )
                ),
                position=Vector3(-4.0f, -4.0f, 0.0f),
                direction=Vector3.UnitY,
                velocity=0.0f,
                color=Color.Transparent,
                moveRandom=false
            )    

        let sphere2 = 
            Moveable(
                name="sphere2",
                geometry=Kugel("sphere2",  3.0f, Color.Transparent),
                surface=Surface(
                    Texture(
                        "bricks",
                        "BasicApp",
                        "textures",
                        "bricks.dds"
                    ),
                    Material(  
                        "woodCrate",
                        Color.White.ToVector4(),
                        new Vector3(0.05f),
                        0.2f
                    )
                ),
                position=Vector3(4.0f, -4.0f, 0.0f),
                direction=Vector3.UnitY,
                velocity=0.0f,
                color=Color.Transparent,
                moveRandom=false
             )   

        let cylinder1 = 
            new Moveable(
                name="cylinder1",
                geometry=Cylinder("cylinder1", 0.1f, 2.0f, Color.Red, Color.Green),
                surface=Surface(
                    Texture(
                        "bricks",
                        "BasicApp",
                        "textures",
                        "bricks.dds"
                    ),
                    Material(  
                        "woodCrate",
                        Color.White.ToVector4(),
                        new Vector3(0.05f),
                        0.2f
                    )
                ),
                position=Vector3(4.0f, 0.0f, 0.0f),
                direction=Vector3.UnitY,
                velocity=0.0f,
                color=Color.Transparent,
                moveRandom=false
                )    
        
        [sphere1:>Displayable; sphere2:>Displayable; cylinder1:>Displayable]