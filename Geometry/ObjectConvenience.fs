namespace Geometry
//
//  ObjectConvenience.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX

open Geometry.GeometricModel

// ----------------------------------------------------------------------------------------------------
// Convenience
// ----------------------------------------------------------------------------------------------------
module ObjectConvenience =
    // ----------------------------------------------------------------------------------------------------
    // COLOR
    // ----------------------------------------------------------------------------------------------------
    let COLOR_GROUND    = Color.DarkGray
    let COLOR_HILL      = Color.DarkSeaGreen
    let COLOR_ANT       = Color.DarkGreen
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
            name="MAT-" + name + color.ToColor4().ToString(),
            ambient=Color4(0.2f),
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=20.0f,
            emissive=color.ToColor4()
        )
    let MAT_RED = 
        MATERIAL("RED", Color.Red)

    let MAT_BLUE = 
        MATERIAL("BLUE", Color.Blue)

    let MAT_GREEN = 
        MATERIAL("GREEN", Color.Green)

    let MAT_MAGENTA = 
        MATERIAL("MAGENTA", Color.Magenta)

    let MAT_DARKGOLDENROD = 
        MATERIAL("DARKGOLDENROD", Color.DarkGoldenrod)

    let MAT_DARKSLATEGRAY = 
        MATERIAL("DARKSLATEGRAY", Color.DarkSlateGray)

    let MAT_BLACK = 
        MATERIAL("BLACK", Color.Black)

    // ----------------------------------------------------------------------------------------------------
    // SURFACE
    // ----------------------------------------------------------------------------------------------------
    let SURFACE(name, color:Color, texturName:string) = 
        if texturName = "" then            
            new Surface(
                Material(
                    name="MAT-" + name + color.ToString(),
                    ambient=Color4(0.2f),
                    diffuse=Color4.White,
                    specular=Color4.White,
                    specularPower=0.0f,
                    emissive=color.ToColor4()
                )
            )
        else
            let textureName = (texturName.Split('.')).[0]
            new Surface(
                Texture (
                    textureName,
                    "AntBehaviourApp",
                    "textures",
                    texturName
                ),
                Material(
                    name="MAT-" + name + color.ToString(),
                    ambient=Color4(0.2f),
                    diffuse=Color4.White,
                    specular=Color4.White,
                    specularPower=20.0f,
                    emissive=color.ToColor4()
                )
            )

    let SURFACE_LIMIT(name, color:Color) = 
        new Surface(
            Material(
                name="MAT-" + name,
                ambient=Color.Transparent.ToColor4(),
                diffuse=Color.Transparent.ToColor4(),
                specular=Color.Transparent.ToColor4(),
                specularPower=0.0f,
                emissive=color.ToColor4()
            ),
            visibility=Visibility.Transparent
        )

    let SURFACE_WALL(color:Color) = 
        SURFACE("WALL", color, "ebonykate.jpg")

    let SURFACE_KUGEL(color:Color) = 
        SURFACE("KUGEL", color, "water_texture.jpg") 

    let SURFACE_FOOD = 
        SURFACE("FOOD", COLOR_FOOD, "ebonykate.jpg")

    let SURFACE_HILL = 
        SURFACE("HILL", COLOR_HILL, "ebonykate.jpg")

    let SURFACE_ANTHILL = 
        SURFACE("ANTHILL", COLOR_ANTHILL, "ebonykate.jpg")

    let SURFACE_GROUND = 
        SURFACE("GROUND", COLOR_GROUND, "water_texture.jpg")

    let SURFACE_ANT = 
        SURFACE("ANT", COLOR_ANT, "ebonykate.jpg")

    let SURFACE_PREDATOR = 
        SURFACE("PREDATOR", COLOR_PREDATOR, "Predator1.jpg")