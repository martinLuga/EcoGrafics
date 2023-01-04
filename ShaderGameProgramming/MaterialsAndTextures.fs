namespace ShaderGameProgramming
//
//  MaterialsAndTextures.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open SharpDX

open Base.ModelSupport

// ----------------------------------------------------------------------------------------------------
// Speicher für viel benutzte Eigenschaften
// ----------------------------------------------------------------------------------------------------
module MaterialsAndTextures =

    let GRAFIC_ECO_PATH =
        "C:/Users/Lugi2/source/F#/Framework/EcoGrafics/ExampleSurfaces/bin/x64/Debug/net471/texture/" 
        
    let ECO_PATH = System.Reflection.Assembly.GetExecutingAssembly().GetName()

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
    let MAT (name, diffuseAlbedo, fresnelR0, roughness) =
        new Material(
            name = name,
            diffuseAlbedo = diffuseAlbedo,
            fresnelR0 = fresnelR0,
            roughness = roughness,
            matTransform=Matrix.Identity
        )

    let MAT_EARTH   = MAT("EARTH", Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_FRONT   = MAT("FRONT",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_NONE    = MAT("NONE",   Color.White.ToVector4(), Vector3(0.05f), 0.2f)

    let MAT_BLUE    = MAT("BLUE",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_BEIGE   = MAT("BEIGE",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_BROWN   = MAT("BROWN",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_BLACK   = MAT("BLACK",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_CYAN    = MAT("CYAN",   Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_DGROD   = MAT("DARKGOLDENROD",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_DSGRAY  = MAT("DARKSLATEGRAY",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_GREEN   = MAT("GREEN",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_GRAY    = MAT("GRAY",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_LT_BLUE = MAT("LIGHT_BLUE",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_MAGENTA = MAT("MAGENTA",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_ORANGE  = MAT("ORANGE",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_RED     = MAT("RED",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_SILVER  = MAT("MAT_SILVER",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_WHITE   = MAT("WHITE",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_AWHITE  = MAT("AWHITE",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_YELLOW  = MAT("YELLOW",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    
    let MAT_ANTHILL = MAT("ANTHILL",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_ANT     = MAT("ANT",     Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_ANTGRD  = MAT("ANTGRD",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_HILL    = MAT("HILL",   Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_PRED    = MAT("PRED",   Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_GROUND  = MAT("GROUND", Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_WATER   = MAT("WATER",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)
    let MAT_TRANSP  = MAT("TRANSPARENT",  Color.White.ToVector4(), Vector3(0.05f), 0.2f)

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
    let TEXTURE(name:string, texturName:string, isCube:bool) =
        let textureFileName = GRAFIC_ECO_PATH +  texturName 
        new Texture (
            name=name,
            fileName=textureFileName,
            pathName="",
            isCube=isCube
        ) 
        
    let TEXT_EMPTY      = TEXTURE("", "", false)
    let TEXT_ANT        = TEXTURE("ANT", "Ant_color.jpg", false)
    let TEXT_ANTHILL    = TEXTURE("ANTHILL", "grass.jpg", false)
    let TEXT_EARTH      = TEXTURE("EARTH", "8081_earthmap2k.jpg", false)
    let TEXT_EARTH_HR   = TEXTURE("EARTHHR", "8081_earthmap10k.jpg", false)
    let TEXT_FOOD       = TEXTURE("FOOD", "ebonykate.jpg", false)
    let TEXT_GRASS      = TEXTURE("GRASS", "grass.jpg", false)
    let TEXT_GROUND     = TEXTURE("GROUND", "texture_140.jpg", false)
    let TEXT_HILL       = TEXTURE("HILL", "texture_140.jpg", false)
    let TEXT_KUGEL      = TEXTURE("KUGEL", "water_texture.jpg", false)
    let TEXT_PRED       = TEXTURE("PRED", "Predator1.jpg", false)
    let TEXT_QUADER     = TEXTURE("QUADER", "crate.jpg", false)
    let TEXT_SKY        = TEXTURE("SKY", "grasscube1024.dds", true)
    let TEXT_SPHERE     = TEXTURE("SPHERE", "water_texture.jpg", false)
    let TEXT_WALL       = TEXTURE("WALL", "texture_140.jpg", false)
    let TEXT_WATER      = TEXTURE("WATER", "water_texture.jpg", false)
    let TEXT_WOOD       = TEXTURE("WOOD", "wooden-textured-background.jpg", false)
    let TEXT_BUMPER     = TEXTURE("BUMBER", "Bumper-100.jpg", false)
    let TEXT_FLPG       = TEXTURE("FLIPPERGROUND", "texture_140.jpg", false)
    let TEXT_BITCN      = TEXTURE("BITCOIN", "bitcoin-g66b7a0261_1280.png", false)
    let TEXT_BACKSIDE   = TEXTURE("BACKSIDE", "Backside.jpg", false)

    let TEXT_NULL       = TEXTURE("NULL",   "//Digits//Null.jpg",  false)
    let TEXT_ONE        = TEXTURE("ONE",    "//Digits//One.jpg",   false)
    let TEXT_TWO        = TEXTURE("TWO",    "//Digits//Two.jpg",   false)
    let TEXT_THREE      = TEXTURE("THREE",  "//Digits//Three.jpg", false)
    let TEXT_FOUR       = TEXTURE("FOUR",   "//Digits//Four.jpg",  false)
    let TEXT_FIFE       = TEXTURE("FIFE",   "//Digits//Fife.jpg",  false)
    let TEXT_SIX        = TEXTURE("SIX",    "//Digits//Six.jpg",   false)
    let TEXT_SEVEN      = TEXTURE("SEVEN",  "//Digits//Seven.jpg", false)
    let TEXT_EIGHT      = TEXTURE("EIGHT",  "//Digits//Eight.jpg", false)
    let TEXT_NINE       = TEXTURE("NINE",   "//Digits//Nine.jpg",  false)
    let TEXT_POINT      = TEXTURE("POINT",  "//Digits//Point.jpg", false)

    let DefaultMaterials  =
        [ MAT_BLACK
          MAT_BLUE
          MAT_CYAN
          MAT_DGROD
          MAT_DSGRAY
          MAT_FRONT
          MAT_GREEN
          MAT_GROUND
          MAT_LT_BLUE
          MAT_MAGENTA
          MAT_NONE
          MAT_ORANGE
          MAT_RED
          MAT_WHITE
          MAT_YELLOW ] 

    let DefaultTextures =
        [ 
          TEXT_EMPTY
          TEXT_GROUND
          TEXT_EARTH
          TEXT_EARTH_HR
          TEXT_KUGEL
          TEXT_QUADER
          TEXT_SPHERE
          TEXT_WALL
          TEXT_GRASS
          TEXT_HILL
          TEXT_WATER
          TEXT_WOOD
          TEXT_NULL
          TEXT_ONE
          TEXT_TWO
          TEXT_THREE
          TEXT_FOUR
          TEXT_FIFE
          TEXT_SIX
          TEXT_SEVEN
          TEXT_EIGHT
          TEXT_NINE
          TEXT_POINT ]