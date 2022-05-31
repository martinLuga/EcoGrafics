namespace ApplicationBase
//
//  ScenarioSupport.fs
//
//  Created by Martin Luga on 10.09.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open Base.LoggingSupport
open Base.MaterialsAndTextures
open Base.PrintSupport
open Base.ObjectBase
open Geometry.GeometricModel
open log4net
open SharpDX
open System.Collections.Generic 

// ----------------------------------------------------------------------------------------------------
// Ein Scenario stellt eine graphische Ausgangssituation her
// Die anzeigbaren Objekte werden erstellt und der jeweilige GraficController damit versorgt
// Realisiert ist ein Scenario als eine Function
// Hier sind Functions zum Erstellen, Finden und Ausführen vorhanden
// ----------------------------------------------------------------------------------------------------
module Logging = 

    let logObject(displayable:BaseObject, logger, message) =
        logger (
            "BASEOBJECT"     + message + "\n" +
            "   Name      "  + displayable.Name + "\n" +
            "   World     "  + formatMatrix(displayable.World) + "\n" +
            "   Position  "  + formatVector3(displayable.Position) + "\n" +
            "   Scale     "  + formatVector3(displayable.Scale) + "\n" +
            "   Rotation  "  + formatMatrix(displayable.Rotation) + "\n"
            )

    let logTransform (transform: Matrix, logger, message) =
        let mutable scle = Vector3.One
        let mutable rot = Quaternion.Identity
        let mutable tran = Vector3.One
        transform.Decompose(&scle, &rot, &tran) |> ignore
        logger (
            "TRANSFORM   "    + message + "\n" +
            "   Scale       " + formatVector3   (scle) + "\n" +
            "   Translation " + formatVector3   (tran) + "\n" +
            "   Rotation    " + formatQuaternion(rot)  + "\n"
            )