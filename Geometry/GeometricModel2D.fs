namespace Geometry
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
open Base.GameTimer
open Base.QuaderSupport
open Base.ModelSupport 
open Base.MathSupport 
open Base.MeshObjects
open Base.VertexDefs
open Base.MathHelper
open Base.Framework
open Base.GeometryUtils
 
open VertexWaves

type GeoPolygon = GeoLibrary.Model.Polygon
type GeoPoint = GeoLibrary.Model.Point
type GeoMultiPoint = GeoLibrary.Model.MultiPoint
type GeoLine = GeoLibrary.Model.LineString
type Geo = GeoLibrary.Model.Geometry

// ----------------------------------------------------------------------------------------------------
// Geometrische 2D Objekte
//  Quadrat
//  Kreis ...
// ----------------------------------------------------------------------------------------------------
module GeometricModel2D =

    let AsGeoP(points: Vector3 []) =
        points
        |> Seq.map (fun p -> GeoPoint(float p.X, float p.Z))

    // ----------------------------------------------------------------------------------------------------
    //   Basis für alle Flächentypen
    // ----------------------------------------------------------------------------------------------------
    [<AbstractClass>]
    [<AllowNullLiteral>]
    type Geometry2D(name: string, points: Vector3 [], color: Color) =
        inherit Geometry(name, points.[0], color, DEFAULT_TESSELATION, DEFAULT_RASTER, Vector3.One)

        let mutable points = points

        abstract member Points: Vector3 []

        member this.AsGeo() =
            let gplist = this.Points |> Seq.map (fun p -> GeoPoint(float p.X, float p.Z))
            new GeoPolygon(gplist)

        member this.AsGeoPoints() =
            let gplist = this.Points |> Seq.map (fun p -> GeoPoint(float p.X, float p.Z))
            new GeoMultiPoint(gplist)

        member this.Union(other:Geometry2D) =
            let geo1 =  this.AsGeoPoints()
            let geo2 =  other.AsGeoPoints()
            geo1.Union(geo2) :?> GeoMultiPoint

        member this.Intersection(other:Geometry2D) =
            let geo1 =  this.AsGeoPoints()
            let geo2 =  other.AsGeoPoints()
            geo1.Intersection(geo2) :?> GeoMultiPoint

        default this.Points = points 

    // ----------------------------------------------------------------------------------------------------
    // Polygon (Kontur)
    // ----------------------------------------------------------------------------------------------------
    type Polygon(name: string, points) =
        inherit Geometry2D(name, points, Color.White)

        override this.Center =
            let poly = this.AsGeo()  
            let cent = poly.CalculateCentroid()
            Vector3(float32 cent.Longitude, 0.0f, float32 cent.Latitude)

        override this.CreateVertexData(visibility: Visibility) =
            PolygonPatch.CreateMeshData(this.Center, points, this.Color, this.Topology, this.TopologyType, visibility) 

        member this.Union(other:Geometry2D) =
            let g = base.Union(other)  
            Polygon.FromGeo(g)

        member this.Intersection(other:Geometry2D) =
            let g = base.Intersection(other)  
            Polygon.FromGeo(g)

        static member FromGeo(plist:GeoMultiPoint) = 
            let points = new List<Vector3>()
            let ls = plist.Geometries 
            for i in 0.. ls.Count - 1 do
                let gp = ls.Item(i) :?>GeoPoint
                let point = Vector3(float32 gp.Latitude, 0.0f, float32 gp.Longitude)
                points.Add(point)

            // Polygon schliessen
            let f = ls.Item(0) :?>GeoPoint
            let first = Vector3(float32 (f.Latitude), 0.0f, float32 f.Longitude)
            points.Add(first)

            new Polygon("RESULT", points.ToArray())
            
    // ----------------------------------------------------------------------------------------------------
    // Kreis
    // ----------------------------------------------------------------------------------------------------
    type Kreis(name: string, origin, radius: float32, color: Color) =
        inherit Geometry2D(name, [|origin|], color)

        let radius = radius 
        let origin = origin 

        member this.Radius  
            with get() = radius

        member this.Union(other:Geometry2D) =
            let g = base.Union(other) 
            Polygon.FromGeo(g)

        member this.Intersection(other:Geometry2D) =
            let g = base.Intersection(other)  
            Polygon.FromGeo(g)

        override this.Center = origin

        override this.ToString() = "Kreis:" + this.Name + " r " + radius.ToString() 

        override this.Points    
            with get() =
                Circle2D.CreatePointData(origin,  color, radius, Shape.Raster, Visibility.Opaque)|> Seq.toArray
                    
        override this.CreateVertexData(visibility: Visibility) =
            Circle2D.CreateMeshData(origin,  color, radius, Shape.Raster, visibility)

    // ----------------------------------------------------------------------------------------------------
    // Kreis
    // ----------------------------------------------------------------------------------------------------
    type Rechteck(name: string, origin: Vector3, laenge: float32, hoehe: float32, color: Color) =
        inherit Geometry2D(name, [|origin|], color)

        let p1 = origin
        let p2 = new Vector3(p1.X + laenge, 0.0f,   p1.Z )
        let p3 = new Vector3(p1.X + laenge, 0.0f,   p1.Z + hoehe)
        let p4 = new Vector3(p1.X ,         0.0f,   p1.Z + hoehe)

        member this.Union(other:Geometry2D) =
            let g = base.Union(other) 
            Polygon.FromGeo(g)

        override this.Center =         
            Vector3(
                origin.X + laenge / 2.0f,
                0.0f,
                origin.Z + hoehe / 2.0f
                )

        override this.ToString() = "Rechteck:" + this.Name + " l " + laenge.ToString() + " h " + hoehe.ToString() 

        override this.Points 
            with get() = [| p1; p2; p3; p4 |]     
        
        override this.CreateVertexData(visibility: Visibility) =
            Square2D.CreateMeshData(p1, p4, p3, p2,  color, visibility, Quality.High)
