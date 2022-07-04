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
type NTSGeometry = NetTopologySuite.Geometries.Geometry

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
    
    let asVector3(gp: NTSPoint, y:float32) =
        Vector3(float32 gp.X, y, float32 gp.Y)
        
    let asVector3FromCoordinate(coordinate: NTSCoordinate) =
        Vector3(float32 coordinate.X, 0.0f, float32 coordinate.Y)

    let asVector3List(coordinates:NTSCoordinate[]) =
        coordinates |> Seq.map (fun co -> asVector3FromCoordinate(co))

    let AsNTSPoint(p:Vector3) =
        NTSPoint(float p.X, float p.Y, float p.Z)

    let AsNTSCoordinate(p:Vector3) =
        NTSCoordinate(float p.X, float p.Z)

    let AsPointList(points: Vector3 []) =
        points |> Seq.map (fun p -> AsNTSPoint(p)) |> Seq.toArray 

    let AsNTSCoordinates(points: Vector3 []) =    
        points |> Seq.map (fun p -> AsNTSCoordinate(p)) |> Seq.toArray 

    let AsNTSLinearRing(points: Vector3 []) =
        NTSLinearRing (AsNTSCoordinates(points))

    let AsNTSMultiPoint(points:Vector3[]) =
        let plist = AsPointList(points)
        new NTSMultiPoint(plist)

    let AsNTSPolygon(points:Vector3[]) =
        let plist = AsNTSLinearRing(points) 
        new NTSPolygon(plist)

    let FromNTSMultiPoints(plist:NTSMultiPoint) =
        let ls = asVector3List(plist.Coordinates) |> Seq.toArray 
        closePolygon(ls) 

    let FromNTSGeometry(geometry:NTSGeometry) =
        let ls = asVector3List(geometry.Coordinates)  |> Seq.rev |> Seq.toArray 
        closePolygon(ls) 

    let calcCentroid(points:Vector3[]) =  
        let poly = AsNTSPolygon(points)        
        //if not poly.IsValid then
        //    raise (CreateException("Cannot calc centroid. Invalid polygon"))
        let mutable cent = poly.Centroid         
        let Y =  points[0] .Y 
        asVector3(cent, Y)

