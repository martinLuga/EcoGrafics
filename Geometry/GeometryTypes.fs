namespace Geometry
//
//  GeometryTypes.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open DirectX.VertexDefs

// ----------------------------------------------------------------------------------------------------
// Geometrie Typen 
// ----------------------------------------------------------------------------------------------------
module GeometryTypes =

    // ----------------------------------------------------------------------------------------------------
    // Quadrat: 4 Vertex  
    // ----------------------------------------------------------------------------------------------------
    type SquareType   = {SV1: Vertex ; SV2: Vertex ; SV3: Vertex; SV4: Vertex}
    type SquareIndexType   = {IV1: int ; IV2: int ; IV3: int; IV4: int}

    type Squaridxtyp = int * int * int * int

    // Quadrat in 4 Vertexe zerlegen
    let squareVerticesClockwise (sq:SquareType) = 
        let {SV1 = sq1; SV2 = sq2; SV3 = sq3; SV4 = sq4 } = sq
        [sq1; sq2; sq3; sq4]
    // 6 Indexe im Uhrzeigersinn erzeugen
    let squareIndicesClockwise (si:SquareIndexType) = 
        let {IV1=si1; IV2=si2; IV3=si3; IV4=si4} = si
        [si1; si2; si4; si2; si3; si4]
   // 6 Indexe im Gegenuhrzeigersinn erzeugen
    let squareIndicesCounterClockwise (si:SquareIndexType) = 
        let {IV1=si1; IV2=si2; IV3=si3; IV4=si4} = si
        [si1; si4; si2; si2; si4; si3]

    // ----------------------------------------------------------------------------------------------------
    // Dreieck: 3 Vertex      
    // ----------------------------------------------------------------------------------------------------
    type TriangleType = {TV1: Vertex ; TV2: Vertex ; TV3: Vertex}
    type TriangleIndexType   = {ITV1: int ; ITV2: int ; ITV3: int}
    // Dreieck in Vertexe zerlegen
    let triangleVertices (tr:TriangleType) = 
        let {TV1 = tr1; TV2 = tr2; TV3 = tr3} = tr
        [tr1; tr2; tr3]
    let triangleIndicesClockwise (ti:TriangleIndexType) = 
        let {ITV1=ti1; ITV2=ti2; ITV3=ti3 } = ti
        [ti1; ti2; ti3]
    let triangleIndicesCounterClockwise (ti:TriangleIndexType) = 
        let {ITV1=ti1; ITV2=ti2; ITV3=ti3 } = ti
        [ti1; ti3; ti2]

    // ----------------------------------------------------------------------------------------------------
    // Würfel: 6 Seiten
    // ----------------------------------------------------------------------------------------------------
    type CubeType       = {FRONT: SquareType ; RIGHT: SquareType ; BACK: SquareType; LEFT: SquareType; TOP: SquareType; BOTTOM: SquareType}
    type CubeIndexType  = {IFRONT: SquareIndexType ; IRIGHT: SquareIndexType ; IBACK: SquareIndexType; ILEFT: SquareIndexType; ITOP: SquareIndexType; IBOTTOM: SquareIndexType}
    // Würfel in Quadrate zerlegen
    let deconstructCube (cube:CubeType) = 
        let {FRONT = front; RIGHT = right; BACK = back; LEFT = left; TOP = top; BOTTOM = bottom} = cube
        [front; right; back; left; top; bottom]
    let deconstructCubeIndex (cubeIndex:CubeIndexType) = 
        let {IFRONT = ifront; IRIGHT = iright; IBACK = iback; ILEFT = ileft; ITOP = itop; IBOTTOM = ibottom} = cubeIndex
        [ifront; iright; iback; ileft; itop; ibottom]

    // ----------------------------------------------------------------------------------------------------
    // Pyramide: Quadratische Grundfläche, 4 Dreiecke
    // ----------------------------------------------------------------------------------------------------
    type PyramidType       = {BASE: SquareType ; FRONT: TriangleType ; RIGHT: TriangleType; BACK: TriangleType; LEFT: TriangleType }
    type PyramidIndexType  = {IBASE: SquareIndexType ; IFRONT: TriangleIndexType ; IRIGHT: TriangleIndexType; IBACK: TriangleIndexType; ILEFT: TriangleIndexType}
    let deconstructPyramid (pyramid:PyramidType) = 
        let {BASE = basis; FRONT = front; RIGHT = right; BACK = back; LEFT = left} = pyramid
        let result = 
            triangleVertices left            
             |> List.append(triangleVertices back)
             |> List.append(triangleVertices right)
             |> List.append(triangleVertices front)  
             |> List.append(squareVerticesClockwise basis)
        result

    let deconstructPyramidIndex (iPyramid:PyramidIndexType) = 
        let {IBASE = ibase; IFRONT = ifront; IRIGHT= iright; IBACK = iback; ILEFT = ileft} = iPyramid
        let result = 
            triangleIndicesClockwise ileft
             |> List.append(triangleIndicesClockwise iback)
             |> List.append(triangleIndicesClockwise iright)
             |> List.append(triangleIndicesClockwise ifront)  
             |> List.append(squareIndicesClockwise ibase)
        result

