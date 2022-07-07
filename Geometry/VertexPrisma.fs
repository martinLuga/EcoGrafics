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

    // 5 Seiten bilden das Prisma
    // 1,2,3 sind die linke Seite
    // 4,5,6 sind die rechte Seite
    // 1,4,2,5 sind die vordere Seite
    // 1,4,6,3 sind die hintere Seite
    // 3,6,2,5 sind die untere Seite
    let prisma (p1:Vector3, p2:Vector3, p3:Vector3, breite, color, isTransparent) = 
        
        // Linke Seite p1 , p2 , p3
        // Rechte Seite 
        let p4 = new Vector3(p1.X + breite, p1.Y, p1.Z)
        let p5 = new Vector3(p2.X + breite, p2.Y, p2.Z)
        let p6 = new Vector3(p3.X + breite, p3.Y, p3.Z)  
        
        let triangleLeft,  triangleIndexLeft   = triangle  p1 p3 p2 normalLeft      color 0     isTransparent

        let triangleRight, triangleIndexRight  = triangle  p5 p6 p4 normalRight     color 3     isTransparent
        
        // Front im Uhrzeigersinn
        let squareFront,   squareIndexFront    = square p1 p2 p5 p4 normalTop       color 6     isTransparent

        // Front im GegenUhrzeigersinn
        let squareBack,    squareIndexBack     = square p4 p6 p3 p3 normalBack      color 10    isTransparent

        let squareBot,     squareIndexBot      = square p3 p6 p5 p2 normalBottom    color 14    isTransparent

        let Prisma        = {PRIGHT  = triangleRight ;         PBACK = squareBack;        PLEFT = triangleLeft;       PTOP = squareFront;        PBOTTOM  = squareBot }
        let PrismaIndex   = {IPRIGHT = triangleIndexRight;    IPBACK = squareIndexBack ; IPLEFT = triangleIndexLeft; IPTOP = squareIndexFront;   IPBOTTOM = squareIndexBot }
        (Prisma, PrismaIndex)   

    let prismaVerticesAndIndices (p1:Vector3, p2:Vector3, p3:Vector3, breite, color:Color, isTransparent) =
        
        let (Prisma, PrismaIndex) = prisma (p1, p2, p3, breite, color, isTransparent)
      
        let (right, back, left, top, bottom) = deconstructPrisma Prisma        
        let vertexList  =
            [
                deconstructTriangle right;
                squareVerticesClockwise back;
                deconstructTriangle left;
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

