namespace Geometry
//
//  GeometricModel2D.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//


open log4net

open SharpDX 
open SharpDX.Direct3D 

open Base.GlobalDefs
open Base.Framework

open DirectX.MeshObjects
open DirectX.MathHelper

open Vertex2D 

open GeometricModel

// ----------------------------------------------------------------------------------------------------
// Geometrische Objekte
// Quadrat
// Dreieck
// ----------------------------------------------------------------------------------------------------
module GeometricModel2D = 

    let logger = LogManager.GetLogger("GeometricModel2D")

    // ----------------------------------------------------------------------------------------------------
    // Quadrat
    // ----------------------------------------------------------------------------------------------------
    type Square(name:string, seitenlaenge:float32, color:Color) =
        inherit Geometric(name, Vector3.Zero, color, PrimitiveTopology.LineStrip, DEFAULT_TESSELATION, DEFAULT_RASTER)
        let mutable seitenlaenge=seitenlaenge

        member this.SeitenLaenge
            with get () = seitenlaenge
            and set (value) = seitenlaenge <- value      
        
        member this.ColorFront=color 

        override this.Boundaries(objectPosition) = 
            this.Minimum <-  objectPosition  
            this.Maximum <- Vector3(objectPosition.X + this.SeitenLaenge, objectPosition.Y + this.SeitenLaenge, objectPosition.Z + this.SeitenLaenge) 
            (this.Minimum, this.Maximum)

        override this.Center = makeCenter base.Ursprung this.SeitenLaenge

        override this.resize newSize  = 
            this.SeitenLaenge <- this.SeitenLaenge * newSize 

        override this.getVertexData(isTransparent) =
            squareVertices this.Ursprung this.SeitenLaenge this.Color 