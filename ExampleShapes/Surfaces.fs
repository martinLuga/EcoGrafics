namespace Surfaces
///
///  Objects.fs
///
///  Created by Martin Luga on 08.02.18.
///  Copyright © 2018 Martin Luga. All rights reserved.
///

open SharpDX 

open DirectX

open GraficBase.SurfaceElements

open Geometry
open Geometry.GeometricModel  

open Base.GlobalDefs

open ObjectConvenience


type Shape = Sphere  | Cube  | Cylinder  | Adobe | Pyramid | Skull | Car | AtomBond | AtomBuilder | Korpus | Icosahedron | GroundPlane | ManyObjects | TwoD

/// <summary> 
///
/// Example Surfaces
///
/// </summary>   
module Surfaces =


    /// <summary> 
    /// Würfel
    /// </summary> 
    ///
    /// TODO: Problem mit der Reihenfolge. 
    /// Transparenz muss am Ende in den Displayables stehen. Dies wird durch sort im GraficSystem erreicht
    /// Trotzdem gibt es Fehler, wenn ein anderes mit der gleichen Geometrie danach kommt
    ///
    let CubeSurfaces() =
        let cube1=
            new Surface(
                shape= Würfel(
                    "CUBE1", 
                    3.0f,
                    Color.Red,          // Front
                    Color.Green,        // Right
                    Color.Blue,         // Back  
                    Color.Cyan,         // Left
                    Color.Yellow,       // Top        
                    Color.Orange        // Bottom            
                ),
                material=new Material(
                    name="MAT-" + "BIGCUBE" + Color.Blue.ToColor4().ToString(),
                    ambient=Color4(0.2f),
                    diffuse=Color4.White,
                    specular=Color4.White,
                    specularPower=20.0f,
                    emissive=Color.Blue.ToColor4()
                )
            )   
 
        let CUBE2 =
            Surface(shape = SMALL_CUBE("CUBE2"), material = MAT_RED, visibility = Visibility.Transparent)

        let CUBE3 =
            Surface(shape = SMALL_CUBE("CUBE3"), material = MAT_GREEN)


        let BIG_CUBE =
            Surface(shape = BIG_CUBE, material = MAT_MAGENTA)

        ignore
    
     /// <summary> 
    /// Kugel
    ///     Achtung: Im Material wird auch hasTexture gespeichert
    ///     Bei gleichem Material wird diese Eigenschaft übernommen
    /// </summary>   
    let SphereSurfaces() =  
        let SPHERE1 =
            new Surface(
                shape=new Kugel("SPHERE1", 1.5f,  Color.DimGray), 
                material=new Material(
                    name="mat1",
                    ambient=Color4(0.2f),
                    diffuse=DIFFUSE_LIGHT,
                    specular=Color4.White,
                    specularPower=20.0f,
                    emissive=Color.Red.ToColor4()
                )
            ) 
 
        let SPHERE2 =
            new Surface(
                shape=new Kugel("SPHERE2", 1.5f, Color.DimGray),
                    material=new Material(
                        name="mat2",
                        ambient=Color4(0.2f),
                        diffuse=DIFFUSE_LIGHT,
                        specular=Color4.White,
                        specularPower=20.0f,
                        emissive=Color.Green.ToColor4()
                    ),
                    visibility=Visibility.Transparent
                ) 

        let SPHERE3 =
            new Surface(
                shape=new Kugel("SPHERE3", 1.5f,  Color.DimGray),
                material=new Material(
                    name="mat3",
                    ambient=Color4(0.2f),
                    diffuse=DIFFUSE_LIGHT,
                    specular=Color4.White,
                    specularPower=20.0f,
                    emissive=Color.Blue.ToColor4()
                )
            ) 
        ignore

    /// <summary> 
    /// Pyramide
    /// </summary>   
    let PyramidSurfaces() = 
        let PYRAMID1 = 
            new Surface(
                shape=new Pyramide(
                    name="PYRAMID1",
                    seitenLaenge=3.0f,
                    hoehe=4.0f,
                    colorFront=Color.Red,
                    colorRight=Color.Green,
                    colorBack=Color.Blue,
                    colorLeft=Color.Yellow,
                    colorBasis=Color.Orange
                ),   
                material=new Material( 
                    name="mat1",
                    ambient=Color4(0.2f),
                    diffuse=Color4.White,
                    specular=Color4.White,
                    specularPower=20.0f,
                    emissive=Color.Green.ToColor4()
                )
            ) 
 
        let PYRAMID2=
            new Surface(
                shape=new Pyramide(
                    name="PYRAMID2",
                    seitenLaenge=3.0f,
                    hoehe=4.0f,
                    colorFront=Color.Red,
                    colorRight=Color.Green,
                    colorBack=Color.Yellow,
                    colorLeft=Color.Cyan,
                    colorBasis=Color.Orange
                    ),  
                material=new Material(
                    name="mat2",
                    ambient=Color4(0.2f),
                    diffuse=Color4.White,
                    specular=Color4.White,
                    specularPower=20.0f,
                    emissive=Color.Red.ToColor4()
                ) 
            )
             
        let PYRAMID3=
            new Surface(
                shape=new Pyramide(
                    name="PYRAMID3",
                    seitenLaenge=3.0f,
                    hoehe=4.0f,
                    colorFront=Color.Red,
                    colorRight=Color.Green,
                    colorBack=Color.Yellow,
                    colorLeft=Color.Cyan,
                    colorBasis=Color.Orange
                ),  
                material=Material(
                    name="mat3",
                    ambient=Color4(0.2f),
                    diffuse=Color4.White,
                    specular=Color4.White,
                    specularPower=20.0f,
                    emissive=Color.Blue.ToColor4()
                )  
            ) 
        ignore

    /// <summary> 
    /// Quader
    /// Achtung. 2 verschiedene Materials dürfen nicht den gleichen Namen haben
    ///          wegen Texture
    /// </summary>   
    let AdobeSurfaces() = 

        let ADOBE1 =
            new Surface(
                shape = new Quader("ADOBE1", 3.0f, 4.0f, 4.0f, Color.Brown),
                material =
                    new Material(
                        name = "mat1",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 20.0f,
                        emissive = Color.Green.ToColor4()
                    ),
                texture = Texture(name = "texture_140", fileName = "texture_140.jpg", pathName = "")
            ) 
 
        let ADOBE2 =
            new Surface(
                shape =
                    new Quader(
                        name = "ADOBE2",
                        laenge = 3.0f,
                        hoehe = 2.0f,
                        breite = 4.0f,
                        colorFront = Color.Green,
                        colorRight = Color.Red,
                        colorBack = Color.Blue,
                        colorLeft = Color.Yellow,
                        colorTop = Color.Brown,
                        colorBottom = Color.Orange
                    ),
                material =
                    new Material(
                        name = "mat2",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 20.0f,
                        emissive = Color.Red.ToColor4()
                    ),
                visibility = Visibility.Transparent
            )  
        let ADOBE3 =
            new Surface(
                shape =
                    new Quader(
                        name = "ADOBE3",
                        laenge = 4.0f,
                        hoehe = 2.0f,
                        breite = 4.0f,
                        colorFront = Color.Red,
                        colorRight = Color.Green,
                        colorBack = Color.Blue,
                        colorLeft = Color.Cyan,
                        colorTop = Color.Yellow,
                        colorBottom = Color.Orange
                    ),
                material =
                    new Material(
                        name = "MAT3",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 20.0f,
                        emissive = Color.Blue.ToColor4()
                    )
            )

        ignore

    /// <summary> 
    /// Drei Cylinder
    /// </summary>   
    let CylinderSurfaces() = 
        
        GeometricModel.setCylinderRaster (Raster.Mittel) 
 
        let CYLINDER1 =
            new Surface(shape = new Cylinder("CYLINDER1", 1.f, 3.0f, Color.Blue, Color.Green, true), material = MAT_BLUE)

        let CYLINDER2 =
            Surface(
                shape = new Cylinder("CYLINDER2", 1.f, 3.0f, Color.Red, Color.Yellow, true),
                material = MAT_DARKGOLDENROD,
                visibility = Visibility.Transparent
            )

        let CYLINDER3 =
            Surface(shape = new Cylinder("CYLINDER3", 1.0f, 3.0f, Color.Olive, Color.Orange, true), material = MAT_MAGENTA) 

        ignore

    /// <summary>  
    /// Objekte definiert durch eine Menge von vertexes. Hier ein Schädel
    /// </summary>  
    let Skull3DSurfaces() =
        
        let SKULL =
            Surface(
                shape = DreiD("SKULL", "model3d\\Skull.txt", Color.LightBlue, 0.0f),
                material =
                    new Material(
                        name = "mat1",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 20.0f,
                        emissive = Color.DarkSlateGray.ToColor4()
                    )
            )


        let SKULL2 =
            Surface(
                shape = DreiD("SKULL2", "model3d\\Skull.txt", Color.LightGreen, 0.0f),
                material =
                    new Material(
                        name = "mat2",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 20.0f,
                        emissive = Color.SlateGray.ToColor4()
                    ),
                visibility = Visibility.Transparent
            ) 
            
        ignore

    /// <summary>  
    /// Objekte definiert durch eine Menge von Vertexes. Hier ein Auto
    /// </summary> 
    let  Car3DSurfaces() =
        
        let CAR =
            new Surface(
                shape = DreiD("CAR", "model3d\\Car.txt", Color.LightGreen, 0.0f),
                material =
                    new Material(
                        name = "MAT1",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 20.0f,
                        emissive = Color.DarkSlateGray.ToColor4()
                    )
            )

        ignore

    /// <summary>  
    /// Objekte definiert durch eine Menge von Vertexes. Hier ein Auto
    /// </summary> 
    let HandgunSurfaces() = 
        let GUN =
            new Surface(
                shape = WavefrontObj("GUN", "C:\\temp\\obj\\Handgun_obj.obj", Color.LightGreen, 0.0f),
                material =
                    new Material(
                        name = "MGUN",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 20.0f,
                        emissive = Color.SlateGray.ToColor4()
                    )
            )
        ignore

    /// <summary>  
    /// Objekte definiert durch eine Menge von Vertexes. Hier ein Auto
    /// </summary> 
    let BodySurfaces() =
        let BODY =
            new Surface(
                shape = WavefrontObj("BODY", "C:\\temp\\obj\\FinalBaseMesh.obj", Color.LightGreen, 0.0f),
                material =
                    new Material(
                        name = "MBODY",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 20.0f,
                        emissive = Color.SaddleBrown.ToColor4() // Die Fabe zieht
                    )
            )  
        ignore

    /// <summary>  
    /// Objekte definiert durch eine Menge von Vertexes. Hier ein Auto
    /// </summary> 
    let CooperSurfaces() =
 
        let COOPER =
            new Surface(
                shape = WavefrontObj("COOPER", "C:\\temp\\obj\\MiniCooper.obj", Color.LightGreen, 0.0f),
                material =
                    new Material(
                        name = "MCOOPER",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 20.0f,
                        emissive = Color.Yellow.ToColor4()
                    )
            )
        ignore
    
    ///
    /// <summary>
    /// Objekte definiert durch eine Menge von Vertexes. Hier ein Auto
    /// </summary>
    let AntScenario () =

        let ANT1 =
            WavefrontObj("ANT1", "C:\\temp\\obj\\formica rufa.obj", Color.Transparent, 0.0f)

        ANT1.resize (3.0f)

        let ANT2 =
            WavefrontObj("ANT2", "C:\\temp\\obj\\ant.obj", Color.Transparent, 0.0f)

        ANT2.resize (1.1f)
 
        let ANT =
            new Surface(
                shape = ANT1,
                material =
                    new Material(
                        name = "MANT",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 20.0f,
                        emissive = Color.Beige.ToColor4()
                    ),
                texture = Texture(name = "texture_Ant", fileName = "", pathName = "C:\\temp\\obj\\Ant_color.jpg")
            ) 

        ignore
 

    /// <summary>  
    /// Objekte definiert durch eine Menge von Vertexes. Hier ein Auto
    /// </summary> 
    let AntHillSurfaces() = 

        let ANTHILL_SHAPE =
            WavefrontObj("ANTHILL", "C:\\temp\\obj\\AntHill.obj", 0.1f, Color.Black, 0.0f)

        let antHill =
            new Surface(
                shape = ANTHILL_SHAPE,
                material =
                    new Material(
                        name = "MANTHILL",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 4.0f,
                        emissive = Color.Maroon.ToColor4()
                    )
            )

        ignore

    ///
    /// <summary>  
    /// Objekte definiert durch eine Menge von Vertexes. Hier ein Auto
    /// </summary> 
    let TreeSurfaces () =

        let materials =
            Wavefront.materialContext ("C:\\temp\\obj\\Lowpoly_tree_sample.obj")

        let treeSurface =
            new Surface(
                shape = WavefrontObj("TREE", "C:\\temp\\obj\\Lowpoly_tree_sample.obj", Color.LightGreen, 0.0f),
                material = materials.Item("Bark")
            )

        ignore 
 
    /// <summary>  
    /// Korpus test
    /// </summary>   
    let KorpusSurfaces() = 

        /// Im Uhrteigersinn unten
        let CONTOUR =
            [| Vector3(0.0f, 0.0f, -5.0f)
               Vector3(1.0f, 0.0f, -5.0f)
               Vector3(2.0f, 0.0f, -5.0f)
               Vector3(3.0f, 0.0f, -5.0f)

               Vector3(4.0f, 0.0f, -4.0f)
               Vector3(4.0f, 0.0f, -3.0f)
               Vector3(4.0f, 0.0f, -2.0f)
               Vector3(3.0f, 0.0f, -1.0f)

               Vector3(2.0f, 0.0f, -1.0f)
               Vector3(1.0f, 0.0f, -1.0f)
               Vector3(0.0f, 0.0f, -1.0f)
               Vector3(-1.0f, 0.0f, -2.0f)

               Vector3(-1.0f, 0.0f, -3.0f)
               Vector3(-1.0f, 0.0f, -4.0f)
               Vector3(0.0f, 0.0f, -5.0f) |] 

        let CORPUS =
            Corpus(
                name = "CORPUS",
                contour = CONTOUR,
                height = 2.0f,
                colorBottom = Color.White,
                colorTop = Color.White,
                colorSide = Color.White
            )

        let plate1Surface =
            new Surface(shape = CORPUS, material = MAT_DARKSLATEGRAY, visibility = Visibility.Transparent)

        let plate2 =
            new Surface(shape = CORPUS, material = MAT_DARKGOLDENROD) 

        ignore

    /// <summary>  
    /// Tesselated objects test
    /// </summary>   
    let GroundPlaneSurfaces() = 

        let FRONT =
            new Surface(
                shape =
                    Fläche.InXYPlane(
                        name = "FRONT",
                        p1 = Vector3.Zero,
                        seitenlaenge = 10.0f,
                        normal = Vector3.BackwardLH,
                        color = Color.Transparent
                    ),
                material =
                    new Material(
                        name = "Grey",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 20.0f,
                        emissive = Color.DarkSlateGray.ToColor4()
                    )
            )

        let GROUND = 
            new Surface(   
                shape=Fläche.InXZPlane( 
                    name="GROUND", 
                    p1=Vector3.Zero,
                    seitenlaenge=10.0f,
                    normal=Vector3.Up,
                    color=Color.Transparent
                ),
                material=Material( 
                    name="Golden",
                    ambient=Color4(0.2f),
                    diffuse=Color4.White,
                    specular=Color4.White,
                    specularPower=20.0f,
                    emissive=Color.Goldenrod.ToColor4()
                    ) 
            ) 

        let RIGHT =
            new Surface(
                shape =
                    Fläche.InYZPlane(
                        name = "RIGHT",
                        p1 = Vector3.Zero,
                        seitenlaenge = 10.0f,
                        normal = Vector3.Right,
                        color = Color.Transparent
                    ),
                material =
                    Material(
                        name = "Blue",
                        ambient = Color4(0.2f),
                        diffuse = Color4.White,
                        specular = Color4.White,
                        specularPower = 20.0f,
                        emissive = Color.CornflowerBlue.ToColor4()
                    )
            ) 
        ignore

    /// <summary>  
    /// Tesselation Test. Zwei Ikosaeder, einer transparent, der andere opak.
    /// </summary>   
    let IcosahedronSurfaces() = 
        let ICOSAHEDRON1 = 
            new Surface(   
                shape=new Polyeder(
                    name="ICOSAHEDRON1", 
                    radius=3.0f,
                    color=Color.DarkRed,
                    tessFactor=4.0f                  
                ),
                material=MAT_DARKGOLDENROD
            ) 

        let ICOSAHEDRON2 = 
            new Surface(   
                shape=Polyeder(
                    name="ICOSAHEDRON2", 
                    radius=3.0f,
                    color=Color.DarkBlue,
                    tessFactor=4.0f                  
                ),
                material=MAT_BLACK,
                visibility=Visibility.Transparent
            ) 
        ignore 

    /// <summary>  
    /// 2D - Objekte, z.B. Formen, Schrift (in Entwicklung)
    /// </summary>   
    let TwoDObjectSurfaces() = 
        let FRONT = 
            new Surface(   
                shape=
                    Quadrat.InXYPlane(
                        name="FRONT",  
                        ursprung=Vector3.Zero,
                        seitenlaenge=5.0f, 
                        color=Color.White
                    ),
                material=MAT_DARKGOLDENROD
            )

        let LEFT = 
            new Surface(   
                shape=
                    Quadrat.InYZPlane(
                        name="LEFT",  
                        ursprung=Vector3.Zero,
                        seitenlaenge=5.0f, 
                        color=Color.White
                    ),
                material=MAT_DARKGOLDENROD
            )
        
        let BOTTOM = 
            new Surface(   
                shape=Quadrat.InXZPlane(
                    name="BOTTOM",  
                    ursprung=Vector3.Zero,
                    seitenlaenge=5.0f, 
                    color=Color.White
                ),
                material=MAT_DARKGOLDENROD
            )

        let RIGHT = 
            new Surface(   
                shape=Quadrat.InYZPlane(
                    name="RIGHT",  
                    ursprung=Vector3.Zero,
                    seitenlaenge=5.0f, 
                    color=Color.White
                ),
                material=MAT_DARKGOLDENROD
            )
        
        let TOP = 
            new Surface(   
                shape=Quadrat.InXZPlane(
                    name="TOP",  
                    ursprung=Vector3.Zero,
                    seitenlaenge=5.0f, 
                    color=Color.White
                ),
                material=MAT_DARKGOLDENROD
            )

        ignore