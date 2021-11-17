﻿namespace Geometry
//
//  GeometricModel.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic

open log4net

open SharpDX 
open SharpDX.Direct3D
open SharpDX.Direct3D12

open Base.GlobalDefs
open Base.LoggingSupport
open Base.MathSupport
open Base.QuaderSupport
open Base.ModelSupport 
open Base.MeshObjects
open Base.VertexDefs

// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------
// Geometrische Objekte
// Kugel
// Quader ...
// ----------------------------------------------------------------------------------------------------
// ----------------------------------------------------------------------------------------------------
module GeometricModel = 

    let logger = LogManager.GetLogger("Geometric.Model") 
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)
    let logWarn  = Warn(logger)
    let logError = Error(logger)

    let fileLogger = LogManager.GetLogger("File")
    let fileDebug = Debug(fileLogger)

    exception GeometryException of string

    let mutable instanceCount = 1

    let setRaster (newRaster:Raster) = 
        DEFAULT_RASTER  <- (int newRaster) 
        //tessellation <- (int newRaster)

    let nextInstanceCountString =
        let next = instanceCount + 1
        instanceCount <- next
        next.ToString()

    let QuaderDefaultName = "Quader" + nextInstanceCountString

    let COUNTERCLOCKWISE = false
    let CLOCKWISE = true

    // ----------------------------------------------------------------------------------------------------
    //  Kugel
    // ---------------------------------------------------------------------------------------------------- 
    type Kugel(name: string, origin, radius: float32, color: Color) =
        inherit Geometry(name, origin, color, DEFAULT_TESSELATION, DEFAULT_RASTER, 1.0f) 
        let mutable radius = radius

        new() = Kugel("Sphere", Vector3.Zero, 1.0f, Color.Gray)
        new(radius, farbe) = Kugel("Sphere", Vector3.Zero, radius, farbe)
        new(name, radius, farbe) = Kugel(name, Vector3.Zero, radius, farbe) 

        member this.Radius
            with get () = radius
            and set (value) = radius <- value

        override this.Center  
            with get() = Vector3(base.Origin.X + this.Radius, base.Origin.Y + this.Radius, base.Origin.Z + this.Radius)
            and set (value) = base.Origin <- new Vector3(value.X - this.Radius, value.Y - this.Radius, value.Z - this.Radius)

        override this.Minimum
            with get () = this.Origin 

        override this.Maximum =
            Vector3(origin.X + this.Radius * 2.0f, origin.Y + this.Radius * 2.0f, origin.Z + this.Radius * 2.0f) 

        override this.resize newSize = this.Radius <- this.Radius * newSize

        override this.ToString() = "Kugel:" + this.Name + " r= " + this.Radius.ToString() + " U= " + this.Origin.ToString()
        
        override this.CreateVertexData(visibility:Visibility) =
            VertexSphere.CreateMeshData(origin, color, radius, visibility)

    // ----------------------------------------------------------------------------------------------------
    //  Quader
    // ----------------------------------------------------------------------------------------------------
    type Quader
        (
            name: string,
            ursprung: Vector3,
            laenge: float32,
            hoehe: float32,
            breite: float32,
            colorFront: Color,
            colorRight: Color,
            colorBack: Color,
            colorLeft: Color,
            colorTop: Color,
            colorBottom: Color
        ) =
        inherit Geometry(name, ursprung, colorFront, DEFAULT_TESSELATION, DEFAULT_RASTER, 1.0f)
        let mutable laenge = laenge
        let mutable hoehe = hoehe
        let mutable breite = breite 

        // Front im Uhrzeigersinn
        let mutable p1 = Vector3.Zero
        let mutable p2 = new Vector3(p1.X,        p1.Y + hoehe, p1.Z)
        let mutable p3 = new Vector3(p1.X+laenge, p1.Y + hoehe, p1.Z)
        let mutable p4 = new Vector3(p1.X+laenge, p1.Y        , p1.Z)

        // Back im Uhrzeigersinn
        let p5 = new Vector3(p1.X+laenge, p1.Y,         p1.Z+breite)
        let p6 = new Vector3(p1.X+laenge, p1.Y+hoehe,   p1.Z+breite)
        let p7 = new Vector3(p1.X,        p1.Y+hoehe,   p1.Z+breite)
        let p8 = new Vector3(p1.X,        p1.Y      ,   p1.Z+breite)

        new(name, ursprung, laenge, hoehe, breite, color) =
            Quader(name, ursprung, laenge, hoehe, breite, color, color, color, color, color, color)

        new(name, ursprung, laenge, hoehe, breite) =
            Quader(name, ursprung, laenge, hoehe, breite, Color.Transparent)
        
        new(name, laenge, hoehe, breite, color) =
            Quader(name, Vector3.Zero, laenge, hoehe, breite, color, color, color, color, color, color)

        new(name, laenge, hoehe, breite) =
            Quader(name, laenge, hoehe, breite, Color.Transparent)

        new(name, min: Vector3, max: Vector3, color) = Quader(name, (max.X - min.X), (max.Y - min.Y), (max.Z - min.Z), color)

        new(name, laenge, hoehe, breite, colorFront, colorRight, colorBack, colorLeft, colorTop, colorBottom) =
            Quader(name, Vector3.Zero, laenge, hoehe, breite, colorFront, colorRight, colorBack, colorLeft, colorTop, colorBottom) 

        static member NewFromMinMax(name, min:Vector3, max:Vector3, color) =
            let l = max.X - min.X
            let h = max.Y - min.Y
            let b = max.Z - min.Z
            new Quader(name, min, l, h, b, color)

        static member NewFromBB(name, bb:BoundingBox, color) =
            let a = bb.Center
            let l = bb.Width
            let y = bb.Height
            let z = bb.Depth
            Quader.NewFromMinMax(name, bb.Minimum, bb.Maximum, color)

        member this.Laenge
            with get () = laenge
            and set (value) = laenge <- value

        member this.Hoehe
            with get () = hoehe
            and set (value) = hoehe <- value

        member this.Breite
            with get () = breite
            and set (value) = breite <- value

        member this.ColorFront = colorFront
        member this.ColorRight = colorRight
        member this.ColorBack = colorBack
        member this.ColorLeft = colorLeft
        member this.ColorTop = colorTop
        member this.ColorBottom = colorBottom

        member this.front   = {P1 = p1; P2 = p2; P3 = p3; P4 = p4} 
        member this.right   = {P1 = p4; P2 = p3; P3 = p6; P4 = p5} 
        member this.back    = {P1 = p5; P2 = p6; P3 = p7; P4 = p8} 
        member this.left    = {P1 = p8; P2 = p7; P3 = p2; P4 = p1}             
        member this.top     = {P1 = p2; P2 = p7; P3 = p6; P4 = p3} 
        member this.bottom  = {P1 = p1; P2 = p4; P3 = p5; P4 = p8}  

        member this.planes =
            [this.front; this.right; this.back; this.left; this.top; this.bottom]

        member this.planeWithPoint(p1: Vector3) =
            fst (
                this.planes
                |> List.map (fun (pl: PlaneType) -> propableComplanar (p1, pl))
                |> List.sortBy (fun (tuple: PlaneType * float32) -> snd tuple)
                |> List.head
            )

        override this.ToString() =
            "Quader " +  name  
            + " L: "  
            + sprintf "%4.2f" this.Laenge
            + " B: "
            + sprintf "%4.2f" this.Breite
            + " H: "
            + sprintf "%4.2f" this.Hoehe
            
        override this.Center  
            with get() = Vector3(base.Origin.X + this.Laenge / 2.0f, base.Origin.Y+ this.Hoehe / 2.0f, base.Origin.Z + this.Breite / 2.0f)
            and set (value) = base.Origin <- new Vector3(value.X - this.Laenge / 2.0f, value.Y - this.Hoehe / 2.0f, value.Z - this.Breite / 2.0f)

        override this.resize newSize  = 
            this.Laenge <- this.Laenge * newSize 
            this.Breite <- this.Breite * newSize
            this.Hoehe <- this.Hoehe * newSize

        override this.Minimum
            with get () = this.Origin 

        override this.Maximum  
            with get () = Vector3( this.Origin.X +  laenge,  this.Origin.Y + hoehe,  this.Origin.Z + breite)

        override this.CreateVertexData(visibility:Visibility) =
            VertexCube.CreateMeshData(ursprung, laenge, hoehe, breite, colorFront, colorRight, colorBack, colorLeft, colorTop, colorBottom, visibility)  

    // ----------------------------------------------------------------------------------------------------
    //  Cylinder
    //  Die Punkte werden um das Zentrum 0,0,0 berechnet
    // ----------------------------------------------------------------------------------------------------
    type Cylinder(name: string, origin:Vector3, radius: float32, hoehe: float32, colorCone: Color, colorCap: Color, withCap: bool) =
        inherit Geometry(name, origin, colorCone, DEFAULT_TESSELATION, DEFAULT_RASTER, 1.0f)
        let mutable radius = radius
        let mutable hoehe = hoehe
        let mutable colorCone = colorCone
        let mutable colorCap = colorCap
        let mutable withCap = withCap                
        
        new(name, origin, radius, hoehe, farbe1, farbe2) = Cylinder(name, origin, radius, hoehe, farbe1, farbe2, true)
        new(name, origin, radius, hoehe, farbe) = Cylinder(name, origin, radius, hoehe, farbe, farbe, true)
        new(name, origin, radius, hoehe) = Cylinder(name, origin, radius, hoehe, Color.Transparent, Color.Transparent, true)
        new(name, radius, hoehe) = Cylinder(name, Vector3.Zero, radius, hoehe, Color.Transparent, Color.Transparent, true)
        new(radius, hoehe, farbe) = Cylinder("Cylinder", Vector3.Zero, radius, hoehe, farbe, farbe, true)
        new(radius, hoehe, farbe1, farbe2) = Cylinder("Cylinder", Vector3.Zero, radius, hoehe, farbe1, farbe2, true)
        new(name, radius, hoehe, farbe1, farbe2) = Cylinder(name, Vector3.Zero, radius, hoehe, farbe1, farbe2, true)
        override this.ToString() = "Cylinder r= " + this.Radius.ToString() + " h= " + this.Hoehe.ToString()

        member this.Radius
            with get () = radius
            and set (value) = radius <- value

        member this.Hoehe
            with get () = hoehe
            and set (value) = hoehe <- value

        member this.ColorCone
            with get () = colorCone
            and set (value) = colorCone <- value
        
        member this.ColorCap
            with get () = colorCap
            and set (value) = colorCap <- value

        member this.WithCap
            with get () = withCap
            and set (value) = withCap <- value

        member this.Farbe1 = colorCone
        member this.Farbe2 = colorCap

        override this.Center  
            with get() = Vector3(base.Origin.X + radius, this.Origin.Y + this.Hoehe / 2.0f, this.Origin.Z + radius)
            and set (value) = base.Origin <- new Vector3(value.X  , value.Y - this.Hoehe / 2.0f, value.Z  )

        override this.resize newSize  = 
            this.Radius <- this.Radius * newSize 
            this.Hoehe <- this.Hoehe * newSize 

        override this.Minimum
            with get () = Vector3(this.Origin.X - radius, this.Origin.Y , this.Origin.Z - radius)

        override this.Maximum  
            with get () = Vector3( this.Origin.X +  radius,  this.Origin.Y + hoehe,  this.Origin.Z + radius)

        override this.CreateVertexData(visibility:Visibility) =
            VertexCylinder.CreateMeshData(this.Origin, colorCone, colorCap, hoehe, radius , withCap, visibility)  

    // ----------------------------------------------------------------------------------------------------
    // Pyramid
    // ----------------------------------------------------------------------------------------------------
    type Pyramide(name: string, ursprung, seitenLaenge :float32, hoehe :float32, colorFront:Color, colorRight:Color, colorBack:Color, colorLeft:Color, colorBasis:Color) =
        inherit Geometry(name, ursprung, Color.White, DEFAULT_TESSELATION, DEFAULT_RASTER, 1.0f)

        let mutable seitenLaenge=seitenLaenge  
        let mutable hoehe =hoehe 

        new(name, ursprung, seitenLaenge, hoehe, color)= Pyramide(name, ursprung, seitenLaenge , hoehe, color, color, color, color, color)
        new(name, seitenLaenge, hoehe, color)= Pyramide(name, Vector3.Zero, seitenLaenge , hoehe, color, color, color, color, color) 
        new(name, ursprung, seitenLaenge, hoehe)= Pyramide(name, ursprung, seitenLaenge , hoehe, Color.Transparent)
        new(name, seitenLaenge, hoehe)= Pyramide(name, Vector3.Zero, seitenLaenge , hoehe, Color.Transparent)
        
        member this.SeitenLaenge
            with get () = seitenLaenge
            and set (value) = seitenLaenge <- value  
        
        member this.Hoehe 
            with get () = hoehe
            and set (value) = hoehe <- value 

        member this.World
            with get () = Matrix.Translation(this.Origin)
        
        member this.ColorBasis=colorBasis                                        
        member this.ColorFront=colorFront                                      
        member this.ColorRight=colorRight                                       
        member this.ColorBack=colorBack                                       
        member this.ColorLeft=colorLeft

        override this.Center  
            with get() = Vector3(this.Origin.X + this.SeitenLaenge/ 2.0f, this.Origin.Y + this.Hoehe / 2.0f, this.Origin.Z + this.SeitenLaenge/ 2.0f)
            and set (value) = base.Origin <- new Vector3(value.X  , value.Y - this.Hoehe / 2.0f, value.Z  )

        override this.resize newSize  = 
            this.SeitenLaenge <- this.SeitenLaenge * newSize 
            this.Hoehe <- this.Hoehe * newSize 

        override this.ToString() = "Pyramid l=  " + this.SeitenLaenge.ToString() + " h= " + this.Hoehe.ToString()

        override this.Minimum
            with get () =  this.Origin 

        override this.Maximum  
            with get () = Vector3(this.Origin.X + this.SeitenLaenge , this.Origin.Y + this.Hoehe, this.Origin.Z + this.SeitenLaenge) 

        member this.Corners =
            [this.p1; this.p2; this.p3; this.p4; this.p5]

        // Ecken der Pyramide
        member this.p1 = new Vector3(this.Origin.X               ,        this.Origin.Y,                this.Origin.Z)  
        member this.p2 = new Vector3(this.Origin.X + seitenLaenge,        this.Origin.Y,                this.Origin.Z)
        member this.p3 = new Vector3(this.Origin.X + seitenLaenge,        this.Origin.Y,                this.Origin.Z + seitenLaenge)
        member this.p4 = new Vector3(this.Origin.X ,                      this.Origin.Y,                this.Origin.Z + seitenLaenge)
        member this.p5 = new Vector3(this.Origin.X + seitenLaenge/ 2.0f,  this.Origin.Y + this.Hoehe,   this.Origin.Z + this.SeitenLaenge/ 2.0f)
            
        override this.CreateVertexData(visibility:Visibility) =
            VertexPyramid.CreateMeshData(this.Origin, seitenLaenge, hoehe, colorFront, colorRight, colorBack, colorLeft, colorBasis, visibility)  
        
    // ----------------------------------------------------------------------------------------------------
    //  Quadrat durch eine tesselierte Fläche
    //  Festgelegt durch 4 Eckpunkte
    // ----------------------------------------------------------------------------------------------------
    type Fläche(name: string, p1: Vector3, p2: Vector3, p3: Vector3, p4: Vector3, color:Color, tessFactor:float32) =
        inherit Geometry(name, Vector3.Zero, Color.White, tessFactor, DEFAULT_RASTER, 1.0f)
        let mutable p1=p1
        let mutable p2=p2
        let mutable p3=p3
        let mutable p4=p4
        let mutable normal=Vector3.Zero
        let mutable center=Vector3.Zero                                     
        let mutable seitenlaenge = 0.0f 

        // Konstruktoren zum Anlegen in einer Ebene
        static member InXYPlane (name:string, p1:Vector3, seitenlaenge:float32, normal:Vector3, color:Color) =
            let p2 = p1 + (Vector3.Right * seitenlaenge)
            let p3 = p1 + (Vector3.Right * seitenlaenge) + (Vector3.Up * seitenlaenge)
            let p4 = p1 + (Vector3.Up * seitenlaenge)
            let q = new Fläche(name, p1, p2, p3, p4, color, 1.0f)
            q.setCenter(Vector3(p1.X + seitenlaenge / 2.0f, p1.Y + seitenlaenge / 2.0f , p1.Z))
            q.setNormal(normal)
            q

        static member InYZPlane (name:string, p1:Vector3, seitenlaenge:float32, normal:Vector3, color:Color) =
            let p2 = p1 + (Vector3.ForwardLH * seitenlaenge)
            let p3 = p1 + (Vector3.ForwardLH * seitenlaenge) + (Vector3.Up * seitenlaenge)
            let p4 = p1 + (Vector3.Up * seitenlaenge)
            let q = new Fläche(name, p1, p2, p3, p4, color, 1.0f)
            q.setCenter(Vector3(p1.X, p1.Y  + seitenlaenge / 2.0f , p1.Z + seitenlaenge / 2.0f))
            q.setNormal(normal)
            q
        
        static member InXZPlane (name:string, p1:Vector3, seitenlaenge:float32, normal:Vector3, color:Color) =
            let p2 = p1 + (Vector3.Right * seitenlaenge)
            let p3 = p1 + (Vector3.Right * seitenlaenge) + (Vector3.ForwardLH * seitenlaenge)
            let p4 = p1 + (Vector3.ForwardLH * seitenlaenge)
            let q = new Fläche(name, p1, p2, p3, p4, color, 1.0f) 
            q.setCenter(Vector3(p1.X + seitenlaenge / 2.0f, p1.Y , p1.Z + seitenlaenge / 2.0f))
            q.setNormal(normal)
            q

        static member InPlanePosition (name:string, ursprung:Vector3, seitenlaenge:float32, planePosition:PlanePosition, color:Color) =
            match planePosition with
            | FRONT  -> Fläche.InXYPlane (name, ursprung, seitenlaenge, Vector3.UnitZ          , color)
            | BACK   -> Fläche.InXYPlane (name, ursprung, seitenlaenge, Vector3.UnitZ * -1.0f  , color)
            | LEFT   -> Fläche.InYZPlane (name, ursprung, seitenlaenge, Vector3.UnitX          , color)
            | RIGHT  -> Fläche.InYZPlane (name, ursprung, seitenlaenge, Vector3.UnitX * -1.0f  , color)
            | BOTTOM -> Fläche.InXZPlane (name, ursprung, seitenlaenge, Vector3.UnitY          , color)
            | TOP    -> Fläche.InXZPlane (name, ursprung, seitenlaenge, Vector3.UnitY * -1.0f  , color)
            | _ -> raise (GeometryException("Plane-Position für Quadrat nicht ermittelt"))   
        
        // Dummy-Konstruktor        
        new() = Fläche("Fläche", Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Color.Black, 1.0f)

        member this.setCenter(aCenter)=
            center <- aCenter

        member this.setNormal(aNormal)=
            normal <- aNormal
            
        override this.Center  
            with get() = center
            and set (value) = center <- value

        member this.P1  
            with get() = p1

        member this.P2 
            with get() = p2 

        member this.P3
            with get() = p3 

        member this.P4
            with get() = p4 

        member this.Seitenlaenge
            with get() = seitenlaenge 

        member this.Color
            with get() = color

        override this.resize newSize  = 
            seitenlaenge <- seitenlaenge * newSize 

        override this.ToString() = "Fläche " + name + ":P1=" + p1.ToString()  + ":P2=" + p2.ToString() +  ":P3=" + p3.ToString() +  ":P4=" + p4.ToString()

        override this.Minimum = Base.MathSupport.computeMinimum ([ p1; p2; p3; p4 ])

        override this.Maximum = Base.MathSupport.computeMaximum ([ p1; p2; p3; p4 ])

        override this.Boundaries(objectPosition) = 
            let tiefe = normal * (-1.0f)
            (this.Minimum + objectPosition, this.Maximum + objectPosition + tiefe) 
        
        // Die Box ist die Fläche ergänzt um eine Tiefe (erst mal 1)
        // Die Richtung ist entgegen der Normalen (wo ein anderes Objekt abprallt)
        override this.BoundingBox(objectPosition) =
            let tiefe = normal * (-1.0f)
            let mutable box = BoundingBox(this.Minimum , this.Maximum + tiefe)
            box

        override this.TopologyType = PrimitiveTopologyType.Patch

        override this.Topology = PrimitiveTopology.PatchListWith4ControlPoints

        override this.CreateVertexData(visibility: Visibility) =
            QuadPatch.CreateMeshData(p1, p2, p3, p4, color, visibility)            

    // ----------------------------------------------------------------------------------------------------
    // Triangle
    // ----------------------------------------------------------------------------------------------------
    type TriPatch(name: string, p1: Vector3, p2: Vector3, p3: Vector3, color:Color, tessFactor:float32) = 
        inherit Geometry(name, Vector3.Zero, Color.White, tessFactor, DEFAULT_RASTER, 1.0f)
        let mutable p1=p1
        let mutable p2=p2
        let mutable p3=p3                                  
        let mutable color=color 

        member this.P1  
            with get() = p1

        member this.P2 
            with get() = p2 

        member this.P3
            with get() = p3 

        member this.Color
            with get() = color

        new() = TriPatch("TriPatch", Vector3.Zero, Vector3.Zero, Vector3.Zero, Color.Black, 1.0f)
        new (p1, p2, p3, color) = TriPatch("TriPatch", p1, p2, p3, color, 1.0f)

        // TODO : HACK 
        override this.Center = Vector3(this.Origin.X , this.Origin.Y, this.Origin.Z )

        override this.ToString() = this.P1.ToString() + " / "

        // TODO : HACK
        override this.Boundaries(objectPosition) = 
            this.Minimum <-  Vector3(min this.P1.X this.P2.X   ,min this.P1.Y  this.P2.Y , min this.P1.Z  this.P2.Z  )  
            this.Maximum <-  Vector3(max this.P1.X this.P2.X   , max this.P1.Y  this.P2.Y , max this.P1.Z  this.P2.Z  )
            (this.Minimum, this.Maximum)

        override this.resize newSize  = 
            ()   

        override this.TopologyType = PrimitiveTopologyType.Patch

        override this.CreateVertexData(visibility: Visibility) =
            TriPatch.CreateMeshData(p1, p2, p3, color, visibility) 

    // ----------------------------------------------------------------------------------------------------
    //  Fläche aus Quadraten
    // ----------------------------------------------------------------------------------------------------
    type QuadPlane(name: string, seitenLaenge:float32, patchLaenge:float32, color:Color, tessFactor:float32) =
        inherit Geometry(name, Vector3.Zero, Color.White, tessFactor, DEFAULT_RASTER, 1.0f)
        member this.SeitenLaenge=seitenLaenge
        member this.PatchLaenge=patchLaenge

        new (name, seitenLaenge, patchLaenge, color) = QuadPlane (name, seitenLaenge, patchLaenge, color, 1.0f) 

        override this.ToString() = this.Name + " L " + this.SeitenLaenge.ToString() + " P " + this.PatchLaenge.ToString()

        override this.Minimum
            with get () = Vector3(0.0f, -999.0f , 0.0f)

        override this.Maximum 
            with get () = Vector3(seitenLaenge, 0.0f, seitenLaenge) 
        
        override this.Center   
            with get() = Vector3(base.Origin.X + this.SeitenLaenge / 2.0f, base.Origin.Y + this.SeitenLaenge / 2.0f, base.Origin. Z+ this.SeitenLaenge / 2.0f)
            and set (value) = base.Origin <- new Vector3(value.X - this.SeitenLaenge , value.Y - this.SeitenLaenge, value.Z - this.SeitenLaenge)

        override this.resize newSize  = 
            ()  
        
        override this.TopologyType = PrimitiveTopologyType.Patch

        override this.CreateVertexData(visibility: Visibility) =
            QuadPlanePatch.CreateMeshData(seitenLaenge, patchLaenge, color, visibility)  

    // ----------------------------------------------------------------------------------------------------
    //  Polyeder
    // ----------------------------------------------------------------------------------------------------
    type Polyeder(name: string, ursprung, radius:float32, color:Color, tessFactor:float32) =
        inherit Geometry(name, ursprung, color, tessFactor, DEFAULT_RASTER, 1.0f)
        let mutable radius=radius

        new (name, radius, color, tessFactor) = Polyeder(name, Vector3.Zero, radius, color, tessFactor)

        member this.Radius
            with get() = radius
            and set(value) = radius <- value

        member this.Corners =
            [this.p1; this.p2; this.p3; this.p4; this.p5; this.p6]

        // Ecken des Polyeders
        member this.p1 = Vector3(this.Center.X - radius, this.Center.Y,            this.Center.Z + radius)    // vorn links
        member this.p2 = Vector3(this.Center.X + radius, this.Center.Y,            this.Center.Z + radius)    // vorn rechts
        member this.p3 = Vector3(this.Center.X + radius, this.Center.Y,            this.Center.Z - radius)    // hinten rechts
        member this.p4 = Vector3(this.Center.X - radius, this.Center.Y,            this.Center.Z - radius)    // hinten links
        member this.p5 = Vector3(this.Center.X,          this.Center.Y + radius,   this.Center.Z         )    // oben
        member this.p6 = Vector3(this.Center.X,          this.Center.Y - radius,   this.Center.Z         )    // unten

        member this.TessFactor = tessFactor

        override this.ToString() = this.Name 

        override this.resize newSize = this.Radius <- this.Radius * newSize

        override this.Minimum
            with get () = this.Origin 

        override this.Maximum =
            Vector3(this.Origin.X + this.Radius * 2.0f, this.Origin.Y + this.Radius * 2.0f, this.Origin.Z + this.Radius * 2.0f) 

        override this.Center  
            with get() = Vector3(base.Origin.X + this.Radius, base.Origin.Y + this.Radius, base.Origin.Z + this.Radius)
            and set (value) = base.Origin <- new Vector3(value.X - this.Radius, value.Y - this.Radius, value.Z - this.Radius)

        override this.TopologyType = PrimitiveTopologyType.Patch

        override this.Topology = PrimitiveTopology.PatchListWith3ControlPoints

        override this.CreateVertexData(visibility:Visibility) =
            IcosahedronPatch.CreateMeshData(this.Center, radius, color, visibility)  

    // ----------------------------------------------------------------------------------------------------
    //  Corpus
    // ----------------------------------------------------------------------------------------------------
    let upperContour(contour: Vector3[], height) =
        contour |> Array.map (fun point -> Vector3(point.X, point.Y + height, point.Z))

    type Corpus(name: string, contour: Vector3[], height:float32, colorBottom:Color, colorTop:Color, colorSide:Color) =
        inherit Geometry(name, Vector3.Zero, colorTop, DEFAULT_TESSELATION, DEFAULT_RASTER, 1.0f)
        let mutable minimum = Vector3.Zero 
        let mutable maximum = Vector3.Zero 
        do  
            minimum <- Base.MathSupport.computeMinimum(contour |> Array.toList )
            maximum <- Base.MathSupport.computeMaximum(upperContour(contour, height) |> Array.toList )

        new (name, contour, height, color) = Corpus (name, contour, height, color, color, color)

        member this.Contour=contour
        member this.ColorBottom=colorBottom
        member this.ColorTop=colorTop
        member this.ColorSide=colorSide
        
        override this.Minimum with get() = minimum
        override this.Maximum with get() = maximum

        // Einfache Lösung über den umschließenden Quader
        // Besser wäre center zu berechnen als Schwerpunkt des Polygons
        override this.Center  
            with get() = 
                let x = this.Minimum.X + abs(this.Maximum.X - this.Minimum.X)/ 2.0f 
                let y = this.Minimum.Y + abs(this.Maximum.Y - this.Minimum.Y)/ 2.0f 
                let z = this.Minimum.Z + abs(this.Maximum.Z - this.Minimum.Z)/ 2.0f 
                Vector3(x,y,z)

        member   this.Height = height
        member   this.Width  = this.Maximum.Z - this.Minimum.Z
        member   this.Length = this.Maximum.X - this.Minimum.X

        override this.ToString() =
            "Corpus " +  name  
            + " L:"  
            + sprintf "%4.2f" this.Length
            + " B:"
            + sprintf "%4.2f" this.Width
            + " H:"
            + sprintf "%4.2f" this.Height

        override this.TopologyType = PrimitiveTopologyType.Patch

        override this.Topology = PrimitiveTopology.PatchListWith3ControlPoints
        
        override this.resize newSize  = 
            () 

        override this.CreateVertexData(visibility:Visibility) =
            CorpusPatch.CreateMeshData(this.Center, contour, height, colorBottom, colorTop, colorSide, this.Topology, this.TopologyType, visibility)            

    // ----------------------------------------------------------------------------------------------------
    // Linie
    // ----------------------------------------------------------------------------------------------------
    type Linie(name:string, von:Vector3, bis:Vector3, color:Color) =
        inherit Geometry(name, Vector3.Zero, color, DEFAULT_TESSELATION, DEFAULT_RASTER, 1.0f)
        let mutable von = von
        let mutable bis = bis 

        member this.Von  
            with get() = von

        member this.Bis  
            with get() = bis
        
        member this.ColorFront=color 

        override this.Boundaries(objectPosition) = 
            this.Minimum <- objectPosition + von   
            this.Maximum <- objectPosition + bis
            (this.Minimum, this.Maximum)

        override this.Center = bis - von

        override this.resize newSize  = 
            ()

        override this.ToString() = "Linie:" + this.Name + " von " + von.ToString() + " nach " + bis.ToString()
                    
        override this.TopologyType = PrimitiveTopologyType.Line

        override this.Topology = PrimitiveTopology.LineList

        override this.CreateVertexData(visibility: Visibility) =
            Line2D.CreateMeshData(von, bis, color, visibility)

    // ----------------------------------------------------------------------------------------------------
    // Quadrat mit achsenparallelen Seiten
    // ----------------------------------------------------------------------------------------------------
    type Quadrat(name:string, p1:Vector3, p2:Vector3, p3:Vector3, p4:Vector3, color:Color) =
        inherit Geometry(name,  p1, color, DEFAULT_TESSELATION, DEFAULT_RASTER, 1.0f)
        let mutable p1=p1
        let mutable p2=p2
        let mutable p3=p3
        let mutable p4=p4
        let mutable seitenlaenge=Vector3.Distance(p1, p2)

        static member InXYPlane (name:string, ursprung:Vector3, seitenlaenge:float32, color:Color) =
            let p2 = ursprung + (Vector3.Right * seitenlaenge)
            let p3 = ursprung + (Vector3.Right * seitenlaenge) + (Vector3.Up * seitenlaenge)
            let p4 = ursprung + (Vector3.Up * seitenlaenge)
            new Quadrat(name, ursprung, p2, p3, p4, color)

        static member InYZPlane (name:string, ursprung:Vector3, seitenlaenge:float32, color:Color) =
            let p2 = ursprung + (Vector3.ForwardLH * seitenlaenge)
            let p3 = ursprung + (Vector3.ForwardLH * seitenlaenge) + (Vector3.Up * seitenlaenge)
            let p4 = ursprung + (Vector3.Up * seitenlaenge)
            new Quadrat(name, ursprung, p2, p3, p4, color)
        
        static member InXZPlane (name:string, ursprung:Vector3, seitenlaenge:float32, color:Color) =
            let p2 = ursprung + (Vector3.Right * seitenlaenge)
            let p3 = ursprung + (Vector3.Right * seitenlaenge) + (Vector3.ForwardLH * seitenlaenge)
            let p4 = ursprung + (Vector3.ForwardLH * seitenlaenge)
            new Quadrat(name, ursprung, p2, p3, p4, color)

        static member InPlanePosition (name:string, ursprung:Vector3, seitenlaenge:float32, planePosition:PlanePosition, color:Color) =
            match planePosition with
            | FRONT  -> Quadrat.InXYPlane (name, ursprung,                                      seitenlaenge, color)
            | BACK   -> Quadrat.InXYPlane (name, ursprung + (Vector3.ForwardLH * seitenlaenge), seitenlaenge, color)
            | LEFT   -> Quadrat.InYZPlane (name, ursprung,                                      seitenlaenge, color)
            | RIGHT  -> Quadrat.InYZPlane (name, ursprung + (Vector3.Right * seitenlaenge),     seitenlaenge, color)
            | BOTTOM -> Quadrat.InXYPlane (name, ursprung,                                      seitenlaenge, color)
            | TOP    -> Quadrat.InXYPlane (name, ursprung + (Vector3.Up * seitenlaenge),        seitenlaenge, color)
            | _ -> raise (GeometryException("Plane-Position für Quadrat nicht ermittelt"))    

        member this.SeitenLaenge
            with get () = seitenlaenge
            and set (value) = seitenlaenge <- value    
            
        member this.P1  
            with get() = p1

        member this.P2 
            with get() = p2 

        member this.P3
            with get() = p3 

        member this.P4
            with get() = p4 
        
        member this.ColorFront=color 

        override this.Boundaries(objectPosition) =
            this.Minimum <- objectPosition + p1
            this.Maximum <- objectPosition + p3
            (this.Minimum, this.Maximum)

        override this.Center =
            Vector3(
                this.Origin.X + this.SeitenLaenge / 2.0f,
                this.Origin.Y + this.SeitenLaenge / 2.0f,
                this.Origin.Z + this.SeitenLaenge / 2.0f
            )

        override this.resize newSize =
            this.SeitenLaenge <- this.SeitenLaenge * newSize

        override this.BoundingBox(objectPosition) =
            let mutable box = BoundingBox()
            box.Minimum <- fst (this.Boundaries(objectPosition))
            box.Maximum <- snd (this.Boundaries(objectPosition))
            box

        override this.TopologyType = PrimitiveTopologyType.Patch

        override this.Topology =
            PrimitiveTopology.PatchListWith4ControlPoints

        override this.CreateVertexData(visibility: Visibility) =
            Square2D.CreateMeshData(p1, p2, p3, p4, color, visibility, Quality.Original)

    // ----------------------------------------------------------------------------------------------------
    //  WavefrontShape
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type PatchShape(name: string, ursprung: Vector3, vertices:List<Vertex>, indices:List<int>, size: float32, quality:Quality) =
        inherit FileBased(name, ursprung, vertices , indices, size, quality)     
        
        new(name: string,  ursprung: Vector3,  size: float32, quality:Quality) =
            new PatchShape(name , ursprung , List<Vertex>(), List<int>(), size, quality)

        override this.ToString() = "WavefrontShape (x " + this.Size.ToString() + ") " + this.Name 

    // ----------------------------------------------------------------------------------------------------
    //  SimpleShape
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type TriangularShape(name: string, ursprung: Vector3, vertices:List<Vertex>, indices:List<int>, size: float32, quality:Quality) =
        inherit FileBased(name, ursprung, vertices, indices, size, quality)  
        
        new(name: string,  ursprung: Vector3,  size: float32, quality:Quality) = 
            new TriangularShape(name , ursprung , List<Vertex>(), List<int>(), size, quality)

        override this.ToString() = "SimpleShape (x " + this.Size.ToString() + ") " +  this.Name 

    // ----------------------------------------------------------------------------------------------------
    // Linie
    // ----------------------------------------------------------------------------------------------------
    type Generic(name:string, color:Color) =
        inherit FileBased(name , Vector3.Zero , List<Vertex>(), List<int>(), 1.0f, Quality.Original)
        
        override this.ToString() = "Generic:" + this.Name  
                    
        override this.TopologyType = PrimitiveTopologyType.Line

        override this.Topology = PrimitiveTopology.LineList

        override this.CreateVertexData(visibility: Visibility) =            
            this.MeshData <- new MeshData(this.Vertices |> Seq.toArray, this.Indices|> Seq.toArray)  
            this.MeshData