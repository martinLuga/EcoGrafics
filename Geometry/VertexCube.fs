namespace Geometry
//
//  VertexCube.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX

open DirectX.MeshObjects

open GeometryUtils
open GeometryTypes

// ----------------------------------------------------------------------------------------------------
// Vertexe für einen Quader erzeugen
// Alle Seiten achsenparallel - Drehungen erfolgen über World-Transformationen
// ----------------------------------------------------------------------------------------------------
module VertexCube =

    let normalFront  = - Vector3.UnitZ      // Front  
    let normalBack   =   Vector3.UnitZ      // Back     
    let normalTop    =   Vector3.UnitY      // Top       
    let normalBottom = - Vector3.UnitY      // Bottom  
    let normalLeft   = - Vector3.UnitX      // Left       
    let normalRight  =   Vector3.UnitX      // Right 

    // Alle 6 Seiten bilden den Quader
    // 1,2,3,4 sind die Front
    // 5,6,7,8 sind Backside
    let cube (ursprung:Vector3) laenge hoehe breite colorFront colorRight colorBack colorLeft colorTop colorBottom isTransparent = 
        
        // Front im Gegenuhrzeigersinn
        let p1 = new Vector3(ursprung.X,        ursprung.Y,         ursprung.Z)
        let p2 = new Vector3(ursprung.X,        ursprung.Y + hoehe, ursprung.Z)
        let p3 = new Vector3(ursprung.X+laenge, ursprung.Y + hoehe, ursprung.Z)
        let p4 = new Vector3(ursprung.X+laenge, ursprung.Y        , ursprung.Z)

        // Back im Uhrzeigersinn
        let p5 = new Vector3(ursprung.X+laenge, ursprung.Y,         ursprung.Z+breite)
        let p6 = new Vector3(ursprung.X+laenge, ursprung.Y+hoehe,   ursprung.Z+breite)
        let p7 = new Vector3(ursprung.X,        ursprung.Y+hoehe,   ursprung.Z+breite)
        let p8 = new Vector3(ursprung.X,        ursprung.Y      ,   ursprung.Z+breite)

        let frontv, fronti  = square p1 p2 p3 p4 normalFront colorFront 0 isTransparent
        let rightv, righti  = square p4 p3 p6 p5 normalRight colorRight 4 isTransparent
        let backv, backi    = square p5 p6 p7 p8 normalBack colorBack 8 isTransparent
        let leftv, lefti    = square p8 p7 p2 p1 normalLeft colorLeft 12 isTransparent
        let topv, topi      = square p2 p7 p6 p3 normalTop colorTop 16 isTransparent
        let botv,boti       = square p1 p4 p5 p8 normalBottom colorBottom 20 isTransparent

        let vert = {FRONT = frontv ; RIGHT = rightv ; BACK = backv ; LEFT = leftv ; TOP = topv ; BOTTOM = botv }
        let ind = {IFRONT = fronti ; IRIGHT = righti ; IBACK = backi ; ILEFT = lefti ; ITOP = topi ; IBOTTOM = boti }
        (vert, ind)   

    let cubeVertices (ursprung:Vector3) laenge hoehe breite (colorFront:Color) (colorRight:Color) (colorBack:Color) (colorLeft:Color) (colorTop:Color) (colorBottom:Color) topology isTransparent =
        
        let (Cube, CubeIndex) = cube ursprung laenge hoehe breite colorFront colorRight colorBack colorLeft colorTop colorBottom isTransparent
      
        let squareList = deconstructCube Cube      
        let squareIndexList = deconstructCubeIndex CubeIndex

        let vertexList =  List.collect (fun x -> squareVerticesClockwise x) squareList
        let indexList = List.collect (fun x -> squareIndicesClockwise x) squareIndexList
        
        let vertices = vertexList |> Array.ofSeq 
        let indices = indexList |> Array.ofSeq 
        
        new MeshData(vertices, indices, topology)

    let cubeVerticesUni (ursprung:Vector3) seite hoehe breite (color:Color) topology isTransparent =
        cubeVertices ursprung seite hoehe breite color color color color color color topology isTransparent