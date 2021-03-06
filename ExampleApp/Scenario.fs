namespace ExampleApp
///
///  Objects.fs
///
///  Created by Martin Luga on 08.02.18.
///  Copyright © 2018 Martin Luga. All rights reserved.
///

open System.IO

open SharpDX 

open Geometry
open Geometry.GeometricModel
open Geometry.GeometricElements

open Base.GlobalDefs

open ApplicationBase.MoveableObject
open ApplicationBase.DisplayableObject
open ApplicationBase.WindowControl
open ApplicationBase.ScenarioSupport
open ApplicationBase.GraficSystem

open DirectX.Camera

open Geometry.GeometricModel2D

open Molecules.Atome
open MoleculeDrawing.MoleculeDraw
open MoleculeBuild

open MoleculeBuild.BondBuilder

type Texture = Geometry.GeometricElements.Texture

type Shape = Sphere  | Cube  | Cylinder  | Adobe | Pyramid | Skull | Car | AtomBond | AtomBuilder | Korpus | Icosahedron | GroundPlane | ManyObjects | TwoD

/// <summary> 
/// Status
///
///      Geometrie               OK  
///      Größe                   OK
///      Farben aus Geometrie    OK
///      Position                OK 
///      Generelle Farbe = Albedo 
///      Textur                  OK
///      Überdeckung             nicht OK
///  
/// Offen
///
///      1. zZ muss Objektname und Geometryname übereinstimmen.
///         Trennung nicht überall sauber. ObjectBuffer vs. VertexBuffer
///      2. Bestimmte Konstellationen in Material führen im SimpleShader zur Nichtanzeige
///
/// </summary>   
module Scenario =

    /// <summary> 
    /// Text-Nachrichten Ausgabe
    /// </summary> 
    let writeObjectReport(objekt:Displayable) =
        writeToOutputWindow("Objekt  : "   + objekt.Name) 
        writeToOutputWindow("Geometry: "   + objekt.Geometry.ToString())
        writeToOutputWindow("Position: "   + objekt.Position.ToString())
        writeToOutputWindow("Center  : "   + objekt.Center.ToString())
        writeToOutputWindow("Bounds  : "   + objekt.Boundaries.ToString())
        newLineOutputWindow()

    let writeReportObjects(displayables:Displayable list) =
        clearOutputWindow()
        for disp in displayables do
            writeObjectReport(disp)

    let DISPLAYABLE_XMIN = 0.0f
    let DISPLAYABLE_XMAX = 20.0f
    let DISPLAYABLE_YMIN = 0.0f
        
    let MINI_CUBE = 
        Würfel(
            "SMALLCUBE", 
            0.5f,
            Color.Red,          /// Front
            Color.Green,        /// Right
            Color.Blue,         /// Back  
            Color.Cyan,         /// Left
            Color.Yellow,       /// Top        
            Color.Orange        /// Bottom            
        )

    let BIG_CUBE = 
        Würfel(
            "BIGCUBE", 
            3.0f,
            Color.Red,          /// Front
            Color.Green,        /// Right
            Color.Blue,         /// Back  
            Color.Cyan,         /// Left
            Color.Yellow,       /// Top        
            Color.Orange        /// Bottom            
        ) 

    let SMALL_CUBE = 
        Würfel(
            "SMALLCUBE", 
            2.0f,
            Color.Red,          /// Front
            Color.Green,        /// Right
            Color.Blue,         /// Back  
            Color.Cyan,         /// Left
            Color.Yellow,       /// Top        
            Color.Orange        /// Bottom            
        )
    
    let MAT_RED = 
        Material(
            name="MAT_RED",
            ambient=Color4(0.2f),
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=20.0f,
            emissive=Color.Red.ToColor4()
        )

    let MAT_BLUE = 
        Material(
            name="MAT_BLUE",
            ambient=Color4(0.2f),
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=20.0f,
            emissive=Color.Blue.ToColor4()
        )

    let MAT_GREEN = 
        Material(
            name="MAT_GREEN",
            ambient=Color4(0.2f),
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=20.0f,
            emissive=Color.Green.ToColor4()
        ) 

    let MAT_MAGENTA = 
        Material(
            name="MAT_MAGENTA",
            ambient=Color4(0.2f),
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=20.0f,
            emissive=Color.Magenta.ToColor4()
        ) 

    let MAT_DARKGOLDENROD = 
            Material( 
                name="MAT_DARKGOLDENROD",
                ambient=Color4(0.2f),
                diffuse=Color4.White,
                specular=Color4.White,
                specularPower=20.0f,
                emissive=Color.DarkGoldenrod.ToColor4()
            ) 
    let MAT_DARKSLATEGRAY = 
            Material( 
                name="MAT_DARKSLATEGRAY",
                ambient=Color4(0.2f),
                diffuse=Color4.White,
                specular=Color4.White,
                specularPower=20.0f,
                emissive=Color.DarkSlateGray.ToColor4()
            ) 

    let MAT_BLACK = 
            Material( 
                name="MAT_BLACK",
                ambient=Color4(0.2f),
                diffuse=Color4.White,
                specular=Color4.White,
                specularPower=20.0f,
                emissive=Color.Black.ToColor4()
            ) 

    let MAT_WHITE = 
            Material( 
                name="MAT_WHITE",
                ambient=Color4(0.2f),
                diffuse=Color4.White,
                specular=Color4.White,
                specularPower=20.0f,
                emissive=Color4.White
            ) 
    
    let xAxis = 
        Immoveable(
            name="xAxis",
            geometry= Line(
                name="XAxis", 
                von=Vector3(-10.0f, 0.0f, 0.0f),
                bis=Vector3( 10.0f, 0.0f, 0.0f),
                color=Color.White
            ),
            surface=Surface(
                MAT_WHITE
            ),
            color=Color.White,
            position=Vector3(0.0f, 0.0f, 0.0f)
        ) 
    let yAxis = 
        Immoveable(
            name="yAxis",
            geometry= Line(
                name="YAxis", 
                von=Vector3(0.0f, -10.0f, 0.0f),
                bis=Vector3(0.0f,  10.0f, 0.0f),
                color=Color.White
            ),
            surface=Surface(
                MAT_WHITE
            ),
            color=Color.White,
            position=Vector3(0.0f, 0.0f, 0.0f)
        ) 
    let zAxis = 
        Immoveable(
            name="zAxis",
            geometry= Line(
                name="ZAxis", 
                von=Vector3(0.0f, 0.0f, -10.0f),
                bis=Vector3(0.0f, 0.0f,  10.0f),
                color=Color.White
            ),
            surface=Surface(
                MAT_WHITE
            ),
            color=Color.White,
            position=Vector3(0.0f, 0.0f, 0.0f)
        ) 
    let AXES =
        [xAxis:>Displayable;yAxis:>Displayable;zAxis:>Displayable]

    let CubeObjects() =
        MySystem.Instance.Reset()
        initCamera(Vector3( 0.0f, 5.0f, -15.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)
        let cube1 = 
            Immoveable(
                name="cube1",
                geometry=BIG_CUBE,
                surface=Surface(
                    MAT_BLUE
                ),
                color=Color.Black,
                position=Vector3(-4.0f, 0.0f, 0.0f)
            )       

        let cube2 = 
            Immoveable(
                name="cube2",
                geometry=SMALL_CUBE,
                surface=Surface(
                    MAT_RED,
                    Visibility.Transparent
                ),
                color=Color.Green,
                position=Vector3( 0.0f, 0.0f, 0.0f)
            )       
        
        let cube22 = 
            Immoveable(
                name="cube22",
                geometry=SMALL_CUBE,
                surface=Surface(
                    MAT_GREEN
                ),
                color=Color.Green,
                position=Vector3( 0.0f, -4.0f, 0.0f)
            ) 

        let cube3 = 
            Immoveable(
                name="cube3",
                geometry=BIG_CUBE,
                surface=Surface(
                    MAT_MAGENTA
                ),
                color=Color.Green,
                position=Vector3( 4.0f, 0.0f, 0.0f)
            ) 
       
        let displayables = [cube3:>Displayable; cube22:>Displayable; cube2:>Displayable; cube1:>Displayable]
        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary> 
    /// Kugel
    ///     Achtung: Im Material wird auch hasTexture gespeichert
    ///     Bei gleichem Material wird diese Eigenschaft übernommen
    /// </summary>   
    let SphereObjects() = 
        MySystem.Instance.Reset()
        let DIFFUSE_LIGHT = Color.LightYellow.ToColor4()
        initCamera(Vector3( 0.0f, 5.0f, -15.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)
        let sphere1 = 
            new Immoveable(
                name="sphere1",
                geometry=new Kugel("sphere1", 1.5f,  Color.DimGray),
                surface=new Surface(
                    Material(
                        name="mat1",
                        ambient=Color4(0.2f),
                        diffuse=DIFFUSE_LIGHT,
                        specular=Color4.White,
                        specularPower=20.0f,
                        emissive=Color.Red.ToColor4()
                    )
                ),
                color=Color.DarkOrange,
                position=Vector3(-4.0f, 0.0f, 0.0f)
            )  

        let sphere2 = 
            new Immoveable(
                name="sphere2",
                geometry=new Kugel("sphere2", 1.5f, Color.DimGray),
                surface=new Surface(
                    Material(
                        name="mat2",
                        ambient=Color4(0.2f),
                        diffuse=DIFFUSE_LIGHT,
                        specular=Color4.White,
                        specularPower=20.0f,
                        emissive=Color.Green.ToColor4()
                    ),
                    Visibility.Transparent
                ),
                color=Color.DarkOrange,
                position=Vector3(0.0f, 0.0f, 0.0f)
            )  

        let sphere3 = 
            new Immoveable(
                name="sphere3",
                geometry=new Kugel("sphere3", 1.5f,  Color.DimGray),
                surface=new Surface(
                    Material(
                        name="mat3",
                        ambient=Color4(0.2f),
                        diffuse=DIFFUSE_LIGHT,
                        specular=Color4.White,
                        specularPower=20.0f,
                        emissive=Color.Blue.ToColor4()
                    )
                ),
                color=Color.DarkOrange,
                position=Vector3(4.0f, 0.0f, 0.0f)
            )

        let displayables = [sphere1:>Displayable; sphere2:>Displayable; sphere3:>Displayable]
        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary> 
    /// Pyramide
    /// </summary>   
    let PyramidObjects() =
        MySystem.Instance.Reset()
        initCamera(Vector3( 0.0f, 5.0f, -15.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)
        let pyramid1 = 
            new Immoveable(
             name="pyramid1",
             geometry=
                new Pyramide(
                 name="pyramid1",
                 seitenLaenge=3.0f,
                 hoehe=4.0f,
                 colorFront=Color.Red,
                 colorRight=Color.Green,
                 colorBack=Color.Blue,
                 colorLeft=Color.Yellow,
                 colorBasis=Color.Orange
                 ),  
             surface=Surface(
                 Material( 
                     name="mat1",
                     ambient=Color4(0.2f),
                     diffuse=Color4.White,
                     specular=Color4.White,
                     specularPower=20.0f,
                     emissive=Color.Green.ToColor4()
                  )
             ),
             color=Color.Black,
             position=Vector3(-4.0f, 0.0f, 0.0f)
        ) 

        let pyramid2 = 
            new Immoveable(
             name="pyramid2",
             geometry=
                new Pyramide(
                 name="pyramid2",
                 seitenLaenge=3.0f,
                 hoehe=4.0f,
                 colorFront=Color.Red,
                 colorRight=Color.Green,
                 colorBack=Color.Yellow,
                 colorLeft=Color.Cyan,
                 colorBasis=Color.Orange
                 ),  
             surface=Surface(
                 Material(
                     name="mat2",
                     ambient=Color4(0.2f),
                     diffuse=Color4.White,
                     specular=Color4.White,
                     specularPower=20.0f,
                     emissive=Color.Red.ToColor4()
                  ),
                  Visibility.Transparent
             ),
             color=Color.Black,
             position=Vector3( 0.0f, 0.0f, 0.0f)
        ) 

        let pyramid3 = 
            new Immoveable(
             name="pyramid3",
             geometry=
                new Pyramide(
                 name="pyramid3",
                 seitenLaenge=3.0f,
                 hoehe=4.0f,
                 colorFront=Color.Red,
                 colorRight=Color.Green,
                 colorBack=Color.Yellow,
                 colorLeft=Color.Cyan,
                 colorBasis=Color.Orange
                 ),  
             surface=Surface(
                 Material(
                     name="mat3",
                     ambient=Color4(0.2f),
                     diffuse=Color4.White,
                     specular=Color4.White,
                     specularPower=20.0f,
                     emissive=Color.Blue.ToColor4()
                  )  
             ),
             color=Color.Black,
             position=Vector3( 4.0f, 0.0f, 0.0f)
        ) 
        let displayables = [pyramid1:>Displayable; pyramid2:>Displayable; pyramid3:>Displayable]
        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary> 
    /// Quader
    /// Achtung. 2 verschiedene Materials dürfen nicht den gleichen Namen haben
    ///          wegen Texture
    /// </summary>   
    let AdobeObjects() =
        MySystem.Instance.Reset()
        initCamera(Vector3( 0.0f, 5.0f, -15.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)
        let adobe1 = 
            new Immoveable(
             name="adobe1",
             geometry=new Quader("adobe1", 3.0f, 4.0f, 4.0f, Color.Brown),
             surface=Surface(
                 Texture(
                    "texture_140",
                    "ExampleApp",
                    "textures",
                    "texture_140.jpg"
                 ),
                 Material(
                     name="mat1",
                     ambient=Color4(0.2f),
                     diffuse=Color4.White,
                     specular=Color4.White,
                     specularPower=20.0f,
                     emissive=Color.Green.ToColor4()
                  )
             ),
             color=Color.Green,
             position=Vector3(-4.0f, 0.0f, 0.0f)
        )      

        let adobe2 = 
            new Immoveable(
                 name="adobe2",
                 geometry=new Quader(
                    name="adobe2",
                    laenge=3.0f,
                    hoehe=2.0f,
                    breite=4.0f,
                    colorFront=Color.Green,
                    colorRight=Color.Red,
                    colorBack=Color.Blue,
                    colorLeft=Color.Yellow,
                    colorTop=Color.Brown,
                    colorBottom=Color.Orange
                 ),
                 surface=Surface(
                     Material( 
                         name="mat2",
                         ambient=Color4(0.2f),
                         diffuse=Color4.White,
                         specular=Color4.White,
                         specularPower=20.0f,
                         emissive=Color.Red.ToColor4()
                      ) ,
                      Visibility.Transparent 
                 ),
                 color=Color.White,
                 position=Vector3(0.0f, 0.0f, 0.0f)
            ) 

        let adobe3 = 
            new Immoveable(
                name="adobe3",
                geometry=new Quader(
                    name="adobe3",
                    laenge=4.0f,
                    hoehe=2.0f,
                    breite=4.0f,
                    colorFront=Color.Red,
                    colorRight=Color.Green,
                    colorBack=Color.Blue,
                    colorLeft=Color.Cyan,
                    colorTop=Color.Yellow,
                    colorBottom=Color.Orange
                ),
                surface=Surface(
                    Material(
                        name="mat3",
                        ambient=Color4(0.2f),
                        diffuse=Color4.White,
                        specular=Color4.White,
                        specularPower=20.0f,
                        emissive=Color.Blue.ToColor4()
                    ) 
                ),
                color=Color.White,
                position=Vector3(4.0f, 0.0f, 0.0f)
            )  

        let displayables = [adobe1:>Displayable; adobe2:>Displayable;adobe3:>Displayable]
        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary> 
    /// Drei Cylinder
    /// </summary>   
    let CylinderObjects() =
        MySystem.Instance.Reset()
        initCamera(Vector3( 0.0f, 5.0f, -15.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)
        GeometricModel.setCylinderRaster (Raster.Mittel) 
        let cylinder1 = 
            new Immoveable(
             name="cylinder1",
             geometry=new Cylinder("cylinder1", 1.f, 3.0f, Color.Blue, Color.Green, true),
             surface=Surface(MAT_BLUE),
             color=Color.Transparent,
             position=Vector3( -4.0f,  0.0f,  0.0f)
             )  

        let cylinder2 = 
            new Immoveable(
             name="cylinder2",
             geometry=new Cylinder("cylinder2", 1.f, 3.0f, Color.Red, Color.Yellow, true),
             surface=Surface(MAT_DARKGOLDENROD,Visibility.Transparent),
             color=Color.Transparent,
             position=Vector3( 0.0f,  0.0f,  0.0f)
            )  
        
        let cylinder3 = 
            new Immoveable(
                name="cylinder3",
                geometry=new Cylinder("cylinder3", 1.0f, 3.0f, Color.Olive, Color.Orange, true),
                surface=Surface(MAT_MAGENTA),
                color=Color.Transparent,
                position=Vector3(4.0f,  0.0f,  0.0f)
            )    

        let displayables = [cylinder1:>Displayable; cylinder2:>Displayable;  cylinder3:>Displayable]
        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary>  
    /// Objekte definiert durch eine Menge von vertexes. Hier ein Schädel
    /// </summary>  
    let SkullContourObjects() =
        MySystem.Instance.Reset()
        initCamera(Vector3( 0.0f, 5.0f, -15.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)
        let skull1 = 
            new Immoveable(
                name="Skull",
                geometry=DreiD("Skull", "models\\Skull.txt", Color.LightBlue, 0.0f),
                surface=Surface(
                     Material( 
                         name="mat1",
                         ambient=Color4(0.2f),
                         diffuse=Color4.White,
                         specular=Color4.White,
                         specularPower=20.0f,
                         emissive=Color.DarkSlateGray.ToColor4()
                      ) 
                 ),
                 color=Color.DarkSlateGray,
                 position=Vector3(-4.0f, -3.0f,  0.0f)             
            )   

        let skull2 = 
            Immoveable(
                name="Skull2",
                geometry=DreiD("Skull2", "models\\Skull.txt", Color.LightGreen, 0.0f),
                surface=Surface(
                     Material( 
                         name="mat2",
                         ambient=Color4(0.2f),
                         diffuse=Color4.White,
                         specular=Color4.White,
                         specularPower=20.0f,
                         emissive=Color.SlateGray.ToColor4()
                      ),
                      Visibility.Transparent
                 ),
                 color=Color.DarkSlateGray,
                 position=Vector3( 4.0f, -3.0f,  0.0f)             
            )

        let displayables = [skull1:>Displayable; skull2:>Displayable]
        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary>  
    /// Objekte definiert durch eine Menge von Vertexes. Hier ein Auto
    /// </summary> 
    let  CarContourObjects() =
        MySystem.Instance.Reset()
        initCamera(Vector3( 0.0f, 5.0f, -15.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)
        let car1 = 
            Immoveable(
                name="Car",
                geometry=DreiD("Car", "models\\Car.txt", Color.LightGreen, 0.0f),
                surface=Surface(
                     Material( 
                         name="mat1",
                         ambient=Color4(0.2f),
                         diffuse=Color4.White,
                         specular=Color4.White,
                         specularPower=20.0f,
                         emissive=Color.DarkSlateGray.ToColor4()
                      ) 
                 ),
                 color=Color.DarkSlateGray,
                 position=Vector3(0.0f, 0.0f,  0.0f)             
             )   
 
        let displayables = [car1:>Displayable]
        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary>  
    /// 4 Atome mit einem Bond
    /// </summary>   
    let AtomWithBondObjects() =
        MySystem.Instance.Reset()
        GeometricModel.setCylinderRaster (Raster.Grob) 
        initCamera(Vector3( 0.0f, 5.0f, -10.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)

        let wasserstoff = 
            new Atom(
                position=Vector3(-2.0f,  0.0f,  0.0f),
                label="O",
                serial=1 
                )        

        let kohlenstoff = 
            new Atom(
                position=Vector3( 0.0f,  0.0f,  3.0f),
                label="C",
                serial=2
                )        

        let magnesium = 
            new Atom(
                position=Vector3( 3.0f,  0.0f,  3.0f),
                label="MG",
                serial=3
                )    
                
        let natrium = 
            new Atom(
                position=Vector3( 0.0f, -3.0f,  0.0f),
                label="NA",
                serial=4
                )  

        let bond12 =
            new Bond(
                wasserstoff,
                kohlenstoff,
                BondType.Singlebond
                )

        let bond21 =
            new Bond(
                wasserstoff,
                kohlenstoff,
                BondType.Singlebond
                )

        let bond13 =
            new Bond(
                wasserstoff,
                magnesium,
                BondType.Singlebond
                )

        let bond34 =
            new Bond(
                magnesium,
                natrium,
                BondType.Singlebond
                )
                
        let bond43 =
            new Bond(
                magnesium,
                natrium,
                BondType.Singlebond
                )
        let atoms =  [wasserstoff; kohlenstoff] 
        // let atoms =  [wasserstoff; kohlenstoff; magnesium; natrium] 
        let atomList = atoms |> Seq.ofList
        let atome = atomList |> ResizeArray

        let bondBuilder = new BondBuilder(atome)

        let bonds =  [bond12]
        //let bonds =  [bond12; bond21; bond13; bond34; bond43]

        let shrinkBonds = bondBuilder.Shrink(bonds)
        let bonds2 = shrinkBonds.ToArray() |> List.ofArray

        let disp1 =  atoms |> List.map(fun x -> x:>Displayable)
        let disp2 =  bonds2 |> List.map(fun x -> x:>Displayable)

        let displayables =  List.concat [disp2; disp1]

        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary>  
    /// AtomBuilder Test
    /// Test der Darstellung  eines transparenten Kastens um ein Residuum
    /// Simple PixelShader weil die Farbe im Vertex beeinflusst werden soll
    /// </summary>   
    let AtomBuilderObjects() =
        MySystem.Instance.Reset()
        
        logger.Debug("Objects.AtomBuilder")
        
        GeometricModel.setCylinderRaster (Raster.Grob)

        let mutable filePath = "C:\\Users\\Lugi2\\Source\\repos\\EcoChemical\\ExampleMolecules\\molecules\\Insulin.pdb"
        let object = FileInfo(filePath)   
        let fileName = object.FullName
        let moleculeName = object.Name.Split('.').[0]
        
        myMolecule <- PDBBuilder.buildFromFile (fileName, moleculeName)
        writeMoleculeReport(myMolecule)

        readResidue()
        nextResiduum()

        let residuum = myResidueEnumerator.Current 
        writeMoleculeReport(residuum)

        let target = residuum.Center
        let pos = target + 20.0f * Vector3.BackwardLH
        initCamera(pos, target, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL) 
        //repositionCameraOnMolecule(residuum, 10.0f)
        let hilite = createHilite (residuum, "1") 
        let displayables = residuum.getDisplayables() |> List.append([hilite]) |> List.rev

        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary>  
    /// Korpus test
    /// </summary>   
    let KorpusObjects() =
        MySystem.Instance.Reset()
        initCamera(Vector3( 0.0f, 5.0f, -15.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)

        /// Im Uhrteigersinn unten
        let CONTOUR =
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

        let CORPUS = 
            Corpus(
                name="CORPUS",
                contour=CONTOUR,
                height=2.0f,
                colorBottom=Color.White,
                colorTop=Color.White,
                colorSide=Color.White
            )

        let plate1 = 
            new Immoveable(
                name="plate1",
                geometry=CORPUS,
                surface=new Surface(MAT_DARKSLATEGRAY),
                color=Color.Black,
                position=Vector3(-4.0f, 0.0f, 0.0f)
            )  

        let plate2 = 
            new Immoveable(
                name="plate2",
                geometry=CORPUS,
                surface=new Surface(
                    MAT_DARKGOLDENROD,
                    Visibility.Transparent
                ),
                color=Color.Black,
                position=Vector3(0.0f, 4.0f, 4.0f)
            )  

        let adobe1 = 
            new Immoveable(
             name="adobe1",
             geometry=new Quader("adobe1", 3.0f, 4.0f, 4.0f, Color.Brown),
             surface=Surface(
                 Texture(
                    "texture_140",
                    "ExampleApp",
                    "textures",
                    "texture_140.jpg"
                 ),
                 Material(
                     name="mat1",
                     ambient=Color4(0.2f),
                     diffuse=Color4.White,
                     specular=Color4.White,
                     specularPower=20.0f,
                     emissive=Color.Green.ToColor4()
                  )
             ),
             color=Color.Green,
             position=Vector3(4.0f, 0.0f, 0.0f)
        ) 
        
        let displayables = [plate1:>Displayable; plate2:>Displayable; adobe1:>Displayable]
        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary>  
    /// Tesselated objects test
    /// </summary>   
    let GroundPlaneObjects() =
        MySystem.Instance.Reset()
        initCamera(Vector3( 0.0f, 5.0f, -15.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)
        let ground = 
            new Immoveable(
                name="Ground",
                geometry=new QuadPatch(
                    name="GroundPlane", 
                    seitenLaenge=DISPLAYABLE_XMAX - DISPLAYABLE_XMIN,
                    color=Color.LightPink,
                    tessFactor=12.0f
                ),
                surface=new Surface(                    
                    Material( 
                        name="mat1",
                        ambient=Color4(0.2f),
                        diffuse=Color4.White,
                        specular=Color4.White,
                        specularPower=20.0f,
                        emissive=Color.DarkSlateGray.ToColor4()
                     ) 
                ),
                position=Vector3(DISPLAYABLE_XMIN, DISPLAYABLE_YMIN, - 0.3f ),
                color=Color.Gray
            ) 
        let displayables = [ground:>Displayable]
        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary>  
    /// Tesselation Test. Zwei Ikosaeder, einer transparent, der andere opak.
    /// </summary>   
    let IcosahedronObjects() =
        MySystem.Instance.Reset()
        initCamera(Vector3( 0.0f, 5.0f, -15.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)
        let icosahedron1 = 
            new Immoveable(
                name="icosahedron",
                geometry=new Polyeder(
                    name="Icosahedron", 
                    center= Vector3(0.0f, 0.0f, -2.0f),
                    radius=3.0f,
                    color=Color.DarkRed,
                    tessFactor=4.0f                  
                    ),
                surface=new Surface(MAT_DARKGOLDENROD),
                color=Color.Red,
                position=Vector3(-4.0f, 2.0f, 0.0f)
                ) 

        let icosahedron2 = 
            new Immoveable(
                name="icosahedron2",
                geometry=new Polyeder(
                    name="Icosahedron2", 
                    center= Vector3(0.0f, 0.0f, -2.0f),
                    radius=3.0f,
                    color=Color.DarkBlue,
                    tessFactor=4.0f                  
                    ),
                surface=new Surface(MAT_BLACK, Visibility.Transparent),
                color=Color.Red,
                position=Vector3(4.0f, 2.0f, 0.0f)
            ) 
        let displayables = [icosahedron1:>Displayable; icosahedron2:>Displayable]
        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary>  
    /// Many objects test
    /// </summary>   
    let ManyObjectsObjects() =
        MySystem.Instance.Reset()
        initCamera(Vector3( 0.0f, 5.0f, -15.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)
        let START_POS = Vector3(-15.0f, 0.0f, 0.0f)

        let cube(i) = 
            Immoveable(
                name="cube" + i.ToString(),
                geometry=MINI_CUBE,
                surface=Surface(
                    MAT_GREEN
                ),
                color=Color.Green,
                position=START_POS + Vector3.UnitX * 2.0f * (float32) i
            )
            :>Displayable        
 
        let displayables = seq { for i in 1 .. 20 ->  cube(i) } |> Seq.toList
        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary>  
    /// 2D - Objekte, z.B. Formen, Schrift (in Entwicklung)
    /// </summary>   
    let TwoDObjects() =
        MySystem.Instance.Reset()  
        initCamera(Vector3( 0.0f, 5.0f, -15.0f), Vector3.Zero, aspectRatio, DEFAULT_ROT_HORIZONTAL, DEFAULT_ROT_VERTICAL)
        let square = 
            Immoveable(
                name="square",
                geometry= Square(
                    name="Square", 
                    seitenlaenge=5.0f,
                    color=Color.White
                ),
                surface=Surface(
                    MAT_BLUE
                ),
                color=Color.White,
                position=Vector3(2.0f, 0.0f, 0.0f)
            )  

        let displayables = [square:>Displayable; xAxis:>Displayable; yAxis:>Displayable]
        writeReportObjects(displayables)
        MySystem.Instance.InitObjects(displayables |> List.append AXES)

    /// <summary>
    /// Initialize
    /// </summary>
    let CreateScenarios() =
        AddScenario(0,  "Cube",         CubeObjects)               /// Cube Objekte
        AddScenario(1,  "Sphere",       SphereObjects)             /// Sphere Objekte
        AddScenario(2,  "Adobe",        AdobeObjects)              /// Adobe Objekte
        AddScenario(3,  "Pyramid",      PyramidObjects)            /// Pyramide Objekte
        AddScenario(4,  "Cylinder",     CylinderObjects)           /// Cylinder Objekte
        AddScenario(5,  "SkullContour", SkullContourObjects)       /// Datei Objekte
        AddScenario(6,  "CarContour",   CarContourObjects)         /// Datei Objekte
        AddScenario(7,  "AtomWithBond", AtomWithBondObjects)       /// Atom  Objekte
        AddScenario(8,  "AtomBuilder",  AtomBuilderObjects)        /// Atom  Objekte
        AddScenario(9,  "Korpus",       KorpusObjects)             /// Korpus Objekte
        AddScenario(10, "GroundPlane",  GroundPlaneObjects)         /// Plane
        AddScenario(11, "Icosahedron",  IcosahedronObjects)         /// Icosahedron
        AddScenario(12, "ManyObjects",  ManyObjectsObjects)         /// Test mit vielen Objekten
        AddScenario(13, "TwoD",         TwoDObjects)                /// 2D Objekte