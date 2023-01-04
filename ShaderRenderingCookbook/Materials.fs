namespace ShaderRenderingCookbook
//
//  MaterialsAndTextures.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open SharpDX

open Structures

// ----------------------------------------------------------------------------------------------------
// Speicher für viel benutzte Eigenschaften
// ----------------------------------------------------------------------------------------------------
module Materials =

    let GRAFIC_ECO_PATH =
        "C:/Users/Lugi2/source/F#/Framework/EcoGrafics/ExampleSurfaces/bin/x64/Debug/net471/texture/" 
        
    let ECO_PATH = System.Reflection.Assembly.GetExecutingAssembly().GetName()
   
    let DIFFUSE_LIGHT = Color.LightYellow.ToColor4()

    // ----------------------------------------------------------------------------------------------------
    // COLOR
    // ----------------------------------------------------------------------------------------------------
    let COLOR_GROUND    = Color.DarkGray
    let COLOR_HILL      = Color.DarkSeaGreen
    let COLOR_ANT       = Color.DarkGoldenrod
    let COLOR_PREDATOR  = Color.Black
    let COLOR_ANTHILL   = Color.Black
    let COLOR_FOOD      = Color.SandyBrown

    // ----------------------------------------------------------------------------------------------------
    // MATERIAL
    // Achtung: Alle Materials müssen instanziert werden
    // Sonst klappt das Umschalten nicht
    // ----------------------------------------------------------------------------------------------------
    let MATERIAL(name, color:Color) =  
            new MaterialConstants( 
                Ambient=Color4(0.2f),
                Diffuse=Color4.White,
                Specular=Color4.White,
                SpecularPower=20.0f,
                HasTexture= false,
                Emissive=color.ToColor4()
            )

    let MAT_EARTH   = MATERIAL("EARTH", Color.DarkSlateGray)
    let MAT_FRONT   = MATERIAL("FRONT", Color.DarkSlateGray)
    let MAT_NONE    = MATERIAL("NONE",  Color.Transparent)

    let MAT_BLUE    = MATERIAL("BLUE",  Color.Blue)
    let MAT_BEIGE   = MATERIAL("BEIGE", Color.Beige)
    let MAT_BROWN   = MATERIAL("BROWN", Color.Brown) 
    let MAT_BLACK   = MATERIAL("BLACK", Color.Black)
    let MAT_CYAN    = MATERIAL("CYAN",  Color.Cyan)
    let MAT_DGROD   = MATERIAL("DARKGOLDENROD", Color.DarkGoldenrod)
    let MAT_DSGRAY  = MATERIAL("DARKSLATEGRAY", Color.DarkSlateGray) 
    let MAT_GREEN   = MATERIAL("GREEN", Color.DarkGreen)
    let MAT_GRAY    = MATERIAL("GRAY", Color.Gray)
    let MAT_LT_BLUE = MATERIAL("LIGHT_BLUE", Color.LightBlue)
    let MAT_MAGENTA = MATERIAL("MAGENTA", Color.Magenta)
    let MAT_ORANGE  = MATERIAL("ORANGE", Color.Orange)
    let MAT_RED     = MATERIAL("RED", Color.Red)
    let MAT_SILVER  = MATERIAL("MAT_SILVER", Color.Silver) 
    let MAT_WHITE   = MATERIAL("WHITE", Color.White)
    let MAT_AWHITE  = MATERIAL("AWHITE", Color.AntiqueWhite)
    let MAT_YELLOW  = MATERIAL("YELLOW", Color.Yellow)
    
    let MAT_ANTHILL = MATERIAL("ANTHILL", Color.Maroon)
    let MAT_ANT     = MATERIAL("ANT",    Color.Transparent)
    let MAT_ANTGRD  = MATERIAL("ANTGRD", Color.Transparent) 
    let MAT_HILL    = MATERIAL("HILL",  Color.Transparent)
    let MAT_PRED    = MATERIAL("PRED",  Color.Black)
    let MAT_GROUND  = MATERIAL("GROUND", Color.DarkSlateGray)
    let MAT_WATER   = MATERIAL("WATER", Color.Transparent)
    let MAT_TRANSP  = MATERIAL("TRANSPARENT", Color.Transparent)