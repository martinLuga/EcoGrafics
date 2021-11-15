namespace Geometry
//
//  PyramidVertex.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX

open Base.MeshObjects
open Base.ModelSupport
 
open GeometricTypes

// ----------------------------------------------------------------------------------------------------
// Vertexes erzeugen  
// Für eine Pyramide
// ---------------------------------------------------------------------------------------------------- 
module VertexPyramid =
    
    let mutable normalBasis  = - Vector3.UnitY                      // Bottom, immer nach unten  
    let mutable normalFront  =   Vector3.Zero                       // Front, initialisiert, wird berechnet   
    let mutable normalBack   = new Vector3 ( 0.0f,  0.0f, -1.0f)    // Back,  initialisiert, wird berechnet  
    let mutable normalLeft   = new Vector3 (-1.0f,  0.0f,  0.0f)    // Left,  initialisiert, wird berechnet        
    let mutable normalRight  = new Vector3 (-1.0f,  0.0f,  0.0f)    // Right, initialisiert, wird berechnet  

    // Basis Quadrat + 4 Dreiecke bilden die Pyramide
    let pyramid (ursprung:Vector3) laenge hoehe colorBasis colorFront colorRight colorBack colorLeft  isTransparent =  
        
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
        
        // Punkte
        let p1 = new Vector3(ursprung.X, ursprung.Y, ursprung.Z)
        let p2 = new Vector3(ursprung.X+laenge, ursprung.Y, ursprung.Z)
        let p3 = new Vector3(ursprung.X+laenge, ursprung.Y, ursprung.Z+laenge)
        let p4 = new Vector3(ursprung.X, ursprung.Y, ursprung.Z+laenge)
        let p5 = new Vector3(ursprung.X+laenge/2.0f, ursprung.Y+hoehe, ursprung.Z+laenge/2.0f)

        normalFront <- createNormal p4 p3 p5
        normalRight <- createNormal p3 p2 p5
        normalBack  <- createNormal p2 p1 p5
        normalLeft  <- createNormal p1 p4 p5

        // Basis
        let basisv, basisi  = square   p1 p2 p3 p4 normalBasis colorBasis 0  isTransparent
        let frontv,fronti   = triangle p1 p5 p2    normalFront colorFront 4  isTransparent
        let rightv,righti   = triangle p2 p5 p3    normalRight colorRight 7  isTransparent
        let backv,backi     = triangle p3 p5 p4    normalBack  colorBack  10 isTransparent
        let leftv,lefti     = triangle p4 p5 p1    normalLeft  colorLeft  13 isTransparent

        let vert = {BASE = basisv ; FRONT = frontv ; RIGHT = rightv ; BACK = backv ; LEFT = leftv}
        let ind = {IBASE = basisi ; IFRONT = fronti ; IRIGHT = righti ; IBACK = backi ; ILEFT = lefti }
        (vert, ind)   

    let pyramidVertices (ursprung:Vector3, laenge:float32, hoehe:float32, colorFront:Color, colorRight:Color, colorBack:Color, colorLeft:Color, colorBasis:Color, isTransparent) =
        let (pyramidVertexes, pyramidIndexes) = pyramid ursprung laenge hoehe colorBasis colorFront colorRight colorBack colorLeft isTransparent
        
        let vertexList = deconstructPyramid pyramidVertexes      
        let indexList = deconstructPyramidIndex pyramidIndexes

        let vertices = vertexList |> Array.ofSeq 
        let indices = indexList |> Array.ofSeq 
        vertices, indices

    let CreateMeshData(ursprung:Vector3, laenge:float32, hoehe, colorFront:Color, colorRight:Color, colorBack:Color, colorLeft:Color, colorBasis:Color, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        new MeshData( 
            pyramidVertices (ursprung, laenge, hoehe, colorFront, colorRight, colorBack, colorLeft, colorBasis, isTransparent)
        )
