//
//  Patch.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//
namespace Geometry

open SharpDX
open SharpDX.Direct3D
open System.Collections.Generic

open DirectX.VertexDefs
open DirectX.MeshObjects

open GeometryUtils
open GeometryTypes

// ----------------------------------------------------------------------------------------------------
// Vertexes erzeugen  
//      Patch
//      Triangle
// STATUS: IN_ARBEIT
// ----------------------------------------------------------------------------------------------------
module VertexPatch =

    // Quadrat: 4 Ecken  
    type QuadType = {QV1: Vertex ; QV2: Vertex ; QV3: Vertex; QV4: Vertex}
    type QuadIndexType   = {QI1: int ; QI2: int ; QI3: int; QI4: int}
    type QuadTextureType   = {UV1: Vector2 ; UV2: Vector2 ; UV3: Vector2; UV4: Vector2}

    let deconstructQuad (sq:QuadType) = 
        let {QV1 = sq1; QV2 = sq2; QV3 = sq3; QV4 = sq4 } = sq
        [sq1; sq2; sq3; sq4]

    let deconstructQuadIndex (si:QuadIndexType) = 
        let {QI1=si1; QI2=si2; QI3=si3; QI4=si4} = si
        [si1; si2; si3; si4]

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

    let defaultTexture factor =         
        {UV1=new Vector2(0.0f, 0.0f); UV2=new Vector2(factor, 0.0f); UV3=new Vector2(factor, 1.0f);UV4=new Vector2(0.0f, factor)}

    // ----------------------------------------------------------------------------------------------------
    // Quadratisches Patch durch 4 Controlpoints   
    // ----------------------------------------------------------------------------------------------------
    let quadVertices p1 p2 p3 p4 (color:Color) idx isTransparent =  
        let text = defaultTexture 1.0f 
        let texti = deconstructQuadTexture text |> Array.ofSeq 
        let v1 = createVertex  p1  -Vector3.UnitY color texti.[0] isTransparent
        let v2 = createVertex  p2  -Vector3.UnitY color texti.[1] isTransparent
        let v3 = createVertex  p3  -Vector3.UnitY color texti.[2] isTransparent 
        let v4 = createVertex  p4  -Vector3.UnitY color texti.[3] isTransparent 
        let vert = {QV1 = v1; QV2 = v2; QV3 = v3; QV4 = v4}  
        let ind =  {IV1 = idx + 0; IV2 = idx + 1; IV3 = idx + 2; IV4 = idx + 3}
        (vert, ind)    
    
    // ---------------------------------------------------------------------------------------------------- 
    // Quadratisches Patch durch Punkt und Seitenlänge 
    // Reihenfolge der Punkte ist der Uhrzeigersinn
    // ----------------------------------------------------------------------------------------------------
    let quadPointsFromSquare p1 quadLength (color:Color) idx  =  
        let p2 = p1 + Vector3.UnitX * quadLength 
        let p3 = p1 + Vector3.UnitX * quadLength + Vector3.UnitZ * quadLength   
        let p4 = p1 + Vector3.UnitZ * quadLength 
        quadVertices p4 p3 p2 p1  color  idx 

    // ----------------------------------------------------------------------------------------------------
    // Erzeugen der Meshdaten für ein quadratisches Patch   
    // ----------------------------------------------------------------------------------------------------
    let quadContext p1 p2 p3 p4 (color:Color) isTransparent = 
        let qv = quadVertices p1 p2 p3 p4 color 0 isTransparent
        let qVertexes, qIndexes = qv   
        let quadVertexList = deconstructQuad qVertexes
        let quadIndexList = deconstructQuadIndex qIndexes
        let vertices = quadVertexList |> Array.ofSeq 
        let indices = quadIndexList |> Array.ofSeq 
        new MeshData(vertices, indices, PrimitiveTopology.PatchListWith4ControlPoints)  

    // ----------------------------------------------------------------------------------------------------
    // Erzeugen der Meshdaten für eine ebene Fläche
    // ----------------------------------------------------------------------------------------------------
    let quadPlaneContext (laenge:float32) (patchLaenge:float32) color  isTransparent =
        let p1 = Vector3.Zero
        let quadVertexList = new List<QuadType>()
        let quadIndexList = new List<QuadIndexType>()
        let mutable ip1 = p1
        for i in 0.0f .. patchLaenge .. laenge - patchLaenge do
            ip1 <- Vector3(0.0f, 0.0f, i)
            let mutable jp1 = ip1
            for j in 0.0f .. patchLaenge .. laenge - patchLaenge do
                jp1 <- Vector3(ip1.X+j, 0.0f, ip1.Z)
                let qv = quadPointsFromSquare jp1 patchLaenge color 0 isTransparent
                let qVertex, qIndex = qv 
                quadVertexList.Add(qVertex)
                quadIndexList.Add(qIndex) 
        
        let vertices = quadVertexList  |> List.ofSeq |> List.collect (fun q -> deconstructQuad q) 

        let vertices = vertices  |> Array.ofSeq 

        let indices = quadIndexList  |> List.ofSeq |> List.collect (fun ind -> deconstructQuadIndex ind)  |> Array.ofSeq 
        new MeshData(vertices, indices, PrimitiveTopology.PatchListWith4ControlPoints)       
        
    // ----------------------------------------------------------------------------------------------------
    // Vertices für ein dreieckiges Patch   
    // Die Ecken werden immer im Uhrzeigersinn angelegt
    // ----------------------------------------------------------------------------------------------------
    let triVertices p1 p2 p3 (color:Color) idx isTransparent= 
        let text = defaultTexture 1.0f 
        let texti = deconstructQuadTexture text |> Array.ofSeq 
        let normal = (createNormal p1 p2 p3) 
        let v1 = createVertex  p1 normal color texti.[0] isTransparent
        let v2 = createVertex  p2 normal color texti.[0] isTransparent
        let v3 = createVertex  p3 normal color texti.[0] isTransparent
        let vert = {TV1 = v1; TV2 = v2; TV3 = v3}   
        let ind =  {ITV1 = idx + 0; ITV2 = idx + 1; ITV3 = idx + 2}
        (vert, ind)  

    // ----------------------------------------------------------------------------------------------------
    // Triangles für eine unregelmäßige Fläche 
    // Die Fläche ist gegeben durch einen Mittelpunkt c und die Punkte die den Rand festlegen
    // ----------------------------------------------------------------------------------------------------        
    let polygonTriangleList (center: Vector3) (point:Vector3[]) (color:Color) isTransparent = 
        let triangleVertexList = new List<TriangleType>()
        let triangleIndexList = new List<TriangleIndexType>()
        for i in 0..point.Length-2 do
            let triangle = triVertices point.[i] point.[i+1] center  color (i*3) isTransparent
            let tVertexes, tIndexes = triangle  
            triangleVertexList.Add(tVertexes)
            triangleIndexList.Add(tIndexes) 
        (triangleVertexList, triangleIndexList)  

    // ----------------------------------------------------------------------------------------------------
    // Triangless für ein Band  
    // Das Band  ist gegeben durch zwei parallele Konturen
    // TODO Die Normalen sind nicht OK
    // ----------------------------------------------------------------------------------------------------        
    let stripeTriangleList (unten: Vector3[]) (oben:Vector3[]) (color:Color) isTransparent = 
        let triangleVertexList = new List<TriangleType>()
        let triangleIndexList = new List<TriangleIndexType>()

        // Untere Dreicke
        for i in 0..unten.Length-2 do
            let triangleUnten = triVertices unten.[i] oben.[i+1] unten.[i+1]  color (i*3) isTransparent
            let tVertexes, tIndexes = triangleUnten  
            triangleVertexList.Add(tVertexes)
            triangleIndexList.Add(tIndexes)
            
        // Obere Dreicke
        let starti = triangleIndexList.Count * 3
        for i in 0..unten.Length-2 do
            let triangleO = triVertices unten.[i] oben.[i] oben.[i+1]  color (starti + i*3) isTransparent
            let tVertexes, tIndexes = triangleO  
            triangleVertexList.Add(tVertexes)
            triangleIndexList.Add(tIndexes) 

        (triangleVertexList, triangleIndexList)  

    // ----------------------------------------------------------------------------------------------------
    // Erzeugen der Meshdaten für ein dreieckiges Patch   
    // ----------------------------------------------------------------------------------------------------
    let triContext p1 p2 p3 (color:Color) isTransparent = 
        let tv = triVertices p1 p2 p3  color 0 isTransparent 
        let tVertexes, tIndexes = tv   
        let triVertexList = triangleVertices tVertexes
        let triIndexList = triangleIndicesClockwise tIndexes
        let vertices = triVertexList |> Array.ofSeq 
        let indices = triIndexList |> Array.ofSeq 
        new MeshData(vertices, indices, PrimitiveTopology.PatchListWith3ControlPoints)  

    // ----------------------------------------------------------------------------------------------------
    // Erzeugen der Meshdaten für ein Icosahedron
    // ----------------------------------------------------------------------------------------------------
    let icosahedronContext (radius:float32) color topology isTransparent = 
        let p1 = Vector3(-radius,  0.0f,   radius)    // vorn links
        let p2 = Vector3( radius,  0.0f,   radius)    // vorn rechts
        let p3 = Vector3( radius,  0.0f,  -radius)    // hinten rechts
        let p4 = Vector3(-radius,  0.0f,  -radius)    // hinten links
        let p5 = Vector3(   0.0f,  radius,   0.0f)    // oben
        let p6 = Vector3(   0.0f, -radius,   0.0f)    // unten

        let triangleVertexList = new List<TriangleType>()
        let triangleIndexList = new List<TriangleIndexType>()
        let mutable ind = 0

        let addTriangleToVertexList p1 p2 p3  idx =
            let triangle = triVertices p1 p2 p3  color idx isTransparent              
            let tVertexes, tIndexes = triangle  
            triangleVertexList.Add(tVertexes)
            triangleIndexList.Add(tIndexes) 

        addTriangleToVertexList p1 p5 p2 0      // Dreick vorn oben
        addTriangleToVertexList p2 p5 p3 3      // Dreick rechts oben
        addTriangleToVertexList p3 p5 p4 6      // Dreick hinten oben
        addTriangleToVertexList p4 p5 p1 9      // Dreick links oben

        addTriangleToVertexList p1 p2 p6 12     // Dreick vorn unten
        addTriangleToVertexList p2 p3 p6 15     // Dreick rechts unten
        addTriangleToVertexList p3 p4 p6 18     // Dreick hinten unten
        addTriangleToVertexList p4 p1 p6 21     // Dreick links unten

        let vertices = triangleVertexList |> List.ofSeq |> List.collect (fun q -> triangleVertices q)  |> Array.ofSeq 

        let indices = triangleIndexList  |> List.ofSeq |> List.collect (fun ind -> triangleIndicesClockwise ind)  |> Array.ofSeq 

        new MeshData(vertices, indices, topology)  

    // ----------------------------------------------------------------------------------------------------
    // Alle Punkte um height in Y-Richtung verschieben
    // ----------------------------------------------------------------------------------------------------
    let shiftPoints(contour:Vector3[]) (height:float32) = 
        contour |> Array.map (fun (vec:Vector3) -> vec + Vector3.Up * height) 

    let mCompose (vertices1:Vertex[]) (indices1:int[]) (vertices2:Vertex[]) (indices2:int[]) topology =
        let vertices = Array.append vertices1 vertices2  
        let imax1 =  indices1.[indices1.Length-1] + 1
        let indices2 = indices2 |> Array.map (fun i -> i + vertices1.Length)
        let indices = Array.append indices1 indices2  
        (vertices, indices, topology)
        
    // ----------------------------------------------------------------------------------------------------
    // Erzeugen der Meshdaten für ein Corpus. Eine unregelmäßige Fläche mit einer festen Höhe
    // ----------------------------------------------------------------------------------------------------
    let corpusContext (center: Vector3)(lowerContour: Vector3[]) height colorBasis colorTop colorBorder (topology:PrimitiveTopology) isTransparent =
        // Meshdata untere Fläche mit center und lowerContour
        
        let lowerCenter =  center - Vector3.Up * (height / 2.0f)
        let lowerPolygon = polygonTriangleList lowerCenter lowerContour colorBasis isTransparent 
        let (verticesLower, indicesLower) = lowerPolygon
        let verticesL = verticesLower |> List.ofSeq |> List.collect (fun q -> triangleVertices q)  |> Array.ofSeq 
        let indicesL = indicesLower  |> List.ofSeq |> List.collect (fun ind -> triangleIndicesCounterClockwise ind)  |> Array.ofSeq  
        let meshLower = new MeshData(verticesL, indicesL, topology) 

        let upperContour = shiftPoints lowerContour height
        let upperCenter =  center + Vector3.Up * (height / 2.0f)

        // Meshdata obere Fläche mit upperCenter und upperContour
        let upperPolygon = polygonTriangleList upperCenter upperContour colorTop isTransparent  
        let (verticesUpper, indicesUpper) = upperPolygon 
        let verticesU = verticesUpper |> List.ofSeq |> List.collect (fun q -> triangleVertices q)  |> Array.ofSeq 
        let indicesU = indicesUpper  |> List.ofSeq |> List.collect (fun ind -> triangleIndicesClockwise ind)  |> Array.ofSeq 
        let meshUpper = new MeshData(verticesU, indicesU, topology) 

        // Meshdata der Seite
        let border = stripeTriangleList lowerContour upperContour colorBorder isTransparent
        let (verticesBorder, indicesBorder) = border
        let verticesB = verticesBorder |> List.ofSeq |> List.collect (fun q -> triangleVertices q)  |> Array.ofSeq 
        let indicesB = indicesBorder  |> List.ofSeq |> List.collect (fun ind -> triangleIndicesCounterClockwise ind)  |> Array.ofSeq  
        let meshBorder = new MeshData(verticesB, indicesB, topology) 

        // Zusammenfügen
        let lowerAndUpper = meshCompose meshLower meshUpper
        meshCompose lowerAndUpper meshBorder