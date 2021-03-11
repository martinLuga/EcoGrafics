namespace Geometry
//
//  GeometryTypes.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX

open DirectX.VertexDefs

/// <summary>
/// Geometrie Typen 
/// </summary>
module GeometryTypes =

    /// <summary>
    /// Quadrat: 4 Vertex  
    /// </summary>
    type SquareType   = {SV1: Vertex ; SV2: Vertex ; SV3: Vertex; SV4: Vertex}
    type SquareIndexType   = {SI1: int ; SI2: int ; SI3: int; SI4: int}

    // Quadrat in 4 Vertexe zerlegen
    let squareVerticesClockwise (sq:SquareType) = 
        let {SV1 = sq1; SV2 = sq2; SV3 = sq3; SV4 = sq4 } = sq
        [sq1; sq2; sq3; sq4]

    // 6 Indexe im Uhrzeigersinn erzeugen
    let squareIndicesClockwise (si:SquareIndexType) = 
        let {SI1=si1; SI2=si2; SI3=si3; SI4=si4} = si
        [si1; si2; si4; si2; si3; si4]
   // 6 Indexe im Gegenuhrzeigersinn erzeugen

    let squareIndicesCounterClockwise (si:SquareIndexType) = 
        let {SI1=si1; SI2=si2; SI3=si3; SI4=si4} = si
        [si1; si4; si2; si2; si4; si3]

    /// <summary>
    /// Dreieck: 3 Vertex      
    /// </summary>
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

    /// <summary>
    /// Würfel: 6 Seiten
    /// </summary>
    type CubeType       = {FRONT: SquareType ; RIGHT: SquareType ; BACK: SquareType; LEFT: SquareType; TOP: SquareType; BOTTOM: SquareType}
    type CubeIndexType  = {IFRONT: SquareIndexType ; IRIGHT: SquareIndexType ; IBACK: SquareIndexType; ILEFT: SquareIndexType; ITOP: SquareIndexType; IBOTTOM: SquareIndexType}
    
    // Würfel in Quadrate zerlegen
    let deconstructCube (cube:CubeType) = 
        let {FRONT = front; RIGHT = right; BACK = back; LEFT = left; TOP = top; BOTTOM = bottom} = cube
        [front; right; back; left; top; bottom]

    let deconstructCubeIndex (cubeIndex:CubeIndexType) = 
        let {IFRONT = ifront; IRIGHT = iright; IBACK = iback; ILEFT = ileft; ITOP = itop; IBOTTOM = ibottom} = cubeIndex
        [ifront; iright; iback; ileft; itop; ibottom]

    /// <summary>
    /// Pyramide: Quadratische Grundfläche, 4 Dreiecke
    /// </summary>
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

    // Quadrat: 4 Ecken  
    type QuadType = {QV1: Vertex ; QV2: Vertex ; QV3: Vertex; QV4: Vertex}
    type QuadIndexType   = {QI1: int ; QI2: int ; QI3: int; QI4: int}
    type QuadTextureType   = {UV1: Vector2 ; UV2: Vector2 ; UV3: Vector2; UV4: Vector2}

    let deconstructQuad (sq:QuadType) = 
        let {QV1 = sq1; QV2 = sq2; QV3 = sq3; QV4 = sq4 } = sq
        [sq1; sq2; sq3; sq4]

    let deconstructQuadIndex (qi:QuadIndexType) = 
        let {QI1=qi1; QI2=qi2; QI3=qi3; QI4=qi4} = qi
        [qi1; qi2; qi3; qi4]

    let deconstructQuadTexture (uvi:QuadTextureType) = 
        let {UV1=uvi1; UV2=uvi2; UV3=uvi3; UV4=uvi4 } = uvi
        [uvi1; uvi2; uvi3; uvi4]

    // Polygon: N Ecken  
    type PolyType = {TV: Vertex[]}
    type PolyIndexType = {ITV: int[]}
    let deconstructPolygon(tr:PolyType) = 
        tr.TV 
    let deconstructPolygonIndex (ti:PolyIndexType) = 
        ti.ITV