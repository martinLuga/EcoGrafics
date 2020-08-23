//
//  Vertex3D.fs
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

open System
open System.IO
open System.Globalization

open GeometryUtils
open GeometryTypes

// ----------------------------------------------------------------------------------------------------
// Vertexes erzeugen  
// für 2D Elemente
// ----------------------------------------------------------------------------------------------------
module Vertex2D =

    // ----------------------------------------------------------------------------------------------------
    // Quadrat als Folge von 4 Linien
    // In der XY-Ebene 
    // ----------------------------------------------------------------------------------------------------
    let squareVertices (ursprung:Vector3) laenge (color:Color) = 
        // Front im Gegenuhrzeigersinn
        let p1 = new Vector3(ursprung.X,        ursprung.Y,          ursprung.Z)
        let p2 = new Vector3(ursprung.X,        ursprung.Y + laenge, ursprung.Z)
        let p3 = new Vector3(ursprung.X+laenge, ursprung.Y + laenge, ursprung.Z)
        let p4 = new Vector3(ursprung.X+laenge, ursprung.Y        ,  ursprung.Z)

        let v1 = createVertex p1 Vector3.UnitZ  color  (new Vector2(0.0f, 0.0f)) true
        let v2 = createVertex p2 Vector3.UnitZ  color  (new Vector2(1.0f, 0.0f)) true
        let v3 = createVertex p3 Vector3.UnitZ  color  (new Vector2(1.0f, 1.0f)) true
        let v4 = createVertex p4 Vector3.UnitZ  color  (new Vector2(0.0f, 1.0f)) true

        let vert = seq{v1;  v2; v3; v4; v1} |> Seq.toArray
        let ind = seq{0;1;2;3;0} |> Seq.toArray
        new MeshData(vert, ind,  PrimitiveTopology.LineStrip)