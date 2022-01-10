namespace Base
//
//  GlobalDefs.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open System

// ----------------------------------------------------------------------------
// Globale Variable für die PGM-Logik
// Typen
// Konstante
// ----------------------------------------------------------------------------
module GlobalDefs =

    let COUNTERCLOCKWISE = false
    let CLOCKWISE = true

    type CoordinatRule = | LEFT_HANDED | RIGHT_HANDED  
    let  ACTUAL_COORD_RULE = CoordinatRule.RIGHT_HANDED

    // Raster wird dort benötigt, wo Tesselierung programmatisch betrieben wird
    type Raster = | SehrGrob = 8 | Grob = 16 | Mittel = 32 | Fein = 64

    let pi:float32 = float32 System.Math.PI
    let IIpi = 2.0f * pi
    let piviertel = pi / 4.0f
    let pihalbe = pi / 2.0f
    let dreiPiHalbe =  3.0f * pihalbe 

    type LogMode = | NONE = 1 | DEBUG = 2 | VERBOSE = 3 
    let mutable logMode = LogMode.NONE

    type WorkStatus = | WAIT = 0 | WORK  = 1 | SUCCESS = 2 | FAILED = 3 | ABENDED = 4  
    type WorkResult =   
        struct 
            val Status: WorkStatus   
            val Result: String   
            new (status, result) =  {Status=status; Result=result}
        end  