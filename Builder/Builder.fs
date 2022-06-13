namespace Builder
//
//  Vertex3D.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System
open System.Globalization

open log4net

open Base.LoggingSupport
open Base.ModelSupport
open Base.ShaderSupport
 
open Wavefront
open SimpleFormat
open PolygonFormat
open SVGFormat
open Glb
open GlTf

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
        builder.Parts

module PolygonBuilder = 
    // ----------------------------------------------------------------------------------------------------
    // Builder für Polygone aus Datei
    // ----------------------------------------------------------------------------------------------------
    let logger = LogManager.GetLogger("Builder.Polygon")
    let logInfo = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    //  Polygon für eine vorgegebene Menge an Punkten erzeugen
    // ----------------------------------------------------------------------------------------------------
    let Build (name, fileName, height:float32, material:Material, texture:Texture, sizeFactor, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
        logInfo ("Creating Geometry for3D-Points-File:" + fileName  )
        let builder = new PolygonBuilder(name, fileName)  
        builder.Build(height, material, texture, sizeFactor, visibility, augmentation, quality, shaders)  
        builder.Parts

module SvgBuilder = 
    // ----------------------------------------------------------------------------------------------------
    // Builder für Polygone aus Svg-Datei
    // ----------------------------------------------------------------------------------------------------
    let logger = LogManager.GetLogger("Builder.Svg")
    let logInfo = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    //  Polygon für eine vorgegebene Menge an Punkten erzeugen
    // ----------------------------------------------------------------------------------------------------
    let CreateParts (name, element:string, fileName, height:float32, material:Material, texture:Texture, sizeFactor, normalized, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
        logInfo ("Creating polygons from Svg-File:" + fileName  )
        let builder = 
            if element = "*" then
                new SvgBuilder(fileName, name)  
            else
                let elem = Convert.ToInt32(element.Trim(), CultureInfo.InvariantCulture)
                new SvgBuilder(fileName, name, elem)
        builder.CreateParts(height, material, texture, sizeFactor, visibility, augmentation, quality, normalized, shaders)  
        builder.Parts

    let CreateObjects (name, element:string, fileName, position, height:float32, material:Material, texture:Texture, sizeFactor, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
        logInfo ("Creating polygons from Svg-File:" + fileName  )
        let builder = 
            if element = "*" then
                new SvgBuilder(fileName, name)  
            else
                let elem = Convert.ToInt32(element.Trim(), CultureInfo.InvariantCulture)
                new SvgBuilder(fileName, name, elem)
        builder.CreateObjects(height, material, texture, position, sizeFactor, visibility, augmentation, quality, shaders)  
        builder.Objects

module GlbBuilder = 
    // ----------------------------------------------------------------------------------------------------
    // Builder für das glb Format
    // ----------------------------------------------------------------------------------------------------
    let logger = LogManager.GetLogger("Builder.Glb")
    let logInfo = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    //  Parts für eine vorgegebene Menge an Vertex/Index erzeugen
    // ----------------------------------------------------------------------------------------------------
    let Build (name, fileName, material:Material, texture:Texture, sizeFactor, visibility:Visibility, augmentation:Augmentation, quality:Quality, shaders:ShaderConfiguration) =
        logInfo ("Creating Geometry for GLB-File: " + fileName  )
        let builder = new GlbBuilder(name, fileName) 
        builder.Build(material, texture, sizeFactor, visibility, augmentation, quality, shaders)    
        builder.Parts

module GltfBuilder = 
    // ----------------------------------------------------------------------------------------------------
    // Builder für das gltf Format in der EcoGrafics Technologie
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
        builder.Parts
         
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