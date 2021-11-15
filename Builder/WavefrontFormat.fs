namespace Builder
//
//  RecordFormatOBJ.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic

open log4net

open SharpDX

open Base.RecordSupport 
open Base.LoggingSupport
open Base.VertexDefs

// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------
// Verarbeiten von Wavefront OBJ-Files
// Die Zeilen der Datei werden in eine Liste eingelesen
// Die Zeilen werden zu Gruppen zusammengefasst
// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------

module WavefrontFormat =

    let fileLogger = LogManager.GetLogger("File")
    let logFile  = Debug(fileLogger)

    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // OBJ Satzarten
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------

    // ----------------------------------------------------------------------------------------------------
    //  Struktur des Wavefront-Formats.
    // 
    //  Vertex data:                        Umgesetzt
    //      v 	Geometric vertices          *
    //      vt 	Texture vertices            *
    //      vn 	Vertex normals              *
    //      vp 	Parameter space vertices    -
    // 
    //  Elements:
    // 
    //     p 	Point                       -
    //     l 	Line                        -
    //     f 	Face                        *
    //     curv 	Curve                   - 
    //     curv2 	2D curve                -
    //     surf 	Surface                 -
    //  
    //  TODO: Das Format wird nicht von Anfang an kompplett umgesetzt.
    //  Es gibt noch mehrere Satzarten 
    // 
    // ----------------------------------------------------------------------------------------------------

    let isComment       (line: string) = comparesTyp "#"        line
    let isNoComment     (line: string) = comparesNotTyp "#"     line
    let isObject        (line: string) = comparesTyp "o"        line
    let isGroup         (line: string) = comparesTyp "g"        line
    let isVertex        (line: string) = comparesTyp "v"        line
    let isVertexTexture (line: string) = comparesTyp "vt"       line
    let isVertexNormal  (line: string) = comparesTyp "vn"       line
    let isFace          (line: string) = comparesTyp "f"        line
    let isMaterialLib   (line: string) = comparesTyp "mtllib"   line 
    let isMaterial      (line: string) = comparesTyp "usemtl"   line 
    let isMaterialDesc  (line: string) = comparesTyp "newmtl"   line 

    let isNs    (line: string) = comparesTyp "Ns" line 
    let isKa    (line: string) = comparesTyp "Ka" line 
    let isKd    (line: string) = comparesTyp "Kd" line 
    let isKs    (line: string) = comparesTyp "Ks" line 
    let isNi    (line: string) = comparesTyp "Ni" line 
    let isd     (line: string) = comparesTyp "d"  line 

    let isPart         (line: string) = comparesTypRange [|"o";"g"; "usemtl"|] line 
    let isStart         (line: string) = comparesTypRange [|"o";"g"|] line 
    let isVertexRelated (line: string) = comparesTypRange [|"v";"vn";"vt"|] line 
    let isIndexRelated  (line: string) = comparesTypRange [|"f"|] line     
    let isMaterialRelated   (line: string) = comparesTypRange [|"Ns";"Ka";"Kd";"Ks";"Ni";"d"|] line

    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    // Analyse
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------

    type AnalyzeResult = | VertexRelated | IndexRelated | Both | MaterialRelated | Nothing
    
    // ----------------------------------------------------------------------------------------------------
    //  Beim Fund eines Headers die Anzahl der gefundenen Objekte auflisten.
    // ----------------------------------------------------------------------------------------------------
    let LogVertices (vertices: List<Vertex>, logfun) =
        for vertex in vertices do
            logfun(vertex.ToString())

    // ----------------------------------------------------------------------------------------------------
    //  Beim Fund eines Headers die Anzahl der gefundenen Objekte auflisten.
    // ----------------------------------------------------------------------------------------------------
    let LogFace (lines: string list, header:string, logfun) =
        
        let mutable result = Nothing
        let anzFaces = findOccurencesCount (lines, "f")
        if anzFaces > 0 then
            logfun (
                "Header: "
                + header.ToString()
                + " with "
                + anzFaces.ToString()
                + "   Faces: "
            )

    let LogPoints(lines: string list, header:string, logfun) =        
        let anzPoints  = findOccurencesCount (lines, "v")
        let anzNormals = findOccurencesCount (lines, "vn")
        let anzTexture = findOccurencesCount (lines, "vt")
        let mutable anzObjects = 0
        if (anzPoints > 0)
           || (anzNormals > 0)
           || (anzTexture > 0) then
            anzObjects <- anzPoints + anzNormals + anzTexture  
        if anzObjects > 0 then
            logfun (
                "Header: "
                + header.ToString()
                + " with "
                + anzObjects.ToString()
                + " lines "   
                + " || Points: "
                + anzPoints.ToString()
                + "   Normals: "
                + anzNormals.ToString()
                + "   Textures: "
                + anzTexture.ToString()
            )

    // ----------------------------------------------------------------------------------------------------
    //  Beim Fund eines Headers die Anzahl der gefundenen Objekte auflisten.
    // ----------------------------------------------------------------------------------------------------
    let Analyze (lines: string list, header:string, logfun) =
        
        let mutable result = Nothing
        let anzVertexe = findOccurencesCount (lines, "v")
        let anzNormals = findOccurencesCount (lines, "vn")
        let anzTexture = findOccurencesCount (lines, "vt")
        let anzFaces = findOccurencesCount (lines, "f")
        let mutable anzObjects = 0

        if (anzVertexe > 0)
           || (anzNormals > 0)
           || (anzTexture > 0) then
            result <- VertexRelated

        if   (anzFaces > 0) then
            result <- IndexRelated

        if ((anzVertexe > 0)
            || (anzNormals > 0)
            || (anzTexture > 0))
           && (anzFaces > 0) then
            result <- Both

        result
    
    // ----------------------------------------------------------------------------------------------------
    //  Die Datei analysieren. 
    //  Um mit den verschiedenen Situationen klarzukommens
    // ----------------------------------------------------------------------------------------------------
    let AnalyzeFile(fileName, lines:string list, logfun) =
        logfun("Analyzing ... ")
        logfun("File " + fileName + " mit " + lines.Length.ToString() + " Zeilen")
        let line, idx, notFound = findWithType(lines, "o")

        // Objekt gefunden
        if not notFound then  
            let startRecords, anzObjekte = findOccurencesInFile(lines, "o")  
            logfun("File enthält " + anzObjekte.ToString() + " Objekt(e) ")  
            //for record in startRecords do logfun("---> " + record) 
        else              
            logfun("Keine Objekte in File " + fileName) 
            let line, idx, notFound = findWithType(lines, "g") 
            // Gruppen gefunden
            if not notFound then  
                let startRecords, anzObjekte = findOccurencesInFile(lines, "g")  
                logfun("File enthält " + anzObjekte.ToString() + " Gruppe(n) ")  
                //for record in startRecords do logfun("---> " + record) 
            else 
                logfun("Keine gruppen in File " + fileName) 