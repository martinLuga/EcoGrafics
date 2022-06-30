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

type NTSPolygon = NetTopologySuite.Geometries.Polygon
type NTSPoint = NetTopologySuite.Geometries.Point
type NTSCoordinate = NetTopologySuite.Geometries.Coordinate
type NTSMultiPoint = NetTopologySuite.Geometries.MultiPoint
type NTSLine = NetTopologySuite.Geometries.LineString
type NTSLinearRing = NetTopologySuite.Geometries.LinearRing
type Geo = NetTopologySuite.Geometries.Geometry

exception CreateException of string

// ----------------------------------------------------------------------------------------------------
// Support für NetTopologySuite
// ----------------------------------------------------------------------------------------------------
module NTSConversions =
    
    let closePolygon (points: Vector3 []) =
        if points.[0] <> points.[points.Length - 1] then
            [ points.[0] ]
            |> Seq.append (points |> Seq.toList)
            |> Seq.toArray
        else
            points
    let invertFace(points:Vector3[]) = 
        points
        |> Array.rev
    
    let asVector3(gp: NTSPoint) =
        Vector3(float32 gp.X, 0.0f, float32 gp.Y)
        
    let asVector3FromCoordinate(coordinate: NTSCoordinate) =
        Vector3(float32 coordinate.X, float32 coordinate.Y, float32 coordinate.Z)

    let asVector3List(coordinates:NTSCoordinate[]) =
        coordinates |> Seq.map (fun co -> asVector3FromCoordinate(co)) |> Seq.toArray 

    let AsPoint(p:Vector3) =
        NTSPoint(float p.X, float p.Y, float p.Z)

    let AsCoordinate(p:Vector3) =
        NTSCoordinate(float p.X, float p.Z)

    let AsPointList(points: Vector3 []) =
        points |> Seq.map (fun p -> AsPoint(p)) |> Seq.toArray 

    let AsCoordinates(points: Vector3 []) =    
        points |> Seq.map (fun p -> AsCoordinate(p)) |> Seq.toArray 

    let AsLinearRing(points: Vector3 []) =
        NTSLinearRing (AsCoordinates(points))

    let AsMultiPoints(points:Vector3[]) =
        let plist = AsPointList(points)
        new NTSMultiPoint(plist)

    let AsPolygon(points:Vector3[]) =
        let plist = AsLinearRing(points) 
        new NTSPolygon(plist)

    let FromMultiPoints(plist:NTSMultiPoint) =
        let ls = asVector3List(plist.Coordinates )
        closePolygon(ls) 

    let calcCentroid(points:Vector3[]) = 
        let pclos = closePolygon(points)    
        let poly = AsPolygon(pclos)        
        //if not poly.IsValid then
        //    raise (CreateException("Cannot calc centroid. Invalid polygon"))
        let cent =  poly.Centroid 
        asVector3(cent)

