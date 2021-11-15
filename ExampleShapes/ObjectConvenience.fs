namespace Surfaces
//
//  ObjectConvenience.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX

open GraficBase.SurfaceElements

open Geometry
open Geometry.GeometricModel

// ----------------------------------------------------------------------------------------------------
// Convenience
// ----------------------------------------------------------------------------------------------------
module ObjectConvenience =
    
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

    let defaultObjectSize   = 0.5f
    let antDefaultEnergy    = 50.0f
    let antDefaultCapacity  = 100.0f

    let COLLIDE_GROUND_POSITION = Vector3(0.0f,  1.0f, -12.0f) 
    let DROPIN_POSITION         = Vector3(0.0f, 10.0f,   0.0f)

    let DOWN_DIRECTION          = Vector3.UnitY * -1.0f
    let BACK_DIRECTION          = Vector3.UnitZ *  1.0f
    let FORWARD_DIRECTION       = Vector3.UnitZ * -1.0f
    let RIGHT_DIRECTION         = Vector3.UnitX *  1.0f
    let LEFT_DIRECTION          = Vector3.UnitX * -1.0f

    // ----------------------------------------------------------------------------------------------------
    // MATERIAL
    // ----------------------------------------------------------------------------------------------------
    let MATERIAL(name, color:Color) = 
        new Material(
            name = "MAT-" + name + color.ToColor4().ToString(),
            ambient = Color4(0.2f),
            diffuse = Color4.White,
            specular = Color4.White,
            specularPower = 20.0f,
            emissive = color.ToColor4()
        ) 

    let TEXTURE(name:string, texturName:string) =
        let textureName = (texturName.Split('.')).[0]
        new Texture (
            name=textureName,
            fileName=texturName,
            pathName=""
        )

    let MAT_RED = 
        MATERIAL("RED", Color.Red)

    let MAT_ORANGE = 
        MATERIAL("ORANGE", Color.Orange)

    let MAT_BLUE = MATERIAL("BLUE", Color.Blue)

    let MAT_GREEN = MATERIAL("GREEN", Color.Green)

    let MAT_MAGENTA = MATERIAL("MAGENTA", Color.Magenta)

    let MAT_CYAN = MATERIAL("CYAN", Color.Cyan)

    let MAT_DARKGOLDENROD =
        MATERIAL("DARKGOLDENROD", Color.DarkGoldenrod)

    let MAT_DARKSLATEGRAY =
        MATERIAL("DARKSLATEGRAY", Color.DarkSlateGray)

    let MAT_BLACK = MATERIAL("BLACK", Color.Black)

    let MAT_WHITE = MATERIAL("WHITE", Color.White)

    let MATERIAL_LIMIT (name, color: Color) =
        new Material(
            name = "MAT-" + name,
            ambient = Color.Transparent.ToColor4(),
            diffuse = Color.Transparent.ToColor4(),
            specular = Color.Transparent.ToColor4(),
            specularPower = 0.0f,
            emissive = color.ToColor4()
        )

    // ----------------------------------------------------------------------------------------------------
    // TEXTUR
    // ----------------------------------------------------------------------------------------------------

    let TEXT__WALL = TEXTURE("WALL", "ebonykate.jpg")

    let TEXT_KUGEL = TEXTURE("KUGEL", "water_texture.jpg")

    let TEXT_FOOD  = TEXTURE("FOOD",   "ebonykate.jpg")

    let TEXT_HILL = TEXTURE("HILL",   "ebonykate.jpg")

    let TEXT_ANT  =  TEXTURE("ANT", "Ant_color.jpg")

    let TEXT_ANTHILL  =  TEXTURE("ANTHILL",  "texture_140.jpg")

    let TEXT_GROUND  =  TEXTURE("GROUND","grass.jpg")

    let TEXT_PRED =  TEXTURE("PRED", "Predator1.jpg")

    let TEXT_SPHERE = TEXTURE("SPHERE", "water_texture.jpg")

    let TEXT_WALL   = TEXTURE("WALL", "ebonykate.jpg")

    // ----------------------------------------------------------------------------------------------------
    // GEOMETRY
    // ----------------------------------------------------------------------------------------------------
    let CORPUS (aContour) = 
        Corpus(
            name="CORPUS",
            contour=aContour,
            height=5.0f,
            colorBottom=Color.White,
            colorTop=Color.White,
            colorSide=Color.White
        )        
    let MINI_CUBE = 
        Würfel(
            "SMALLCUBE", 
            0.5f,
            Color.Red,          // Front
            Color.Green,        // Right
            Color.Blue,         // Back  
            Color.Cyan,         // Left
            Color.Yellow,       // Top        
            Color.Orange        // Bottom            
        )
    let BIG_CUBE = 
        Würfel(
            "BIGCUBE", 
            3.0f,
            Color.Red,          // Front
            Color.Green,        // Right
            Color.Blue,         // Back  
            Color.Cyan,         // Left
            Color.Yellow,       // Top        
            Color.Orange        // Bottom            
        ) 
    let SMALL_CUBE(name) = 
        Würfel(
            name, 
            2.0f,
            Color.Red,          // Front
            Color.Green,        // Right
            Color.Blue,         // Back  
            Color.Cyan,         // Left
            Color.Yellow,       // Top        
            Color.Orange        // Bottom            
        ) 
        
    // Im Uhrteigersinn unten
    let CONTOUR_PLATE =
        [|Vector3( 0.0f, 0.0f, -5.0f);
            Vector3( 1.0f, 0.0f, -5.0f);
            Vector3( 2.0f, 0.0f, -5.0f);
            Vector3( 3.0f, 0.0f, -5.0f);

            Vector3( 4.0f, 0.0f, -4.0f);
            Vector3( 4.0f, 0.0f, -3.0f);
            Vector3( 4.0f, 0.0f, -2.0f);
            Vector3( 3.0f, 0.0f, -1.0f);

            Vector3( 2.0f, 0.0f, -1.0f);
            Vector3( 1.0f, 0.0f, -1.0f);
            Vector3( 0.0f, 0.0f, -1.0f);
            Vector3(-1.0f, 0.0f, -2.0f);

            Vector3(-1.0f, 0.0f, -3.0f);
            Vector3(-1.0f, 0.0f, -4.0f);
            Vector3( 0.0f, 0.0f, -5.0f) 
         |] 