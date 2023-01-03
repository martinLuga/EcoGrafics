namespace ExampleSurfaces
// 
//   Objects.fs
// 
//   Created by Martin Luga on 08.02.18.
//   Copyright © 2018 Martin Luga. All rights reserved.
// 

open SharpDX 
 
open Geometry.GeometricModel2D  
open Geometry.GeometricModel3D  
 
open Base.ModelSupport
open Base.MaterialsAndTextures  

// ---------------------------------------------------------------------------------------------------- 
// 
//  Example Surfaces
// 
// ----------------------------------------------------------------------------------------------------   

module Surfaces =

// ----------------------------------------------------------------------------------------------------
// GEOMETRY
// ----------------------------------------------------------------------------------------------------
    let CORPUS (aContour) = 
        Corpus(
            name="CORPUS",
            contour=aContour,
            height=2.0f,
            colorBottom=Color.White,
            colorTop=Color.White,
            colorSide=Color.White
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

    // ----------------------------------------------------------------------------------------------------
    // SURFACE
    // ----------------------------------------------------------------------------------------------------
    let PART_PLANE(name, ursprung:Vector3, idx:int, length, height, mat:Material, text:Texture) = 
        let origin = Vector3 (ursprung.X + float32 idx * (length + 0.5f), ursprung.Y, ursprung.Z)
        new Part(
            name= name,
            shape = Rechteck.InXYPlane(name, origin, false, length, height, Representation.Plane), 
            material=mat,
            texture=text
        )

    let PART_CUBE(name, ursprung, seite, color:Color, mat:Material, text:Texture) = 
        new Part(
            name,
            shape =Quader(name, seite, seite, seite, color),
            material=mat,
            texture=text
        )

    let PART_QUADER(name, length, width, height, color:Color, mat:Material, text:Texture) = 
        new Part(
            name,
            shape =Quader(name, length, height, width, color),
            material=mat,
            texture=text
        )

    let PART_QUADER_MINMAX(name:string, min, max, color:Color, mat:Material, text:Texture) = 
        new Part(
            name,
            shape =Quader(name, min, max, color), 
            material=mat,
            texture=text
        )

    let PART_KUGEL(name, radius, color:Color, mat:Material, text:Texture)  =
        new Part(
            name,
            shape =new Kugel(name, radius, color),  
            material=mat,
            texture=text
        ) 
    
// ---------------------------------------------------------------------------------------------------- 
//  Kugel
// ----------------------------------------------------------------------------------------------------   
module SphereSurfaces =  
    let PART_SPHERE1 =
        new Part(
            "sphere1",
            shape =new Kugel("SPHERE1", 1.5f,  Color.DimGray),  
            material=MAT_RED
        ) 
 
    let PART_SPHERE2 =
        new Part(
            "sphere2",
            shape = new Kugel("SPHERE2", 1.5f, Color.DimGray),
            material =
                new Material(
                    name = "MAT_2",
                    ambient = Color4(0.2f),
                    diffuse = DIFFUSE_LIGHT,
                    specular = Color4.White,
                    specularPower = 20.0f,
                    emissive = Color.Green.ToColor4()
                ),
            visibility = Visibility.Transparent
        )

    let PART_SPHERE3 =
        new Part("sphere3", shape = new Kugel("SPHERE3", 1.5f, Color.DimGray), material = MAT_BLUE)  
        
// ---------------------------------------------------------------------------------------------------- 
//  Pyramide
// ----------------------------------------------------------------------------------------------------   
module PyramidSurfaces = 

    let PYRAMID_SHAPE(name) =
        new Pyramide( 
                name = "PYRAMID" + name,
                origin =  Vector3.Zero,
                seitenLaenge = 3.0f,
                hoehe = 4.0f,
                colorFront = Color.Red,
                colorRight = Color.Green,
                colorBack = Color.Blue,
                colorLeft = Color.Yellow,
                colorBasis = Color.Orange
            ) 

// ---------------------------------------------------------------------------------------------------- 
//  Quader
//  Achtung. 2 verschiedene Materials dürfen nicht den gleichen Namen haben
//           wegen Texture
// ----------------------------------------------------------------------------------------------------   
module AdobeSurfaces = 

    let PART_ADOBE1 =
        new Part(
            "ADOBE1",
            shape =new Quader("ADOBE1", 3.0f, 4.0f, 4.0f, Color.Brown),
            material = MAT_GREEN,
            texture  = TEXT_HILL
        ) 
 
    let PART_ADOBE2 =
        new Part(
            "ADOBE2",
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
            material = MAT_RED,
            visibility = Visibility.Transparent
        )  

    let PART_ADOBE3 =
        new Part(
            "ADOBE3",
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
            material = MAT_BLUE
        )

// ----------------------------------------------------------------------------------------------------  
//  Korpus test
// ----------------------------------------------------------------------------------------------------   
module KorpusSurfaces = 

    //  Im Uhrteigersinn unten
    let CONTOUR =   
        [|
            Vector3(0.0f, 0.0f, -5.0f);
            Vector3(1.0f, 0.0f, -5.0f);
            Vector3(2.0f, 0.0f, -5.0f);
            Vector3(3.0f, 0.0f, -5.0f);
            Vector3(4.0f, 0.0f, -4.0f);
            Vector3(4.0f, 0.0f, -3.0f);
            Vector3(4.0f, 0.0f, -2.0f);
            Vector3(3.0f, 0.0f, -1.0f);
            Vector3(2.0f, 0.0f, -1.0f);
            Vector3(1.0f, 0.0f, -1.0f);
            Vector3(0.0f, 0.0f, -1.0f);
            Vector3(-1.0f, 0.0f, -2.0f);
            Vector3(-1.0f, 0.0f, -3.0f);
            Vector3(-1.0f, 0.0f, -4.0f);
            Vector3(0.0f, 0.0f, -5.0f) ;
        |] 

    let CORPUS =
        Corpus(
            name = "CORPUS",
            contour = CONTOUR,
            height = 2.0f,
            colorBottom = Color.White,
            colorTop = Color.White,
            colorSide = Color.White
        )

// ----------------------------------------------------------------------------------------------------  
//  Tesselated objects test
// ----------------------------------------------------------------------------------------------------   
module GroundSurfaces = 

    let PART_FRONT =
        new Part(
            name = "FRONT",
            shape =
                Fläche.InXYPlane(
                    name = "FRONT",
                    origin = Vector3.Zero,
                    seitenlaenge = 10.0f,
                    normal = Vector3.BackwardLH,
                    color = Color.Transparent
                ),
            material = MAT_DSGRAY,
            visibility = Visibility.Transparent
        )

    let PART_GROUND (origin, extent) =
        new Part(
            name = "GROUND",
            shape =
                  Fläche.InXZPlane(
                      name = "GROUND",
                      origin = origin,
                      seitenlaenge = extent,
                      normal = Vector3.Up,
                      color = Color.Transparent
                  ) ,
            material = MAT_GROUND,
            texture = TEXT_GROUND
        )

    let PART_RIGHT =
        new Part(
            name = "RIGHT",
            shape =
                  Fläche.InYZPlane(
                      name = "RIGHT",
                      origin = Vector3.Zero,
                      seitenlaenge = 10.0f,
                      normal = Vector3.Right,
                      color = Color.Transparent
                  ) ,
            material = MAT_BLUE
        ) 
    
// ----------------------------------------------------------------------------------------------------  
//  2D - Objekte, z.B. Formen, Schrift (in Entwicklung)
// ----------------------------------------------------------------------------------------------------   
module TwoDObjectSurfaces = 
    let PART_FRONT =
        new Part(
            name = "FRONT",
            shape = Quadrat.InXYPlane(name = "FRONT", origin =  Vector3.Zero, seitenlaenge = 5.0f, color = Color.White),
            material = MAT_DGROD
        )

    let PART_LEFT =
        new Part(
            name = "LEFT",
            shape = Quadrat.InYZPlane(name = "LEFT", origin =  Vector3.Zero, seitenlaenge = 5.0f, color = Color.White),
            material = MAT_DGROD
        )

    let PART_BOTTOM =
        new Part(
            name = "BOTTOM",
            shape = Quadrat.InXZPlane(name = "BOTTOM", origin =  Vector3.Zero, seitenlaenge = 5.0f, color = Color.White),
            material = MAT_DGROD
        )

    let PART_RIGHT =
        new Part(
            name = "RIGHT",
            shape = Quadrat.InYZPlane(name = "RIGHT", origin =  Vector3.Zero, seitenlaenge = 5.0f, color = Color.White),
            material = MAT_DGROD
        )

    let PART_TOP =
        new Part(
            name = "TOP",
            shape = Quadrat.InXZPlane(name = "TOP", origin =  Vector3.Zero, seitenlaenge = 5.0f, color = Color.White),
            material = MAT_DGROD
        )

    let PART_LIMIT (name, height, width, color: Color) =
        new Part(
            name = "LIMIT",
            shape = new Quader(name, 2.0f, height, width, Color.Transparent),
            material = MATERIAL_LIMIT(name, color),
            visibility = Visibility.Transparent
        )

    let PART_GROUND (name, length, width, height, color: Color) =
        new Part(
            name = "GROUND",
            shape = new Quader(name, length, height, width, Color.Transparent),
            material = MAT_GROUND,
            visibility = Visibility.Opaque
        )