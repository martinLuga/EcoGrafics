namespace Builder
//
//  Wavefront.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System
open System.Collections.Generic
open System.Globalization 

open log4net

open SharpDX
open SharpDX.Direct3D
open SharpDX.Direct3D12

open Base.VertexDefs
open Base.RecordSupport
open Base.ModelSupport
open Base.Framework
open Base.FileSupport
open Base.LoggingSupport 
open Base.ShaderSupport 
open Base.StringConvert
open Base.MathSupport
open Base.MaterialsAndTextures

open Geometry.GeometricModel3D

open BuilderSupport

open WavefrontFormat



// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------
// Support für das Einlesen von Wavefront-Files
// Builder für Displayable, meshes und Materials
// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------
 
module Wavefront =

    let fileLogger = LogManager.GetLogger("File")
    let logFile  = Debug(fileLogger)

    let logger = LogManager.GetLogger("Builder.Wavefront")
    let logDebug = Debug(logger)
    
    type Index(v, t, n) =
        let mutable _vi = v
        let mutable _vt = t
        let mutable _vn = n
        member this.vi with get() = _vi
        member this.ti with get() = _vt
        member this.ni with get() = _vn
        override this.ToString()  = "Index(" + _vi.ToString() + ":" + _vt.ToString() + ":" + _vn.ToString()   + ")" 

    type Face = Index[]        // Ein Flächenelement bestehend aus 1... n Punkten

    // ---------------------------------------------------------------------------------------------------- 
    //  Die Indexe zu den Punkten. 
    //  Ein Index-Block kann 1-3 Indexe enthalten, die ggfs. durch "/" getrennt sind.
    //  f 48 107 45             Indexe: vertex
    //  f 2/3/1 3/4/1 4/6/1     Alle Indexe  vertex, textur, normal
    //  f v1/vt1                Indexe: vertex, textur
    //  f 2//1 3//1 4//1        Indexe  vertex, normal   
    // ----------------------------------------------------------------------------------------------------
    let indexBlock(indexString:string) = 
        let werte = indexString.Trim().Split('/')
        let vi  = ToInt(werte.[0])  // Vertex Index
        if werte.Length > 1 then 
            let vti = ToInt(werte.[1])  // Vertex texture coordinate index
            if werte.Length > 2 then // Vertex Normale Index
                let  ni = ToInt(werte.[2]) 
                vi, vti, ni
            else 
                vi, vti, 0 
        else
            vi, 0 , 0

    let getTopology (itopo: int) =
        match itopo with
        | 1 -> PrimitiveTopology.PointList
        | 2 -> PrimitiveTopology.LineList
        | 3 -> PrimitiveTopology.PatchListWith3ControlPoints
        | 4 -> PrimitiveTopology.PatchListWith4ControlPoints
        | 5 -> PrimitiveTopology.PatchListWith5ControlPoints
        | 6 -> PrimitiveTopology.PatchListWith6ControlPoints
        | 7 -> PrimitiveTopology.PatchListWith7ControlPoints
        | 8 -> PrimitiveTopology.PatchListWith8ControlPoints
        | 9 -> PrimitiveTopology.PatchListWith9ControlPoints
        | _ -> raise (new Exception("Topology not implemented"))

    let getTopologyType (itopo: int) =
        match itopo with
        | 1 -> PrimitiveTopologyType.Point
        | 2 -> PrimitiveTopologyType.Line 
        | 3 
        | 4   
        | 5  
        | 6  
        | 7  
        | 8  
        | 9 -> PrimitiveTopologyType.Patch
        | _ -> raise (new Exception("Topology not implemented"))

    let getTopologyFace(face:Face) = 
        getTopology (face.Length), getTopologyType (face.Length)

    //  Eine Face-Zeile aufteilen: 3 in einer Zeile = Triangle, 4 in einer Zeile => Patch etc
    //  Voraussetzung die Zeile ist face
    let lineAsFace(line:string) : Face =
        let blocks =  line.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
        let indArray = 
            seq { for i in 1.. blocks.Length-1 do               // Den ersten nicht wegen Satzart
                    yield new Index(indexBlock (blocks.[i]))
                }
            |> Seq.toArray 
        indArray 
            
    let MATERIAL_COLOR(color:Color) = 
        new Material( 
            name=color.ToString(),
            ambient=Color4(0.2f),
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=20.0f,
            emissive=color.ToColor4()
        )

    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // Geocache 
    // Datenverwaltung für Wavefront-Objekte
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    type GeoCache(size:int) =

        let mutable points  :Vector3[] = Array.zeroCreate size
        let mutable normals :Vector3[] = Array.zeroCreate size
        let mutable textures:Vector2[] = Array.zeroCreate size

        member this.Points 
            with get() = points

        member this.Normals 
            with get() = normals

        member this.Textures 
            with get() = textures

        member this.pointAt(idx) = 
            if points = null||idx = 0 then Vector3.Zero else points.[idx-1]

        member this.normalAt(idx) = 
            if normals = null||idx = 0 then Vector3.Zero else normals.[idx-1]

        member this.textureAt(idx) = 
            if textures=null||idx = 0 then Vector2.Zero else textures.[idx-1]

        member this.Erase() =
            points      <- Array.zeroCreate size
            normals     <- Array.zeroCreate size
            textures    <- Array.zeroCreate size

        member this.adjustXYZ() =
           let minimum = computeMinimum(points|>Array.toList) 
           for i in 0..size-1 do
                points[i] <- points[i] - minimum

        member this.Resize() =
            let mutable aFactor = this.ComputeFactor()
            for i in 0..size-1 do
                points[i] <- points[i] * aFactor

        member this.ComputeFactor() =
            let minimum = computeMinimum(points|>Array.toList)
            let maximum  = computeMaximum(points|>Array.toList)
            let actualHeight = maximum.Y - minimum.Y
            let actualDepth = maximum.Z - minimum.Z
            let actualWidt = maximum.X - minimum.X
            let mutable actualSize = max actualHeight actualWidt             
            actualSize <- max actualSize   actualDepth 
            let standardHeight = 1.0f
            standardHeight / actualSize 

    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // GeometryBuilder
    // ----------------------------------------------------------------------------------------------------
    // 1. EINLESEN: Gruppen von Zeilen verarbeiten
    // Über die faces die Vertexe aus point, normal, texture  richtig erstellen
    // Damit den Geocache befüllen
    // ----------------------------------------------------------------------------------------------------
    // 2. Meshes erstellen:  Aus Vertexen und Indexen Meshes erstellen.
    // Dazu die richtige toplogy ermitteln
    // Die Steuerung erfolgt über die face Zeilen, die unterschiedliche Länge (topolgy) haben können
    // Dazu werden die Einträge aus dem Geocache benutzt
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------    
    type WavefrontBuilder(name, fileName: string) =
        let mutable name = name 
        let mutable fileName = fileName 
        let mutable groupName = ""
        let mutable parts : List<Part> = new List<Part>()
        let mutable part : Part = null
        let mutable geoCache = new GeoCache(50000)
        let mutable materials : Dictionary<string, Material> = new Dictionary<string, Material>()
        let mutable generalSizeFactor = Vector3.One
        let mutable augmentation = Augmentation.None
        let mutable isTransparent = false
        let mutable visibility = Visibility.Opaque
        let mutable quality = Quality.Medium
        let mutable shaders:ShaderConfiguration = null

        let mutable lastTopology : PrimitiveTopology = PrimitiveTopology.Undefined
        let mutable lastTopologyType : PrimitiveTopologyType = PrimitiveTopologyType.Undefined

        let mutable lines : string list = []
        let mutable atEnd = false

        let mutable materialCount = 0
        let mutable actualMaterial:Material = null
        let mutable defaultMaterial:Material = null
        let mutable actualTexture = null

        do
            lines <- (readLines (fileName) |> Seq.toList)
            logDebug ("Creating Shapes for Wavefront-File:" + fileName)

        member this.Parts =
            parts |> Seq.toList

        member this.Vertices =
            parts 
            |> Seq.map(fun p -> p.Shape.Vertices)   
            |> Seq.concat
            |> Seq.toList 

        member this.Minimum =             
            Base.MathSupport.computeMinimum (Seq.map (fun (p:Part) -> p.Shape.Minimum) parts |>  Seq.toList) 

        member this.Maximum =  
            Base.MathSupport.computeMaximum (Seq.map (fun (p:Part) -> p.Shape.Maximum) parts |>  Seq.toList) 

        //  Build Vertexe: Index points into GeoCache
        member this.makeVertex(index:Index) = 
            let position    = geoCache.pointAt(index.vi) * generalSizeFactor
            let normal      = geoCache.normalAt(index.ni)
            let uv          = geoCache.textureAt(index.ti)
            let color = Color.White
            let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()
            createVertex position normal color4 uv  

        member this.LogVertices() =
            logFile("Vertices of "+  fileName + " ---------------------------------------------------------")
            logFile("Test-Run at "+  DateTime.Now.ToString() + " ---------------------------------------------------------")
            logFile("Minimum is  "+  this.Minimum.ToString() + " ---------------------------------------------------------")
            logFile("Maximum is  "+  this.Maximum.ToString() + " ---------------------------------------------------------")
            for part in parts do
                LogVertices(part.Shape.Vertices, logFile)

        // ----------------------------------------------------------------------------------------------------
        // Build the Displayable deep 
        // Also the MeshData
        // ----------------------------------------------------------------------------------------------------
        member this.Build (material:Material, texture:Texture, sizeFactor: Vector3, _visibility:Visibility, augment:Augmentation, _quality:Quality, _shaders:ShaderConfiguration) =

            isTransparent <- TransparenceFromVisibility(_visibility)
            augmentation <- augment 
            generalSizeFactor <- sizeFactor
            
            actualMaterial  <- material
            defaultMaterial <- material
            actualTexture <- texture
        
            fileName <- fileName
            visibility <- _visibility  
            quality <- _quality
            shaders <- _shaders

            AnalyzeFile(fileName, lines, logDebug)

            this.ParseGeoData()

            geoCache.adjustXYZ()

            geoCache.Resize()

            this.ParseFile()

            match augmentation with
            | Augmentation.Hilite ->
                let hp =  createHilitePartFrom(name, parts) 
                parts.Add(hp)
                logDebug ("Augmentation Hilte " + hp.Shape.Name )
            | Augmentation.ShowCenter ->
                let cp =  createCenterPartFrom(name, parts)
                parts.Add(cp)
            | _ -> 
                ()

            logDebug ("Build complete --------------------------------------------")

            //this.LogVertices()

        member this.ParseGeoData() =

            let mutable idxV = 0 
            let mutable idxT = 0 
            let mutable idxN = 0 
          
            let mutable faceCount = 0

            geoCache.Erase()

            atEnd <- false

            let shapeName() = "Shape-" + faceCount.ToString()

            for line in lines do
        
                match  (line.FirstColumn()) with
        
                | "v"         (*isVertex    *)      ->  
                    let vals = line.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
                    let point =
                        Vector3(
                            Convert.ToSingle(vals.[1].Trim(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(vals.[2].Trim(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(vals.[3].Trim(), CultureInfo.InvariantCulture)
                        )
                    geoCache.Points.[idxV] <- point
                    idxV <- idxV + 1
        
                | "vt"        (*isVertexTexture *)  -> 
                    let vals = line.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
                    let texture =
                        Vector2(
                            Convert.ToSingle(vals.[1].Trim(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(vals.[2].Trim(), CultureInfo.InvariantCulture)
                        )
                    geoCache.Textures.[idxT] <- texture
                    idxT <- idxT + 1 
        
                | "vn"        (*isVertexNormal *)   ->  
                    let vals = line.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
                    let norm =
                        Vector3(
                            Convert.ToSingle(vals.[1].Trim(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(vals.[2].Trim(), CultureInfo.InvariantCulture),
                            Convert.ToSingle(vals.[3].Trim(), CultureInfo.InvariantCulture)
                        )
                    geoCache.Normals.[idxN] <- norm
                    idxN <- idxN + 1
        
                | _ -> ()

        member this.ParseFile() =

            let mutable faceCount = 0
            let mutable idx = 0

            atEnd <- false

            let shapeName() = "Shape-" + faceCount.ToString()

            for line in lines do
        
                match  (line.FirstColumn()) with
        
                | "#"         (* isComment *)       -> 
                    ()   
                        
                | "o"         (*isObject *)         -> 
                    groupName <- line.SecondColumn() 
                    // triggert Gruppenwechsel bei faces
                    lastTopology <- PrimitiveTopology.Undefined
                    lastTopologyType <- PrimitiveTopologyType.Undefined
        
                | "g"         (*isGroup *)          ->   
                    groupName <- line.SecondColumn() 
                    // triggert Gruppenwechsel bei faces
                    lastTopology <- PrimitiveTopology.Undefined
                    lastTopologyType <- PrimitiveTopologyType.Undefined
        
                | "f"         (*isFace *)           -> 
                    let face = lineAsFace (line)
                    let actualTopology, actualTopologyType = getTopologyFace (face)
        
                    // Gruppenwechsel: es folgen faces mit einer anderen Vertex-Anzahl
                    if (actualTopology <> (lastTopology)) || (groupName <> "") then
                        faceCount <- faceCount + 1
                        let faceName = if groupName <> "" then groupName else shapeName()  
        
                        part <- new Part(faceName, actualMaterial, actualTexture, visibility, shaders)
                                
                        let mutable shape:Shape = null
                        match actualTopologyType with
                        | PrimitiveTopologyType.Triangle -> 
                            shape <- new TriangularShape(name + faceName, Vector3.Zero, generalSizeFactor, quality)
                        |_ ->
                            shape <- new PatchShape(name + faceName, Vector3.Zero, generalSizeFactor, quality)                        
                                
                        shape.Topology <- actualTopology
                        shape.TopologyType <- actualTopologyType
                        part.Shape <- shape                        
                        parts.Add(part)
        
                        if groupName = "" then
                            logDebug (
                                "Shape:" + shape.Name +
                                " Topology:" + shape.Topology.ToString() + 
                                " Material:" + actualMaterial.Name +
                                " by topology change to " + actualTopology.ToString()
                            )
                        else 
                            logDebug (
                                "Shape:" + shape.Name +
                                " Topology:" + shape.Topology.ToString() + 
                                " Material:" + actualMaterial.Name +
                                " by new Group= " + groupName
                            )
                        groupName <- ""
                        idx <- 0
        
                    // Verarbeitung für ein face
                    let vertexe = seq {for index in face do this.makeVertex (index)} |> ResizeArray<Vertex>
                    part.Shape.AddVertices(vertexe)
                            
                    // Reverse (Clockwise)
                    let indexe = seq {for i in 0..face.Length-1 do yield idx + i} |> Seq.toList |> List.rev |> ResizeArray<int>
        
                    part.Shape.AddIndices(indexe)
                    idx <- idx + face.Length
        
                    lastTopology <- actualTopology
                    lastTopologyType <- actualTopologyType
        
                | "mtllib"    (*isMaterialLib*)     ->
                    ()
                        
                | "usemtl"    (*isMaterial*)        ->  
                    let materialName = (line.Replace("usemtl ", "")).Trim()
                    actualMaterial <-  
                        try
                            materials.Item(materialName) 
                        with :? KeyNotFoundException -> defaultMaterial      
        
                | _ -> ()
        
        // ----------------------------------------------------------------------------------------------------
        // Material 
        // ----------------------------------------------------------------------------------------------------

        member this.matValueOf(line: string) =
            let blocks = line.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)

            let r = (blocks.[1] |> float32) * 255.0f
            let g = (blocks.[2] |> float32) * 255.0f
            let b = (blocks.[3] |> float32) * 255.0f
            let color = Color4 (Vector4(r, g, b, 255.0f))
            color

        member this.colorValueOf (line: string) =
            let blocks =
                line.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
            (new Color(blocks.[1] |> float32, blocks.[2] |> float32, blocks.[3] |> float32) ).ToColor4() 

        //  Material aus den gesammelten Lines
        //  Ka: specifies ambient color, to account for light that is scattered about the entire scene [see Wikipedia entry for Phong Reflection Model] using values between 0 and 1 for the RGB components.
        //  Kd: specifies diffuse color, which typically contributes most of the color to an object [see Wikipedia entry for Diffuse Reflection]. In this example, Kd represents a grey color, which will get modified by a colored texture map specified in the map_Kd statement
        //  Ks: specifies specular color, the color seen where the surface is shiny and mirror-like [see Wikipedia entry for Specular Reflection].
        //  Ns: defines the focus of specular highlights in the material. Ns values normally range from 0 to 1000, with a high value resulting in a tight, concentrated highlight.
        //  Ni: defines the optical density (aka index of refraction) in the current material. The values can range from 0.001 to 10. A value of 1.0 means that light does not bend as it passes through an object.
        //  d : specifies a factor for dissolve, how much this material dissolves into the background. A factor of 1.0 is fully opaque. A factor of 0.0 is completely transparent.

        member this.makeMaterial(header:string, materialLines:string list) =
            materialCount <- materialCount + 1
            let hdrName = 
                (header.Replace("newmtl ", ""))
            let result = 
                    new Material(
                        name=hdrName,
                        ambient=Color4.White,
                        diffuse=Color4.White,
                        specular=Color4.White,
                        specularPower=0.0f,
                        emissive=Color.Black.ToColor4()
                    )
            for materialLine in materialLines do 
                match materialLine.FirstColumn() with
                |  "Ka"     -> result.Ambient       <- this.colorValueOf(materialLine)    // Ambient
                |  "Kd"     -> result.Emissive      <- this.colorValueOf(materialLine)    // Diffuse
                |  "Ks"     -> result.Specular      <- this.colorValueOf(materialLine)    // Specular
                | _ ->()
            result

        member this.AddMaterial (materialName:string, nextMaterial: Material, logfun) =
            materials.Replace(materialName.Trim(), nextMaterial)
            logfun ("Stored Material: " +  materialName)

        //  Ablegen der Vertex-Informationen
        member this.processMaterial (groupRecords:string list) =
            let materialLines =
                groupRecords.Tail
                |> Seq.toArray
                |> Seq.filter (fun line -> isMaterialRelated (line))
                |> Seq.toList

            if materialLines.Length > 0 then
                let material = this.makeMaterial (groupRecords.Head, materialLines)
                this.AddMaterial(material.Name, material, logDebug)  

        //  Public Create-Methode
        member this.CreateMaterials() =
            let matFile = fileName.Replace(".obj", ".mtl")
            let mutable materialLines:string list = []

            try
                materialLines <- (readLines (matFile) |> Seq.toList)

                logDebug ("Creating Materials from Material-File:" + matFile
                )
            with :? System.IO.FileNotFoundException -> logDebug ("No material-file for name:" + matFile)

            if not materialLines.IsEmpty then

                let nextHeader, startPos, groupRecords, startList, notFound =
                    findNextAndSplit (materialLines, isMaterialDesc)

                if notFound then
                    raise (new Exception("Invalid mtl-File, no entries"))
                else
                    let subLists = splitAtType (materialLines, "newmtl")

                    if not subLists.IsEmpty then
                        for sublist in subLists do
                            this.processMaterial (sublist)