namespace Builder
//
//  Wavefront.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open Base
open Base.LoggingSupport 
open Base.ModelSupport
open Base.ShaderSupport 
open Base.VertexDefs
open Geometry.GeometricModel
open GlbFormat
open log4net
open SharpDX
open SharpDX.Direct3D
open SharpDX.Direct3D12 
open System.Collections.Generic 

// ----------------------------------------------------------------------------------------------------
// Support für das Einlesen von glb-Files
// ----------------------------------------------------------------------------------------------------
 
module Glb =

    // ----------------------------------------------------------------------------------------------------
    // GeometryBuilder
    // ----------------------------------------------------------------------------------------------------
    type GlbBuilder(name, fileName: string) =
        
        let worker = new Worker(fileName)

        let mutable name = name 
        let mutable parts : List<Part> = new List<Part>()
        let mutable part : Part = null
        let mutable materials: Dictionary<string, ModelSupport.Material> = new Dictionary<string, ModelSupport.Material>()
        let mutable textures : Dictionary<string, ModelSupport.Texture>  = new Dictionary<string, ModelSupport.Texture>()
        let mutable generalSizeFactor = 1.0f
        let mutable augmentation = Augmentation.None
        let mutable isTransparent = false
        let mutable actualMaterial:ModelSupport.Material = null
        let mutable defaultMaterial:ModelSupport.Material = null
        let mutable actualTexture:ModelSupport.Texture = null
        let mutable lastTopology : PrimitiveTopology = PrimitiveTopology.Undefined
        let mutable lastTopologyType : PrimitiveTopologyType = PrimitiveTopologyType.Undefined
 
        // ----------------------------------------------------------------------------------------------------
        //  Erzeugen des Gltf Models
        // ----------------------------------------------------------------------------------------------------
        member this.Build(material:ModelSupport.Material, texture:ModelSupport.Texture, sizeFactor: float32, visibility:Visibility, augment:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
            augmentation        <- augment 
            generalSizeFactor   <- sizeFactor
            actualMaterial      <- material
            defaultMaterial     <- material
            actualTexture       <- texture
            isTransparent       <- visibility = Visibility.Transparent
            
            worker.Initialize(generalSizeFactor)
            this.AddPart(worker.Vertices, worker.Indices, defaultMaterial, actualTexture, visibility, shaders) 

        // ----------------------------------------------------------------------------------------------------
        //  Erzeugen des Parts
        // ----------------------------------------------------------------------------------------------------
        member this.AddPart(vertices, indices, material, texture, visibility, shaders) =
            part <- 
                new Part(
                    name,
                    new TriangularShape(name, Vector3.Zero, vertices, indices, generalSizeFactor, Quality.High),
                    material,
                    texture,
                    visibility,
                    shaders
                )
            parts.Add(part)

        member this.Parts =
            parts 
            |> Seq.toList
            
        member this.Vertices =
            parts 
            |> Seq.map(fun p -> p.Shape.Vertices)   
            |> Seq.concat
            |> Seq.toList 