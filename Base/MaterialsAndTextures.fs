namespace Base
//
//  MaterialsAndTextures.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic

open SharpDX

open ModelSupport
open Framework

// ----------------------------------------------------------------------------------------------------
// Speicher für viel benutzte Eigenschaften
// ----------------------------------------------------------------------------------------------------
module MaterialsAndTextures =

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
            new Material( 
                name=name,
                ambient=Color4(0.2f),
                diffuse=Color4.White,
                specular=Color4.White,
                specularPower=20.0f,
                emissive=color.ToColor4()
            )

    let MAT(name) =  
            new Material( 
                name=name,
                ambient=Color4(1.2f),
                diffuse=Color4.White,
                specular=Color4.White,
                specularPower=30.0f,
                emissive=Color4(0.2f)
            )

    let MAT_EARTH = MAT("EARTH")

    let MAT_GROUND = 
        new Material( 
            name="GROUND",
            ambient=Color4(9.2f),
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=5.0f,
            emissive=Color4(0.1f)
        )

    let MAT_FRONT = MATERIAL("FRONT", Color.DarkSlateGray)
    let MAT_NONE = MATERIAL("NONE", Color.Transparent)
    let MAT_BLUE = MATERIAL("BLUE", Color.Blue)
    let MAT_LIGHT_BLUE = MATERIAL("LIGHT_BLUE", Color.LightBlue)
    let MAT_RED = MATERIAL("RED", Color.Red)
    let MAT_GREEN = MATERIAL("GREEN", Color.DarkGreen)
    let MAT_ORANGE = MATERIAL("ORANGE", Color.Orange)
    let MAT_YELLOW = MATERIAL("YELLOW", Color.Yellow)
    let MAT_MAGENTA = MATERIAL("MAGENTA", Color.Magenta)
    let MAT_CYAN = MATERIAL("CYAN", Color.Cyan)
    let MAT_BLACK = MATERIAL("BLACK", Color.Black)
    let MAT_WHITE = MATERIAL("WHITE", Color.White)
    let MAT_ANTHILL = MATERIAL("ANTHILL", Color.Maroon)
    let MAT_ANT = MAT("ANT")
    let MAT_HILL = MATERIAL("HILL", Color.Transparent)
    let MAT_PRED = MATERIAL("PRED", Color.Black)
    let MAT_BEIGE = MATERIAL("BEIGE",Color.Beige) 
    let MAT_DARKGOLDENROD = MATERIAL("DARKGOLDENROD", Color.DarkGoldenrod)
    let MAT_DARKSLATEGRAY = MATERIAL("DARKSLATEGRAY", Color.DarkSlateGray) 

    let MATERIAL_LIMIT (name, color: Color) =
        new Material(
            name = name,
            ambient = Color.Transparent.ToColor4(),
            diffuse = Color.Transparent.ToColor4(),
            specular = Color.Transparent.ToColor4(),
            specularPower = 20.0f,
            emissive = color.ToColor4()
        )

    let MAT_UMGEBUNG_TRANSPARENT =
        new Material(
            name = "MAT_UMGEBUNG_TRANSPARENT",
            ambient = Color.Transparent.ToColor4(),
            diffuse = Color.Transparent.ToColor4(),
            specular = Color4.White,
            specularPower = 20.0f,
            emissive = Color.Transparent.ToColor4()
        ) 
    let MAT_UMGEBUNG_OPAQUE_EMPTY =
        new Material(
            name = "MAT_UMGEBUNG_OPAQUE_EMPTY",
            ambient = Color.LightBlue.ToColor4(),
            diffuse = Color4.White,
            specular = Color4.White,
            specularPower = 20.0f,
            emissive = Color.Blue.ToColor4()
        )

    let MAT_UMGEBUNG_OPAQUE_NOTEMPTY =
        new Material(
            name = "MAT_UMGEBUNG_OPAQUE_NOTEMPTY",
            ambient = Color4(0.2f),
            diffuse = Color4.White,
            specular = Color4.White,
            specularPower = 20.0f,
            emissive = Color.White.ToColor4()
        ) 

    // ----------------------------------------------------------------------------------------------------
    // TEXTUR
    // ----------------------------------------------------------------------------------------------------
    let TEXTURE(name:string, texturName:string) =
        let textureName = (texturName.Split('.')).[0]
        new Texture (
            name=textureName,
            fileName=texturName,
            pathName=""
        )

    let TEXT_EMPTY = TEXTURE("", "")
    let TEXT_WALL = TEXTURE("WALL", "texture_140.jpg")
    let TEXT_KUGEL = TEXTURE("KUGEL", "water_texture.jpg")
    let TEXT_QUADER = TEXTURE("QUADER", "crate.jpg")
    let TEXT_EARTH = TEXTURE("EARTH", "8081_earthmap2k.jpg")
    let TEXT_EARTH_HR = TEXTURE("EARTHHR", "8081_earthmap10k.jpg")
    let TEXT_FOOD = TEXTURE("FOOD", "ebonykate.jpg")
    let TEXT_HILL = TEXTURE("HILL", "texture_140.jpg")
    let TEXT_ANT = TEXTURE("ANT", "Ant_color.jpg")
    let TEXT_ANTHILL = TEXTURE("ANTHILL", "texture_140.jpg")
    let TEXT_GRASS = TEXTURE("GROUND", "grass.jpg")
    let TEXT_GROUND = TEXTURE("GROUND", "grass.jpg")
    let TEXT_PRED = TEXTURE("PRED", "Predator1.jpg")
    let TEXT_SPHERE = TEXTURE("SPHERE", "water_texture.jpg")

    let DefaultMaterials () =
        [
            MAT_GROUND  ;
            MAT_FRONT ;
            MAT_NONE   ;
            MAT_BLUE  ;
            MAT_LIGHT_BLUE   ;
            MAT_RED  ;
            MAT_ORANGE   ;
            MAT_YELLOW   ;
            MAT_GREEN   ;
            MAT_MAGENTA   ;
            MAT_CYAN   ;
            MAT_DARKGOLDENROD  ;
            MAT_DARKSLATEGRAY  ;
            MAT_BLACK   
        ]