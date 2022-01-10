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

open GeometricTypes 

// ----------------------------------------------------------------------------------------------------
// Vertexe für einen Quader erzeugen
// Alle Seiten achsenparallel - Drehungen erfolgen über World-Transformationen
// ----------------------------------------------------------------------------------------------------
module VertexPrisma =

    let normalFront  =   Vector3.UnitZ      // Front  
    let normalBack   = - Vector3.UnitZ      // Back     
    let normalTop    =   Vector3.UnitY      // Top       
    let normalBottom = - Vector3.UnitY      // Bottom  
    let normalLeft   = - Vector3.UnitX      // Left       
    let normalRight  =   Vector3.UnitX      // Right 

    // Alle 6 Seiten bilden den Quader
    // 1,2,3,4 sind die Front
    // 5,6,7,8 sind Backside
    let prisma (ursprung:Vector3) laenge hoehe breite colorFront colorRight colorBack colorLeft colorTop colorBottom isTransparent = 
        
        // Front im Gegenuhrzeigersinn
        let p1 = new Vector3(ursprung.X,        ursprung.Y,         ursprung.Z)
        let p4 = new Vector3(ursprung.X+laenge, ursprung.Y        , ursprung.Z)

        // Back im Uhrzeigersinn
        let p5 = new Vector3(ursprung.X+laenge, ursprung.Y,         ursprung.Z+breite)
        let p6 = new Vector3(ursprung.X+laenge, ursprung.Y+hoehe,   ursprung.Z+breite)
        let p7 = new Vector3(ursprung.X,        ursprung.Y+hoehe,   ursprung.Z+breite)
        let p8 = new Vector3(ursprung.X,        ursprung.Y      ,   ursprung.Z+breite)

        let triangleRight, triangleIndexRight  = triangle  p4 p6 p5 normalRight colorRight 0 isTransparent
        let squareBack,    squareIndexBack     = square p5 p6 p7 p8 normalBack colorBack 3 isTransparent
        let triangleLeft,  triangleIndexLeft   = triangle  p8  p7 p1 normalLeft colorLeft 7 isTransparent
        let squareTop,     squareIndexTop      = square p1 p7 p6 p4 normalTop colorTop 10 isTransparent
        let squareBot,     squareIndexBot      = square p1 p4 p5 p8 normalBottom colorBottom 14 isTransparent

        let Prisma        = {PRIGHT  = triangleRight ;         PBACK = squareBack;        PLEFT = triangleLeft;       PTOP  = squareTop;        PBOTTOM  = squareBot }
        let PrismaIndex   = {IPRIGHT = triangleIndexRight ;    IPBACK = squareIndexBack ; IPLEFT = triangleIndexLeft; IPTOP = squareIndexTop;   IPBOTTOM = squareIndexBot }
        (Prisma, PrismaIndex)   

    let prismaVerticesAndIndices (ursprung:Vector3) laenge hoehe breite (colorFront:Color) (colorRight:Color) (colorBack:Color) (colorLeft:Color) (colorTop:Color) (colorBottom:Color) isTransparent =
        
        let (Prisma, PrismaIndex) = prisma ursprung laenge hoehe breite colorFront colorRight colorBack colorLeft colorTop colorBottom isTransparent
      
        let (right, back, left, top, bottom) = deconstructPrisma Prisma        
        let vertexList  =
            [
                triangleVertices right;
                squareVerticesClockwise back;
                triangleVertices left;
                squareVerticesClockwise top;
                squareVerticesClockwise bottom 
            ]
            |> List.concat 

        let (iright, iback, ileft, itop, ibottom) = deconstructPrismaIndex PrismaIndex
        let indexList = 
            [            
                triangleIndicesClockwise iright;
                squareIndicesClockwise iback;
                triangleIndicesClockwise ileft;
                squareIndicesClockwise itop;
                squareIndicesClockwise ibottom 
            ]
            |> List.concat  

        let vertices = vertexList |> Array.ofSeq 
        let indices  = indexList |> Array.ofSeq 
        
        vertices, indices 

    let prismaVerticesUni (ursprung:Vector3) seite hoehe breite (color:Color) isTransparent =
        prismaVerticesAndIndices ursprung seite hoehe breite color color color color color color isTransparent
    
    let CreateMeshData(ursprung:Vector3,laenge:float32, hoehe:float32, breite:float32, colorFront:Color, colorRight:Color, colorBack:Color, colorLeft:Color, colorTop:Color, colorBottom:Color, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        new MeshData(   
            prismaVerticesAndIndices ursprung laenge hoehe breite colorFront colorRight colorBack colorLeft colorTop colorBottom isTransparent
        )  

