namespace Builder
//
//  Vertex3D.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open Base.LoggingSupport
open Base.ModelSupport
open Base.ShaderSupport
 
open Wavefront
open SimpleFormat

// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------
// Client-Schnittestelle
// Für einfache Grafik-Dateien
// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------

module SimpleBuilder = 
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // Builder für das einfache Format
    // ----------------------------------------------------------------------------------------------------
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
 
// ----------------------------------------------------------------------------------------------------
// Client-Funktionen
// Für die Wavefront-Schnittstelle
// ----------------------------------------------------------------------------------------------------
module WavefrontBuilder =

    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // Builder für das Wavefront-Format
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    let logger = LogManager.GetLogger("Builder.Wavefront")
    let logInfo = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    //  Komplette Displayables  für eine Wavefront .obj Datei
    // ----------------------------------------------------------------------------------------------------
    let Build (name: string, fileName: string, material:Material, texture:Texture, sizeFactor, visibility:Visibility, augmentation:Augmentation, quality:Quality) =
        logInfo ("Creating Geometry for Wavefront-File:" + fileName)
        let builder = new WavefrontBuilder(name, fileName)
        builder.CreateMaterials()
        builder.Build(material, texture, sizeFactor, visibility, augmentation, quality) 
        builder.Parts 