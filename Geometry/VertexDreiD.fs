//
//  VertexDreiD.fs
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

// ----------------------------------------------------------------------------------------------------
// Vertexes erzeugen  
// für eine vorgegebene Menge an Vertex/Index
// ----------------------------------------------------------------------------------------------------

module VertexDreiD =

    let mutable vCount = 0
    let mutable tCount = 0 
    let mutable reader:StreamReader= null
    let mutable input = ""

    let advanceLines() =
        while (input <> null && not (input.StartsWith("{", StringComparison.Ordinal))) do
            input <- reader.ReadLine()  

    // ----------------------------------------------------------------------------------------------------
    // Erzeugen der Meshdaten für eine Menge von Punkten
    // ----------------------------------------------------------------------------------------------------
    let readFromFile(fileName:String, color:Color) isTransparent =
        let mutable vertices = new List<Vertex>() 
        let mutable indices = new List<int>() 
        reader <- new StreamReader(fileName) 
        
        // Anz Vertex
        input <- reader.ReadLine() 
        if  not (input = null) then
            let first = input.Split(':').[1].Trim() 
            vCount <- Convert.ToInt32(first) 

        // Anz Index
        input <- reader.ReadLine() 
        if not (input = null) then
            let first = input.Split(':').[1].Trim()
            tCount <- Convert.ToInt32(first)

        advanceLines()

        // ----------------------------------------------------------------------------------------------------
        // Vertex
        // ----------------------------------------------------------------------------------------------------
        for i in 0..vCount-1 do
            input <- reader.ReadLine() 
            if input <> null then 
                let vals = input.Split(' ') 
                let pos = 
                    Vector3(
                        Convert.ToSingle(vals.[0].Trim(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(vals.[1].Trim(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(vals.[2].Trim(), CultureInfo.InvariantCulture)
                    ) 
                let norm =
                    Vector3(
                        Convert.ToSingle(vals.[3].Trim(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(vals.[4].Trim(), CultureInfo.InvariantCulture),
                        Convert.ToSingle(vals.[5].Trim(), CultureInfo.InvariantCulture)
                    ) 
                vertices.Add(
                    createVertex pos norm color Vector2.Zero isTransparent                    
                )
        advanceLines()

        // ----------------------------------------------------------------------------------------------------
        // Index
        // ----------------------------------------------------------------------------------------------------
        for i in 0..tCount-1 do 
            input <- reader.ReadLine() 
            if input <> null then  
                let m = input.Trim().Split(' ')
                indices.Add(Convert.ToInt32(m.[0].Trim()))
                indices.Add(Convert.ToInt32(m.[1].Trim()))
                indices.Add(Convert.ToInt32(m.[2].Trim()))

        vertices, indices

    let  dreiDcontext fileName color isTransparent =
        let vertices, indices = readFromFile(fileName, color) isTransparent  
        new MeshData(vertices.ToArray(), indices.ToArray(), PrimitiveTopology.TriangleList)