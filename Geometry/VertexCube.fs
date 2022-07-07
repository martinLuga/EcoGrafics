namespace Geometry
//
//  VertexCube.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX 

open Base.MeshObjects 
open Base.ModelSupport
open Base.VertexDefs

open GeometricTypes 

// ----------------------------------------------------------------------------------------------------
// Vertexe für einen achsenparallelen Quader erzeugen
// ----------------------------------------------------------------------------------------------------
module VertexCube =

    let normalFront  =   Vector3.UnitZ      // Front  
    let normalBack   = - Vector3.UnitZ      // Back     
    let normalTop    =   Vector3.UnitY      // Top       
    let normalBottom = - Vector3.UnitY      // Bottom  
    let normalLeft   = - Vector3.UnitX      // Left       
    let normalRight  =   Vector3.UnitX      // Right 

    // Alle 6 Seiten bilden den Quader
    // 1,2,3,4 sind die Front
    // 5,6,7,8 sind Backside
    let cube (ursprung:Vector3) laenge hoehe breite colorFront colorRight colorBack colorLeft colorTop colorBottom isTransparent = 
        
        // Front im Uhrzeigersinn
        let p1 = new Vector3(ursprung.X,        ursprung.Y,         ursprung.Z)
        let p2 = new Vector3(ursprung.X,        ursprung.Y + hoehe, ursprung.Z)
        let p3 = new Vector3(ursprung.X+laenge, ursprung.Y + hoehe, ursprung.Z)
        let p4 = new Vector3(ursprung.X+laenge, ursprung.Y        , ursprung.Z)

        // Back im Uhrzeigersinn
        let p5 = new Vector3(ursprung.X+laenge, ursprung.Y,         ursprung.Z+breite)
        let p6 = new Vector3(ursprung.X+laenge, ursprung.Y+hoehe,   ursprung.Z+breite)
        let p7 = new Vector3(ursprung.X,        ursprung.Y+hoehe,   ursprung.Z+breite)
        let p8 = new Vector3(ursprung.X,        ursprung.Y      ,   ursprung.Z+breite)

        let squareFront, squareIndexFront  = square p1 p2 p3 p4 normalFront colorFront 0 isTransparent
        let squareRight, squareIndexRight  = square p4 p3 p6 p5 normalRight colorRight 4 isTransparent
        let squareBack,  squareIndexBack   = square p5 p6 p7 p8 normalBack colorBack 8 isTransparent
        let squareLeft,  squareIndexLeft   = square p8 p7 p2 p1 normalLeft colorLeft 12 isTransparent
        let squareTop,   squareIndexTop    = square p2 p7 p6 p3 normalTop colorTop 16 isTransparent
        let squareBot,   squareIndexBot    = square p1 p4 p5 p8 normalBottom colorBottom 20 isTransparent

        let Cube        = {FRONT  = squareFront ;      RIGHT  = squareRight ;       BACK = squareBack;        LEFT = squareLeft;       TOP  = squareTop;        BOTTOM  = squareBot }
        let CubeIndex   = {IFRONT = squareIndexFront ; IRIGHT = squareIndexRight ;  IBACK = squareIndexBack ; ILEFT = squareIndexLeft; ITOP = squareIndexTop;   IBOTTOM = squareIndexBot }
        (Cube, CubeIndex)   

    let cubeVerticesAndIndices (ursprung:Vector3) laenge hoehe breite (colorFront:Color) (colorRight:Color) (colorBack:Color) (colorLeft:Color) (colorTop:Color) (colorBottom:Color) isTransparent =
        
        let (Cube, CubeIndex) = cube ursprung laenge hoehe breite colorFront colorRight colorBack colorLeft colorTop colorBottom isTransparent
      
        let squareList = deconstructCube Cube      
        let squareIndexList = deconstructCubeIndex CubeIndex

        let vertexList  = List.collect (fun x -> squareVerticesClockwise x) squareList
        let indexList   = List.collect (fun x -> squareIndicesClockwise x) squareIndexList
        
        let vertices = vertexList |> Array.ofSeq 
        let indices  = indexList |> Array.ofSeq 
        
        vertices, indices 

    let cubeVerticesUni (ursprung:Vector3) seite hoehe breite (color:Color) isTransparent =
        cubeVerticesAndIndices ursprung seite hoehe breite color color color color color color isTransparent