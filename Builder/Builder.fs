namespace Builder
//
//  Vertex3D.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX

open log4net

open Base.LoggingSupport
open Base.ModelSupport
open Base.ShaderSupport
open Base.ObjectBase
 
open Wavefront
open SimpleFormat
open PolygonFormat
open Segment
open Svg
open PBR

// ----------------------------------------------------------------------------------------------------
// Client-Schnittestelle
// Für Grafik-Dateien
// ----------------------------------------------------------------------------------------------------

module SimpleBuilder = 
    // ----------------------------------------------------------------------------------------------------
    // Builder für das einfache Format
    // ----------------------------------------------------------------------------------------------------
    let logger = LogManager.GetLogger("Builder.Simple")
    let logInfo = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    //  MeshData für eine vorgegebene Menge an Vertex/Index erzeugen
    // ----------------------------------------------------------------------------------------------------
    let Build (name, fileName, material:Material, texture:Texture, sizeFactor, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
        logInfo ("Creating Geometry for3D-Points-File:" + fileName  )
        let builder = new SimpleBuilder(name, fileName)  
        builder.Build(material, texture, sizeFactor, visibility, augmentation, quality, shaders)  
        builder.Parts |> Seq.toList

module PolygonBuilder = 
    // ----------------------------------------------------------------------------------------------------
    // Builder für Polygone aus Datei
    // ----------------------------------------------------------------------------------------------------
    let logger = LogManager.GetLogger("Builder.Polygon")
    let logInfo = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    //  Polygon für eine vorgegebene Menge an Punkten erzeugen
    // ----------------------------------------------------------------------------------------------------
    let Build (name, fileName, origin, height:float32, material:Material, texture:Texture, sizeFactor, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
        logInfo ("Creating Geometry for3D-Points-File:" + fileName  )
        let builder = new PolygonBuilder(name, fileName)  
        builder.Build(origin, height, material, texture, sizeFactor, visibility, augmentation, quality, shaders)  
        builder.Parts |> Seq.toList

module SvgBuilder = 
    // ----------------------------------------------------------------------------------------------------
    // Builder für Polygone aus Svg-Datei
    // ----------------------------------------------------------------------------------------------------
    let logger = LogManager.GetLogger("Builder.Svg")
    let logInfo = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    //  Polygon für ein SVG-File erzeugen
    // ----------------------------------------------------------------------------------------------------
    let Build (fileName, name, element, position, height:float32, material:Material, texture:Texture, sizeFactor, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
        logInfo ("Creating polygons from Svg-File:" + fileName  )
        
        let mutable klass = ""
        let mutable elem = ""
        
        klass <- if name = "*" then null else name

        elem <- if element = "*" then null else element

        let builder = new SvgBuilder(fileName) 

        builder.Build(klass, elem, height, material, texture, position, sizeFactor, visibility, augmentation, quality, shaders)  
        builder.Objects

module SegmentBuilder = 
    // ----------------------------------------------------------------------------------------------------
    // Builder für Zahlen aus Segmenten
    // ----------------------------------------------------------------------------------------------------
    let logger = LogManager.GetLogger("Builder.Segment")
    let logInfo = Info(logger)

    let mutable segment = new BaseObject("", Vector3.Zero) 

    // ----------------------------------------------------------------------------------------------------
    //  Zahlen in Segmentdarstellung erzeugen
    // ----------------------------------------------------------------------------------------------------
    let Build (zahl, position, height:float32, material:Material, texture:Texture, sizeFactor, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
        if segment.Name = "" then
            let svg = SvgBuilder.Build( 
                "model2d\\7-segment_cdeg.svg",
                "cdeg",
                "0",
                Vector3.Zero,
                height,
                material,
                texture, 
                sizeFactor,
                visibility,
                augmentation,
                quality,
                shaders
            )
            segment <- svg.[0]
        let builder = new SegmentBuilder(segment, Color.Green)  
        builder.Build(zahl, position) 
        builder.Objects

    let Parts (zahl, position, height:float32, material:Material, texture:Texture, sizeFactor, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
        if segment.Name = "" then
            let svg = SvgBuilder.Build( 
                "model2d\\7-segment_cdeg.svg",
                "cdeg",
                "0",
                Vector3.Zero,
                height,
                material,
                texture, 
                sizeFactor,
                visibility,
                augmentation,
                quality,
                shaders
            )
            segment <- svg.[0]
        let builder = new SegmentBuilder(segment, Color.Green)  
        builder.Build(zahl, position) 
        builder.Parts

module PBRBuilder = 
    // ----------------------------------------------------------------------------------------------------
    // Builder für das glb Format
    // ----------------------------------------------------------------------------------------------------
    let logger = LogManager.GetLogger("Builder.Glb")
    let logInfo = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    //  Parts für eine vorgegebene Menge an Vertex/Index erzeugen
    // ----------------------------------------------------------------------------------------------------
    let Build (name, fileName, sizeFactor:Vector3, visibility:Visibility, augmentation:Augmentation) =
        logInfo ("Creating Geometry for GLB-File: " + fileName  )
        let builder = new PBRBuilder(name, fileName) 
        builder.Build(sizeFactor, visibility, augmentation)    
        builder.Parts

module GltfBuilder = 

    open GlTf
    // ----------------------------------------------------------------------------------------------------
    // Builder für das gltf Format in der EcoGrafics Technologie mit VGltf (deprecated)
    // ----------------------------------------------------------------------------------------------------
    let logger = LogManager.GetLogger("Builder.Gltf")
    let logInfo = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    //  Parts für eine vorgegebene Menge an Vertex/Index erzeugen
    // ----------------------------------------------------------------------------------------------------
    let Build (_objectName, _fileName, _sizeFactor, _material:Material, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
        logInfo ("Creating Geometry for GLTF-File: " + _fileName)
        let builder = new GlTfBuilder(_objectName, _fileName)  
        builder.Build(_sizeFactor, _material, visibility, augmentation, quality, shaders)    
        builder.Parts |> Seq.toList

module Gltf2Builder = 
    
    open GlTf2
    // ----------------------------------------------------------------------------------------------------
    // Builder für das gltf Format in der EcoGrafics Technologie mit gltfLoader
    // ----------------------------------------------------------------------------------------------------
    let logger = LogManager.GetLogger("Builder.Gltf2")
    let logInfo = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    //  Parts für eine vorgegebene Menge an Vertex/Index erzeugen
    // ----------------------------------------------------------------------------------------------------
    let Build (_objectName, _fileName, _sizeFactor, _material:Material, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
        logInfo ("Creating Geometry for GLTF2-File: " + _fileName)
        let builder = new GlTf2Builder(_objectName, _fileName)  
        builder.Build(_sizeFactor, _material, visibility, augmentation, quality, shaders)    
        builder.Parts |> Seq.toList
         
module WavefrontBuilder =
    // ----------------------------------------------------------------------------------------------------
    // Builder für das Wavefront-Format
    // ----------------------------------------------------------------------------------------------------
    let logger = LogManager.GetLogger("Builder.Wavefront")
    let logInfo = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    //  Parts für eine Wavefront .obj Datei
    // ----------------------------------------------------------------------------------------------------
    let Build (name: string, fileName: string, material:Material, texture:Texture, sizeFactor, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
        logInfo ("Creating Geometry for Wavefront-File: " + fileName)
        let builder = new WavefrontBuilder(name, fileName)
        builder.CreateMaterials()
        builder.Build(material, texture, sizeFactor, visibility, augmentation, quality, shaders) 
        builder.Parts 