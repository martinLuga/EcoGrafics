namespace Simulation
//
//  WeltObjects.fs
//
//  Created by Martin Luga on 10.09.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Concurrent
open System.Collections.Generic
open System.Threading

open log4net

open SharpDX

open Base.Logging

open Geometry.GeometricModel
open ApplicationBase.DisplayableObject
open ApplicationBase.MoveableObject

// ----------------------------------------------------------------------------------------------------
// Simulations-Objekte
// Ant : Beweglich 
// Food: Unbeweglich
// ----------------------------------------------------------------------------------------------------
module WeltObjects =

    let logger = LogManager.GetLogger("simulation.WeltObjects")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)
    let logWarn  = Warn(logger)

    let colorMaxEnergy = Color.OrangeRed
    let colorMinEnergy = Color.White
    let colorNoEnergy = Color.Transparent
    let antMinimumEnergy = 50.0f
    let MaxLifetime = 1009L

    // ----------------------------------------------------------------------------------------------------
    // Landscape  
    // ----------------------------------------------------------------------------------------------------   
    type Landscape(name:string, geometry:Geometric, surface, color:Color, position: Vector3) = 
        inherit Immoveable(name, geometry, surface, color, position) 

        override this.isLandscape() = true

        override this.isGround() =
            if this.Name.ToUpper() = "GROUND" then 
                true
            else
                false

        override this.ToString() = 
            "Landscape:" + name + ": " + this.Position.ToString() 