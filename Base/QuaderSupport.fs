namespace Base
//
//  GeometryTypes.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open SharpDX

open Framework

 


// ----------------------------------------------------------------------------------------------------
//  Geometrie Typen 
// ----------------------------------------------------------------------------------------------------
module QuaderSupport =

    let logger = LogManager.GetLogger("QuaderSupport")

    exception GeometryException of string

    type CollisionState =
        { closest: Vector3
          collides: bool
          distance: float32 }

    let mutable DEFAULT_RASTER = 8
    let DEFAULT_TESSELATION = 1.0f

    //
    // Achsenparallele Flächen
    //

    type PlanePosition =
        | FRONT
        | BACK
        | LEFT
        | RIGHT
        | TOP
        | BOTTOM
        | NONE

    let planePositions =
        [ FRONT
          BACK
          LEFT
          RIGHT
          TOP
        ]

    let planeSeiten =
        [ FRONT
          BACK 
          LEFT
          RIGHT
          TOP
          BOTTOM ]

    let normalAt planePosition =
        match planePosition with
        | FRONT  -> Vector3.UnitZ * -1.0f
        | BACK   -> Vector3.UnitZ
        | LEFT   -> Vector3.UnitX * -1.0f
        | RIGHT  -> Vector3.UnitX
        | BOTTOM -> Vector3.UnitY * -1.0f
        | TOP    -> Vector3.UnitY
        | _ -> raise (GeometryException("Plane-Position für Normale nicht ermittelt"))   

    let oppositePosition position =
        match position with
        | FRONT  -> BACK
        | BACK   -> FRONT
        | LEFT   -> RIGHT
        | RIGHT  -> LEFT
        | BOTTOM -> TOP
        | TOP    -> BOTTOM
        | NONE   -> NONE

    let missingPosition positions =
        planePositions |> Seq.except positions  |> Seq.exactlyOne

    let planePositionAt (position:Vector3, minimum:Vector3, maximum:Vector3) =
        if approximatelyEqual(position.Z, minimum.Z) then 
            FRONT
        else if approximatelyEqual(position.Z, maximum.Z) then 
            BACK
        else if approximatelyEqual(position.X, minimum.X) then 
            LEFT  
        else if approximatelyEqual(position.X, maximum.X) then 
            RIGHT  
        else if approximatelyEqual(position.Y, minimum.Y) then 
            BOTTOM 
        else if approximatelyEqual(position.Y, maximum.Y) then 
            TOP    
        else    
            raise (GeometryException("Plane-Position nicht ermittelt"))    
            
    let getNormalAt(position: Vector3, minimum:Vector3, maximum:Vector3) = 
        let planePosition = planePositionAt(position, minimum, maximum)
        normalAt planePosition 

    type TesselationMode = 
        TRI | QUAD | BEZIER | NONE

    //
    // Allgemeine Fläche
    //

    type PlaneType = {P1: Vector3 ; P2: Vector3 ; P3: Vector3; P4: Vector3}
    let planePoints (pl:PlaneType) = 
        let {P1 = p1; P2 = p2; P3 = p3; P4 = p4 } = pl
        [p1; p2; p3; p4]

    let createPlane (p1: Vector3, p2: Vector3, p3: Vector3, p4: Vector3) = { P1 = p1; P2 = p2; P3 = p3; P4 = p4 }

    let newPlane (p1: Vector3, laenge: float32, hoehe: float32, breite: float32) =
        { P1 = p1
          P2 = new Vector3(p1.X, p1.Y + hoehe, p1.Z)
          P3 = new Vector3(p1.X + laenge, p1.Y + hoehe, p1.Z)
          P4 = new Vector3(p1.X + laenge, p1.Y, p1.Z) }

    let planeNormal (pl:PlaneType) = 
        let {P1 = p1; P2 = p2; P3 = p3; P4 = p4 } = pl
        let u = p2 - p1
        let v = p3 - p1
        Vector3.Cross(u,v)

    let logPlane(pl:PlaneType, logger:ILog) =
        let {P1 = p1; P2 = p2; P3 = p3; P4 = p4 } = pl
        logger.Debug(
            "FLäche mit P1(" + p1.ToString() + ")" + 
            "FLäche mit P2(" + p2.ToString() + ")" +  
            "FLäche mit P3(" + p3.ToString() + ")" + 
            "FLäche mit P4(" + p4.ToString() + ")" 
        )

    let printPlane(pl:PlaneType) =
        let {P1 = p1; P2 = p2; P3 = p3; P4 = p4 } = pl        
        "FLäche mit P1(" + p1.ToString() + ")" + 
        "FLäche mit P2(" + p2.ToString() + ")" +  
        "FLäche mit P3(" + p3.ToString() + ")" + 
        "FLäche mit P4(" + p4.ToString() + ")" 
    
    // Spatprodukt
    let ScalarTriple(point:Vector3, pl:PlaneType) =
        let {P1 = p1; P2 = p2; P3 = p3; P4 = p4 } = pl
        let u = p2 - p1
        let v = p3 - p1
        let norm = Vector3.Cross(u,v)
        let w = point - p1
        Vector3.Dot(norm,w)

    // Spatprodukt zu (Punkt, Fläche)
    let propableComplanar(point:Vector3, pl:PlaneType) =
        pl, ScalarTriple(point, pl)

    // Die Seiten eines Quaders
    let planeForPosition (planePosition:PlanePosition, position:Vector3, seitenlaenge:float32) =
        match planePosition with
        | FRONT  -> newPlane(position                                     , seitenlaenge, seitenlaenge, seitenlaenge)
        | BACK   -> newPlane(position + (Vector3.ForwardLH * seitenlaenge), seitenlaenge, seitenlaenge, seitenlaenge)
        | LEFT   -> newPlane(position,                                      seitenlaenge, seitenlaenge, seitenlaenge)
        | RIGHT  -> newPlane(position + (Vector3.Right * seitenlaenge),     seitenlaenge, seitenlaenge, seitenlaenge)
        | BOTTOM -> newPlane(position,                                      seitenlaenge, seitenlaenge, seitenlaenge)
        | TOP    -> newPlane(position + (Vector3.Up * seitenlaenge),        seitenlaenge, seitenlaenge, seitenlaenge)
        | _ -> raise (GeometryException("Plane-Position für Quadrat nicht ermittelt"))  

    
    let points(pl:PlaneType) =        
        let {P1 = p1; P2 = p2; P3 = p3; P4 = p4 } = pl
        [p1; p2; p3; p4]

    // TODO : Rausbekommen wann kleiner, grösser
    let isOutside (position:Vector3, pl:PlaneType) = 
        let result = ScalarTriple(position, pl) > 0.0f
        result

    let seiteVonQuadrat (p1: Vector3, p2: Vector3, p3: Vector3, p4: Vector3) =
        max (max ((p2 - p1).Length()) ((p3 - p2).Length())) (max ((p4 - p3).Length()) ((p1 - p4).Length()))