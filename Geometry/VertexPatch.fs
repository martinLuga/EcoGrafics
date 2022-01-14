namespace Geometry
//
//  VertexPatch.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX
open System.Collections.Generic

open Base.VertexDefs
open Base.MeshObjects
open Base.ModelSupport

open GeometricTypes

module Generic = 
    // ----------------------------------------------------------------------------------------------------
    //  Quadratisches Patch durch 4 Controlpoints   
    // ----------------------------------------------------------------------------------------------------
    let quadVertices p1 p2 p3 p4 (color:Color) idx isTransparent =  
        let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()
        let text = defaultQuadTexture 1.0f 
        let texti = deconstructQuadTexture text |> Array.ofSeq 
        let v1 = createVertex  p1  -Vector3.UnitY color4 texti.[0]  
        let v2 = createVertex  p2  -Vector3.UnitY color4 texti.[1]  
        let v3 = createVertex  p3  -Vector3.UnitY color4 texti.[2]   
        let v4 = createVertex  p4  -Vector3.UnitY color4 texti.[3]   
        let vert = {QV1 = v1; QV2 = v2; QV3 = v3; QV4 = v4}  
        let ind =  {QI1 = idx + 0; QI2 = idx + 1; QI3 = idx + 2; QI4 = idx + 3}
        (vert, ind)    
    
    // ---------------------------------------------------------------------------------------------------- 
    //  Quadratisches Patch durch Punkt und Seitenlänge 
    //  Reihenfolge der Punkte ist der Uhrzeigersinn
    // ----------------------------------------------------------------------------------------------------
    let quadPointsFromSquare p1 quadLength (color:Color) idx  =  
        let p2 = p1 + Vector3.UnitX * quadLength 
        let p3 = p1 + Vector3.UnitX * quadLength + Vector3.UnitZ * quadLength   
        let p4 = p1 + Vector3.UnitZ * quadLength 
        quadVertices p4 p3 p2 p1  color  idx 

    // ----------------------------------------------------------------------------------------------------
    //  Vertices für ein dreieckiges Patch   
    //  Die Ecken werden immer im Uhrzeigersinn angelegt
    // ----------------------------------------------------------------------------------------------------
    let triVertices p1 p2 p3 (color:Color) idx isTransparent =     
        let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()
        let text = defaultTriangleTexture 1.0f 
        let texti = deconstructTriangleTexture text |> Array.ofSeq 
        let normal = (createNormal p1 p2 p3) 
        let v1 = createVertex  p1 normal color4 texti.[0]  
        let v2 = createVertex  p2 normal color4 texti.[1]  
        let v3 = createVertex  p3 normal color4 texti.[2]    
        let vert = {TV1 = v1; TV2 = v2; TV3 = v3}   
        let ind =  {ITV1 = idx + 0; ITV2 = idx + 1; ITV3 = idx + 2}
        (vert, ind)  

    // ----------------------------------------------------------------------------------------------------
    //  Triangles für eine unregelmäßige Fläche 
    //  Die Fläche ist gegeben durch einen Mittelpunkt c und die Punkte die den Rand festlegen
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
    //  Triangles für ein Band  
    //  Das Band ist gegeben durch zwei parallele Konturen
    //  TODO Die Normalen sind nicht OK
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
//  Meshes für Patch-Topology erzeugen
// ----------------------------------------------------------------------------------------------------
module QuadPatch =

    open Generic

    // ----------------------------------------------------------------------------------------------------
    //  Erzeugen der Meshdaten für ein quadratisches Patch   
    // ----------------------------------------------------------------------------------------------------
    let quadContext (p1, p2, p3, p4,color:Color,isTransparent) = 
        let qv = quadVertices p1 p2 p3 p4 color 0 isTransparent
        let qVertexes, qIndexes = qv   
        let quadVertexList = deconstructQuad qVertexes
        let quadIndexList = deconstructQuadIndex qIndexes
        let vertices = quadVertexList |> Array.ofSeq 
        let indices = quadIndexList |> Array.ofSeq 
        let oben = new MeshData<Vertex>(vertices, indices)  
        
        let qv = quadVertices p1 p4 p3 p2 color 0 isTransparent
        let qVertexes, qIndexes = qv   
        let quadVertexList = deconstructQuad qVertexes
        let quadIndexList = deconstructQuadIndex qIndexes
        let vertices = quadVertexList |> Array.ofSeq 
        let indices = quadIndexList |> Array.ofSeq 
        let unten = new MeshData<Vertex>(vertices, indices) 
        MeshData.Compose(oben, unten)
    
    let CreateMeshData(p1, p2, p3, p4,color:Color, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        quadContext(p1, p2, p3, p4,color, isTransparent)

module QuadPlanePatch =

    open Generic

    // ----------------------------------------------------------------------------------------------------
    //  Erzeugen der Meshdaten für eine ebene Fläche
    // ----------------------------------------------------------------------------------------------------
    let quadPlaneContext (laenge:float32, patchLaenge:float32, color, isTransparent) =
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
        vertices, indices 
        
    let CreateMeshData(seitenLaenge:float32, patchLaenge:float32, color:Color, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        new MeshData<Vertex>(   
            quadPlaneContext(seitenLaenge, patchLaenge, color, isTransparent)
        ) 

module TriPatch =        

    open Generic 

    // ----------------------------------------------------------------------------------------------------
    //  Erzeugen der Meshdaten für ein dreieckiges Patch   
    // ----------------------------------------------------------------------------------------------------
    let triContext (p1, p2, p3, color:Color, isTransparent) = 
        let tv = triVertices p1 p2 p3  color 0 isTransparent 
        let tVertexes, tIndexes = tv   
        let triVertexList = triangleVertices tVertexes
        let triIndexList = triangleIndicesClockwise tIndexes
        let vertices = triVertexList |> Array.ofSeq 
        let indices = triIndexList |> Array.ofSeq 
        vertices, indices 

    let CreateMeshData(p1, p2, p3, color:Color, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        new MeshData<Vertex>(   
            triContext (p1, p2, p3, color, isTransparent)
        ) 

module IcosahedronPatch =

    open Generic 

    // ----------------------------------------------------------------------------------------------------
    //  Erzeugen der Meshdaten für ein Icosahedron
    // ----------------------------------------------------------------------------------------------------
    let icosahedronContext (center:Vector3, radius:float32, color:Color, isTransparent) = 
        let p1 = Vector3(center.X - radius, center.Y,            center.Z + radius)    // vorn links
        let p2 = Vector3(center.X + radius, center.Y,            center.Z + radius)    // vorn rechts
        let p3 = Vector3(center.X + radius, center.Y,            center.Z - radius)    // hinten rechts
        let p4 = Vector3(center.X - radius, center.Y,            center.Z - radius)    // hinten links
        let p5 = Vector3(center.X,          center.Y + radius,   center.Z         )    // oben
        let p6 = Vector3(center.X,          center.Y - radius,   center.Z         )    // unten

        let triangleVertexList = new List<TriangleType>()
        let triangleIndexList = new List<TriangleIndexType>()

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

        vertices, indices 
        
    let CreateMeshData(center:Vector3, radius:float32, color:Color,  visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        new MeshData<Vertex>(   
            icosahedronContext (center, radius, color, isTransparent)
        ) 

module CorpusPatch =

    open Generic

    // ----------------------------------------------------------------------------------------------------
    //  Erzeugen der Meshdaten für ein Corpus. Eine unregelmäßige Fläche mit einer festen Höhe
    // ----------------------------------------------------------------------------------------------------

    // ----------------------------------------------------------------------------------------------------
    //  Alle Punkte um height in Y-Richtung verschieben
    // ----------------------------------------------------------------------------------------------------
    let shiftPoints(contour:Vector3[]) (height:float32) = 
        contour |> Array.map (fun (vec:Vector3) -> Vector3.Add(vec, height * Vector3.Up )) 

    let corpusContext (center: Vector3, lowerContour: Vector3[], height, colorBasis, colorTop, colorBorder, topology, topologyType, isTransparent) =

        let upperContour = shiftPoints lowerContour height

        let halfHeight = height / 2.0f
        let halfUp = Vector3.Up * halfHeight
        let halfDown = Vector3.Down * halfHeight
        let lowerCenter =  center - halfUp 
        let upperCenter =  center - halfDown 
        
        // Meshdata untere Fläche mit center und lowerContour 
        let lowerPolygon = polygonTriangleList lowerCenter lowerContour colorBasis isTransparent 
        let (verticesLower, indicesLower) = lowerPolygon
        let verticesL = verticesLower |> List.ofSeq |> List.collect (fun q -> triangleVertices q) |> ResizeArray<Vertex> 
        let indicesL = indicesLower  |> List.ofSeq |> List.collect (fun ind -> triangleIndicesCounterClockwise ind) |> ResizeArray<int> 
        let meshLower = MeshData.Create(verticesL, indicesL) 

        // Meshdata obere Fläche mit upperCenter und upperContour
        let upperPolygon = polygonTriangleList upperCenter upperContour colorTop isTransparent  
        let (verticesUpper, indicesUpper) = upperPolygon 
        let verticesU = verticesUpper |> List.ofSeq |> List.collect (fun q -> triangleVertices q)  |> ResizeArray<Vertex> 
        let indicesU = indicesUpper  |> List.ofSeq |> List.collect (fun ind -> triangleIndicesClockwise ind)  |> ResizeArray<int> 
        let meshUpper = MeshData.Create(verticesU, indicesU) 

        // Meshdata der Seite
        let border = stripeTriangleList lowerContour upperContour colorBorder isTransparent
        let (verticesBorder, indicesBorder) = border
        let verticesB = verticesBorder |> List.ofSeq |> List.collect (fun q -> triangleVertices q)  |> ResizeArray<Vertex>  
        let indicesB = indicesBorder  |> List.ofSeq |> List.collect (fun ind -> triangleIndicesCounterClockwise ind)  |> ResizeArray<int>  
        let meshBorder = MeshData.Create(verticesB, indicesB) 

        // Zusammenfügen
        let lowerAndUpper = MeshData.Compose(meshLower, meshUpper)
        MeshData.Compose(lowerAndUpper, meshBorder)

    let CreateMeshData(center: Vector3, lowerContour: Vector3[], height, colorBasis, colorTop, colorBorder, topology, topologyType, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        corpusContext (center, lowerContour, height, colorBasis, colorTop, colorBorder, topology, topologyType, isTransparent)