namespace Geometry
//
//  GeometryTypes.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX

open Base.VertexDefs 

// ----------------------------------------------------------------------------------------------------
//  Geometrie Typen 
// ----------------------------------------------------------------------------------------------------
module GeometricTypes = 

    // ----------------------------------------------------------------------------------------------------
    //  Quadrat: 4 Vertex  
    // ----------------------------------------------------------------------------------------------------
    type SquareType         = {SV1: Vertex ; SV2: Vertex ; SV3: Vertex; SV4: Vertex}
    type SquareIndexType    = {SI1: int ; SI2: int ; SI3: int; SI4: int}
    type SquareTextureType  = {SUV1: Vector2 ; SUV2: Vector2 ; SUV3: Vector2}

    // Quadrat in 4 Vertexe zerlegen
    let squareVerticesClockwise (sq:SquareType) = 
        let {SV1 = sq1; SV2 = sq2; SV3 = sq3; SV4 = sq4 } = sq
        [sq1; sq2; sq3; sq4]

    // 6 Indexe (2 Dreiecke) im Uhrzeigersinn erzeugen
    let squareIndicesClockwise (si:SquareIndexType) = 
        let {SI1=si1; SI2=si2; SI3=si3; SI4=si4} = si
        [si1; si2; si4; si2; si3; si4]

   // 6 Indexe (2 Dreiecke) im Gegenuhrzeigersinn erzeugen
    let squareIndicesCounterClockwise (si:SquareIndexType) = 
        let {SI1=si1; SI2=si2; SI3=si3; SI4=si4} = si
        [si1; si4; si2; si2; si4; si3]

    // ----------------------------------------------------------------------------------------------------
    //  Dreieck: 3 Vertex      
    // ----------------------------------------------------------------------------------------------------
    type TriangleType           = {TV1: Vertex ; TV2: Vertex ; TV3: Vertex}
    type TriangleIndexType      = {ITV1: int ; ITV2: int ; ITV3: int}
    type TriangleTextureType    = {TUV1: Vector2 ; TUV2: Vector2 ; TUV3: Vector2}

    let defaultTriangleTexture factor =         
        {TUV1=new Vector2(0.0f, 0.0f); TUV2=new Vector2(factor, 0.0f); TUV3=new Vector2(factor, 1.0f)}
    
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

    let deconstructTriangleTexture (uvi:TriangleTextureType) = 
        let {TUV1=uvi1; TUV2=uvi2; TUV3=uvi3} = uvi
        [uvi1; uvi2; uvi3]

    // Quadrat in Dreiecke zerlegen
    let deconstructSquare (square:SquareType) = 
        let {SV1=sv1; SV2 = sv2; SV3 = sv3; SV4=sv4} = square
        {TV1=sv1; TV2=sv2; TV3=sv3}, {TV1=sv3; TV2=sv4; TV3=sv1}

    // ----------------------------------------------------------------------------------------------------
    //  Würfel: 6 Seiten
    // ----------------------------------------------------------------------------------------------------
    type CubeType       = {FRONT:  SquareType ;      RIGHT: SquareType;       BACK: SquareType; LEFT: SquareType; TOP: SquareType; BOTTOM: SquareType}
    type CubeIndexType  = {IFRONT: SquareIndexType ; IRIGHT: SquareIndexType; IBACK: SquareIndexType; ILEFT: SquareIndexType; ITOP: SquareIndexType; IBOTTOM: SquareIndexType}
    
    // Würfel in Quadrate zerlegen
    let deconstructCube (cube:CubeType) = 
        let {FRONT = front; RIGHT = right; BACK = back; LEFT = left; TOP = top; BOTTOM = bottom} = cube
        [front; right; back; left; top; bottom]

    let deconstructCubeIndex (cubeIndex:CubeIndexType) = 
        let {IFRONT = ifront; IRIGHT = iright; IBACK = iback; ILEFT = ileft; ITOP = itop; IBOTTOM = ibottom} = cubeIndex
        [ifront; iright; iback; ileft; itop; ibottom]

    // ----------------------------------------------------------------------------------------------------
    //  Pyramide: Quadratische Grundfläche, 4 Dreiecke
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

    // ----------------------------------------------------------------------------------------------------
    //  Quadrat: 4 Ecken
    // ----------------------------------------------------------------------------------------------------
    type QuadType = {QV1: Vertex ; QV2: Vertex ; QV3: Vertex; QV4: Vertex}
    type QuadIndexType   = {QI1: int ; QI2: int ; QI3: int; QI4: int}
    type QuadTextureType   = {UV1: Vector2 ; UV2: Vector2 ; UV3: Vector2; UV4: Vector2}
    
    let defaultQuadTexture factor =         
        {UV1=new Vector2(0.0f, 0.0f); UV2=new Vector2(factor, 0.0f); UV3=new Vector2(factor, 1.0f);UV4=new Vector2(0.0f, factor)}

    let deconstructQuad (sq:QuadType) = 
        let {QV1 = sq1; QV2 = sq2; QV3 = sq3; QV4 = sq4 } = sq
        [sq1; sq2; sq3; sq4]

    let deconstructQuadIndex (qi:QuadIndexType) = 
        let {QI1=qi1; QI2=qi2; QI3=qi3; QI4=qi4} = qi
        [qi1; qi2; qi3; qi4]

    let deconstructQuadTexture (uvi:QuadTextureType) = 
        let {UV1=uvi1; UV2=uvi2; UV3=uvi3; UV4=uvi4 } = uvi
        [uvi1; uvi2; uvi3; uvi4]

    // ----------------------------------------------------------------------------------------------------
    // Polygon: N Ecken  
    // ----------------------------------------------------------------------------------------------------
    type PolyType = {TV: Vertex[]}
    type PolyIndexType = {ITV: int[]}
    let deconstructPolygon(tr:PolyType) = 
        tr.TV 
    let deconstructPolygonIndex (ti:PolyIndexType) = 
        ti.ITV

    // Die Normale als Kreuzprodukt von 2 Vektoren 
    // Bei 3 Punkten
    // Negativ, weil kreuzprodukt RH ist
    // wir hier aber immer LH arbeiten
    let createNormal p1 p2 p3 =   
        let u = p3 - p1
        let v = p3 - p2
        let norm = Vector3.Cross(u,v)
        norm.Normalize()
        -norm

    // ----------------------------------------------------------------------------------------------------
    // Die 4 Ecken des Quadrats, müssen in Reihenfolge im Uhrzeiger kommen
    // Indices zeigen auf Vertexe für 2 Dreiecke jeweils im Uhrzeigersinn
    // ----------------------------------------------------------------------------------------------------
    let square p1 p2 p3 p4 normal (color:Color) idx isTransparent =
        //  P4 (x,y,z +l) ------ P3 (x+l, y, z+l)
        //   |  \                |
        //   |    \              |
        //   |      \            |
        //   |        \          |
        //   |          \        |
        //   |            \      |
        //   |              \    |
        //   |                \  |
        //  P1 (x,y,z) --------- P2 (x+l, y, z)
        let grade = 2.5f
        let mutable colr4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()
        let v1 = createVertex p1 normal colr4  (new Vector2(0.0f, 0.0f))  
        let v2 = createVertex p2 normal colr4  (new Vector2(grade, 0.0f))  
        let v3 = createVertex p3 normal colr4  (new Vector2(grade, grade))  
        let v4 = createVertex p4 normal colr4  (new Vector2(0.0f, grade))  
        let vert = {SV1 = v1; SV2 = v2; SV3 = v3; SV4 = v4}
        let ind =  {SI1 = idx + 0; SI2 = idx + 1; SI3 = idx + 2; SI4 = idx + 3}
        (vert, ind) 

    // ----------------------------------------------------------------------------------------------------
    // Das Dreieck zu den 3 Punkten, Reihenfolge im Uhrzeiger 
    // ----------------------------------------------------------------------------------------------------
    let triangle p1 p2 p3 normal (color:Color) idx isTransparent =     
        let mutable colr4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()
        let v1 = createVertex p1 normal colr4 (new Vector2(0.0f, 0.0f)) 
        let v2 = createVertex p2 normal colr4 (new Vector2(1.0f, 0.0f))  
        let v3 = createVertex p3 normal colr4 (new Vector2(1.0f, 1.0f))  
        let vert = {TV1 = v1; TV2 = v2; TV3 = v3}
        let ind =  {ITV1 = idx + 0; ITV2 = idx + 1; ITV3 = idx + 2 }
        (vert, ind)