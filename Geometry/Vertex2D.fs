namespace Geometry
//
//  Vertex2D.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX

open Base.VertexDefs
open Base.MeshObjects
open Base.ModelSupport
 

// ----------------------------------------------------------------------------------------------------
// Vertexes erzeugen  
// für 2D Elemente
// ----------------------------------------------------------------------------------------------------
module Square2D =

    // ----------------------------------------------------------------------------------------------------
    // Quadrat als Folge von 4 Linien
    // In der XY-Ebene 
    // </summary>
    let squareVertices (p1:Vector3, p2:Vector3, p3:Vector3, p4:Vector3, color:Color, isTransparent) = 

        let v1 = createVertex p1 Vector3.UnitZ  color  (new Vector2(0.0f, 0.0f)) true
        let v2 = createVertex p2 Vector3.UnitZ  color  (new Vector2(1.0f, 0.0f)) true
        let v3 = createVertex p3 Vector3.UnitZ  color  (new Vector2(1.0f, 1.0f)) true
        let v4 = createVertex p4 Vector3.UnitZ  color  (new Vector2(0.0f, 1.0f)) true

        let vert = seq{v1;  v2; v3; v4; v1} |> Seq.toArray
        let ind = seq{0;1;2;3;0} |> Seq.toArray
        new MeshData(vert, ind)

    let CreateMeshData(p1, p2, p3, p4,color:Color, visibility:Visibility, quality:Quality) =
        let isTransparent = TransparenceFromVisibility(visibility)
        squareVertices (p1, p2, p3, p4,color, isTransparent)

module Line2D =
    // ----------------------------------------------------------------------------------------------------
    // Linie In der XY-Ebene
    // ----------------------------------------------------------------------------------------------------
    let lineVertices (ursprung:Vector3, target:Vector3, color:Color, isTransparent) = 

        let v1 = createVertex ursprung Vector3.UnitZ color  (new Vector2(0.0f, 0.0f)) isTransparent 
        let v2 = createVertex target Vector3.UnitZ color  (new Vector2(0.0f, 0.0f)) isTransparent

        let vert = seq{v1;v2} |> Seq.toArray
        let ind = seq{0;1} |> Seq.toArray
        new MeshData(vert, ind)

    let CreateMeshData(ursprung:Vector3, target:Vector3, color:Color, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        lineVertices (ursprung , target , color , isTransparent)