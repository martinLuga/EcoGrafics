namespace Geometry
//
//  GeometricModel.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic

open SharpDX 

open Base.ModelSupport 
open Base.Framework 

type GeoPolygon = GeoLibrary.Model.Polygon
type GeoPoint = GeoLibrary.Model.Point
type GeoMultiPoint = GeoLibrary.Model.MultiPoint
type GeoLine = GeoLibrary.Model.LineString
type Geo = GeoLibrary.Model.Geometry

exception CreateException of string

// ----------------------------------------------------------------------------------------------------
// Geometrische 2D Objekte
//  Quadrat
//  Kreis ...
// ----------------------------------------------------------------------------------------------------
module GeometricModel2D =
    
    let closePolygon(points:Vector3[]) =
        [points.[0]]                    
        |> Seq.append (points |> Seq.toList)                    
        |> Seq.toArray

    let invertFace(points:Vector3[]) = 
        points
        |> Array.rev
    
    let asVector3(gp: GeoPoint) =
        Vector3(float32 gp.Latitude,0.0f, float32 gp.Longitude )

    let AsPoint(p:Vector3) =
        GeoPoint(float p.Z, float p.X)

    let AsPointList(points: Vector3 []) =
        points |> Seq.map (fun p -> AsPoint(p))

    let AsMultiPoints(points:Vector3[]) =
        let plist = AsPointList(points)
        new GeoMultiPoint(plist)

    let AsPolygon(points:Vector3[]) =
        let plist = AsPointList(points)
        new GeoPolygon(plist)

    let FromMultiPoints(plist:GeoMultiPoint) =
        let points = new List<Vector3>()
        let ls = plist.Geometries 
        for i in 0.. ls.Count - 1 do
            let gp = ls.Item(i) :?>GeoPoint
            let point = Vector3(float32 gp.Latitude, 0.0f, float32 gp.Longitude)
            points.Add(point)
        closePolygon(points.ToArray()) 

    let calcCentroid(points:Vector3[]) =    
        let poly = AsPolygon(points)        
        if not poly.IsValid then
            raise (CreateException("Cannot calc centroid. Invalid polygon"))
        let cent = poly.CalculateCentroid()
        asVector3(cent)

    // ----------------------------------------------------------------------------------------------------
    //   Basis für alle Flächentypen
    // ----------------------------------------------------------------------------------------------------
    [<AbstractClass>]
    [<AllowNullLiteral>]
    type Geometry2D(name: string, points: Vector3 [], color: Color) =
        inherit Geometry(name, points.[0], color, DEFAULT_TESSELATION, DEFAULT_RASTER, Vector3.One)

        let mutable points = points

        new (name, geo:GeoMultiPoint) = new Geometry2D (name, FromMultiPoints(geo), Color.White)

        abstract member Points: Vector3 []

        override this.Center =
            try
                let polygon = closePolygon(this.Points)
                calcCentroid(polygon)
            with :? CreateException as ex ->
                raiseException("Center of " + this.Name + " invalid: " + ex.Data0)

        member this.Union(other: Geometry2D) =
            let geo1 = AsMultiPoints(this.Points)
            let geo2 = AsMultiPoints(other.Points)
            let union = geo1.Union(geo2) :?> GeoMultiPoint
            if union = null then
                raiseException("Union(" + this.Name + "+" + other.Name + ")" + "  without result")
            this.AsPolygon(
                "Union(" + this.Name + "+" + other.Name + ")",
                union)

        member this.Intersection(other: Geometry2D) =
            let geo1 = AsMultiPoints(this.Points)
            let geo2 = AsMultiPoints(other.Points)
            let inter = geo1.Intersection(geo2) :?> GeoMultiPoint
            if inter = null then
                raiseException("Intersection(" + this.Name + "+" + other.Name + ")" + "  without result")
            this.AsPolygon(            
                "Intersection(" + this.Name + "+" + other.Name + ")",
                inter)

        member this.Difference(other: Geometry2D) =
            let geo1 = AsMultiPoints(this.Points)
            let geo2 = AsMultiPoints(other.Points)
            let diff = geo1.Difference(geo2) :?> GeoMultiPoint
            if diff = null then
                raiseException("Difference(" + this.Name + "+" + other.Name + ")" + "  without result")
            this.AsPolygon(            
                "Difference(" + this.Name + "+" + other.Name + ")",
                diff)

        member this.Alone( ) =
            let geo1 = AsMultiPoints(this.Points)  
            this.AsPolygon("Polygon(" + this.Name + ")",
                geo1)

        abstract member AsPolygon: string*GeoMultiPoint->Geometry2D

        default this.Points = points 

    // ----------------------------------------------------------------------------------------------------
    // Polygon (Kontur)
    // ----------------------------------------------------------------------------------------------------
    type Polygon(name: string, points) =
        inherit Geometry2D(name, points, Color.White)

        new (name, geo:GeoMultiPoint) = new Polygon (name, FromMultiPoints(geo))

        override this.Center =
            try
                calcCentroid(this.Points)
            with :? CreateException as ex ->
                raiseException("Center of " + this.Name + " invalid: " + ex.Data0)

        override this.AsPolygon(name, plist) = 
            let points = FromMultiPoints(plist)
            new Polygon(name, points)
        
        override this.ToString() = "Polygon:" + this.Name 

        override this.CreateVertexData(visibility: Visibility) =
            PolygonPatch.CreateMeshData(this.Center, points, this.Color, this.Topology, this.TopologyType, visibility) 
            
    // ----------------------------------------------------------------------------------------------------
    // Kreis
    // ----------------------------------------------------------------------------------------------------
    type Kreis(name: string, origin, radius: float32, color: Color) =
        inherit Geometry2D(name, [|origin|], color)  
        let radius = radius 
        let origin = origin 
        let mutable points: Vector3 [] = [||]
        do
            points <- Circle2D.CreatePointData(origin, color, radius, Shape.Raster, Visibility.Opaque)|> Seq.toArray

        member this.Radius  
            with get() = radius

        override this.Points =
            points

        override this.AsPolygon(name, plist) = 
            let points = FromMultiPoints(plist)            
            new Polygon(name, invertFace(points))

        override this.ToString() = "Kreis:" + this.Name + " r " + radius.ToString() 

        override this.CreateVertexData(visibility: Visibility) =
            let points = invertFace(closePolygon(points))
            PolygonPatch.CreateMeshData(this.Center, points, this.Color, this.Topology, this.TopologyType, visibility) 

    // ----------------------------------------------------------------------------------------------------
    // Rechteck
    // ----------------------------------------------------------------------------------------------------
    type Rechteck(name: string, origin: Vector3, laenge: float32, hoehe: float32, color: Color) =
        inherit Geometry2D(name, [|origin|], color)
        let p1 = origin
        let p2 = new Vector3(p1.X + laenge, 0.0f,   p1.Z )
        let p3 = new Vector3(p1.X + laenge, 0.0f,   p1.Z + hoehe)
        let p4 = new Vector3(p1.X ,         0.0f,   p1.Z + hoehe)

        override this.Center =         
            Vector3(
                origin.X + laenge / 2.0f,
                0.0f,
                origin.Z + hoehe / 2.0f
                )

        override this.AsPolygon(name, plist) = 
            let points = FromMultiPoints(plist)
            new Polygon(name, points)

        override this.ToString() = "Rechteck:" + this.Name + " l " + laenge.ToString() + " h " + hoehe.ToString() 

        override this.Points 
            with get() = [| p1; p2; p3; p4 |]     
        
        override this.CreateVertexData(visibility: Visibility) =
            Square2D.CreateMeshData(p1, p4, p3, p2,  color, visibility, Quality.High)
