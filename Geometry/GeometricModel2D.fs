namespace Geometry
//
//  GeometricModel.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic

open SharpDX
open SharpDX.Direct3D 
open SharpDX.Direct3D12

open Base.ModelSupport 
open Base.Framework 
open Base.MeshObjects
open Base.GeometryUtils
open Base.MathSupport

open GeometricTypes

open NTSConversions

// ----------------------------------------------------------------------------------------------------
// Geometrische 2D Objekte
//  Quadrat
//  Kreis ...
// ----------------------------------------------------------------------------------------------------

module GeometricModel2D =

    type Representation = | Point| Line| Surface

    // ----------------------------------------------------------------------------------------------------
    //   Basis für alle Flächentypen
    // ----------------------------------------------------------------------------------------------------
    [<AbstractClass>]
    [<AllowNullLiteral>]
    type Geometry2D(name: string, points: Vector3 [], color: Color, representation:Representation) =
        inherit GeometryBased(name, points.[0], color, DEFAULT_TESSELATION, DEFAULT_RASTER, Vector3.One)
        let mutable representation = representation

        new (name, geo:NTSMultiPoint, representation) = new Geometry2D (name, FromNTSMultiPoints(geo), Color.White, representation)
        new (name, geo:NTSMultiPoint, color) = new Geometry2D (name, FromNTSMultiPoints(geo),color , Representation.Surface)
        new (name, geo:NTSMultiPoint) = new Geometry2D (name, FromNTSMultiPoints(geo), Color.White, Representation.Surface)

        abstract member Points: Vector3 []

        override this.Center =
            try
                 calcCentroid(this.Points) 

            with :? CreateException as ex ->
                raiseException("Center of " + this.Name + " invalid: " + ex.Data0)

        abstract member Copy: Unit -> Geometry2D
        default this.Copy() = this
        
        member this.Representation
            with get() = representation
            and set(value) = representation <- value

        // ----------------------------------------------------------------------------------------------------
        // Meshdata
        //  1. Schritt: Die Eckpunkte erzeugen
        //  2. Schritt 
        // ----------------------------------------------------------------------------------------------------
        abstract member CreateTriangles: bool->List<TriangleType>*List<TriangleIndexType>
        default this.CreateTriangles(transparency) =
            raiseException("Not implemented")

        abstract member CreateLines: bool->List<LineType>*List<LineIndexType>
        default this.CreateLines(transparency) =
            raiseException("Not implemented")

        abstract member CreatePoints: bool->List<PointType>*List<PointIndexType>
        default this.CreatePoints(transparency) =
            raiseException("Not implemented")

        override this.CreateVertexData(visibility: Visibility) =        
            let isTransparent = TransparenceFromVisibility(visibility)
            match this.Representation with
            | Representation.Surface -> 
                this.Topology <- PrimitiveTopology.TriangleList
                this.TopologyType <- PrimitiveTopologyType.Triangle
                let triangles = this.CreateTriangles(isTransparent)
                let vertices, indices = Construction.FromTriangles(triangles, false)  // TODO : Umgedreht vom Korpus
                MeshData.Create(vertices, indices)
                
            | Representation.Line -> 
                this.Topology <- PrimitiveTopology.LineList
                this.TopologyType <- PrimitiveTopologyType.Line
                let lines = this.CreateLines(isTransparent)
                let vertices, indices = Construction.FromLines(lines)
                MeshData.Create(vertices, indices )

            | Representation.Point -> 
                this.Topology <- PrimitiveTopology.PointList
                this.TopologyType <- PrimitiveTopologyType.Point
                let points = this.CreatePoints(isTransparent)
                let vertices, indices = Construction.FromPoints(points)
                MeshData.Create(vertices, indices )           

    // ----------------------------------------------------------------------------------------------------
    // Polygon (Kontur)
    // ----------------------------------------------------------------------------------------------------
    type Polygon(name: string, points, representation) =
        inherit Geometry2D(name, points, Color.White, representation)  
        
        let mutable points = closePolygon(points)
  
        new (name: string, points:Vector3[]) = new Polygon (name, closePolygon(points), Representation.Surface)
        new () = new Polygon ("DUMMY", [||],  Representation.Surface)

        member this.Shift(height) = 
            points <- shiftUp(points, height) |> Seq.toArray
 
        override this.Points = points

        override this.Copy() =
            new Polygon(this.Name + "Copy", points, representation)

        member this.NormalizePosition() =             
             let min = computeMinimumXYZ(points|> Seq.toList) 
             for i = 0 to points.Length - 1 do
                 let mutable resized = points.[i]
                 resized  <- points.[i] - min
                 points.[i] <- resized

        override this.ToString() = "Polygon:" + this.Name 

        override this.CreateTriangles(transparency) =
            let triangles = Polygon2D.CreateTriangles(this.Center, points, Color.White, transparency) 
            triangles

        override this.CreateLines(transparency) =
            let lines = Polygon2D.CreateLines(points, Color.White, transparency)
            lines

        override this.CreatePoints(transparency) =
           let points =  Polygon2D.CreatePoints(points , Color.White, transparency)
           points
            
    // ----------------------------------------------------------------------------------------------------
    // Kreis
    // ----------------------------------------------------------------------------------------------------
    type Kreis(name: string, center, radius: float32, representation:Representation) =
        inherit Geometry2D(name, [|center|], Color.White, representation)   
        let radius = radius 
        let origin = center 
        let mutable points: Vector3 [] = [||] 
        do
            points <- closePolygon(Circle2D.GetPoints(origin, radius, Shape.Raster).ToArray())
        
        new (name, origin, radius) = new Kreis (name, origin, radius,  Representation.Surface)

        member this.Radius  
            with get() = radius

        override this.Points =
            points

        override this.Center =
            origin

        override this.ToString() = "Kreis:" + this.Name + " r " + radius.ToString() 

        override this.CreateTriangles(transparency) =    
            let triangles = Polygon2D.CreateTriangles(this.Center, points, Color.White, transparency) 
            triangles

        override this.CreateLines(transparency) =
            let lines = Polygon2D.CreateLines( points, Color.White, transparency)
            lines

        override this.CreatePoints(transparency) =
            let result = Polygon2D.CreatePoints(points, Color.White, transparency)
            result

    // ----------------------------------------------------------------------------------------------------
    // Rechteck
    // ----------------------------------------------------------------------------------------------------
    type Rechteck(name: string, origin: Vector3, laenge: float32, hoehe: float32, representation) =
        inherit Geometry2D(name, [|origin|], Color.White, representation)  
        let p1 = origin
        let p2 = new Vector3(p1.X + laenge, 0.0f,   p1.Z )
        let p3 = new Vector3(p1.X + laenge, 0.0f,   p1.Z + hoehe)
        let p4 = new Vector3(p1.X ,         0.0f,   p1.Z + hoehe)

        new (name: string, origin: Vector3, laenge: float32, hoehe: float32, color:Color) = 
            new Rechteck(name , origin , laenge , hoehe , Representation.Surface)

        override this.Center =         
            Vector3(origin.X + laenge / 2.0f, 0.0f, origin.Z + hoehe / 2.0f)

        override this.ToString() = "Rechteck:" + this.Name + " l " + laenge.ToString() + " h " + hoehe.ToString() 

        override this.Points 
            with get() = [| p1; p2; p3; p4 |]    
            
        override this.CreateTriangles(transparency) =     
            let points = closePolygon(this.Points)
            let triangles = Polygon2D.CreateTriangles(this.Center, points, Color.White, transparency) 
            triangles
        
        override this.CreateLines(transparency) =
            let lines = Polygon2D.CreateLines(this.Points, Color.White, transparency)
            lines

        override this.CreatePoints(transparency) =
            let result = Polygon2D.CreatePoints(this.Points, Color.White, transparency)
            result

    type Geometry2D with
        // ----------------------------------------------------------------------------------------------------
        //  Mengenoperationen: Basis ist this.Points (als Vector3)
        // ----------------------------------------------------------------------------------------------------
        member this.Union(other: Geometry2D) =
            let geo1  = AsNTSPolygon(closePolygon (this.Points))  
            let geo2  = AsNTSPolygon(closePolygon (other.Points))
            let union = geo1.Union(geo2)  
            if union = null then
                raiseException("Union(" + this.Name + "+" + other.Name + ")" + "  without result")
            let points = FromNTSGeometry(union)
            let name = "Union(" + this.Name + "+" + other.Name + ")"
            new Polygon(name, points, this.Representation) 

        member this.Intersection(other: Geometry2D) =
            let geo1 = AsNTSPolygon(closePolygon (this.Points))  
            let geo2 = AsNTSPolygon(closePolygon (other.Points))  
            let inter = geo1.Intersection(geo2) 
            if inter = null then
                raiseException("Intersection(" + this.Name + "+" + other.Name + ")" + "  without result")            
            let points = FromNTSGeometry(inter)
            let name = "Intersection(" + this.Name + "+" + other.Name + ")"
            new Polygon(name, points, this.Representation) 

        member this.Difference(other: Geometry2D) =
            let geo1 = AsNTSPolygon(closePolygon (this.Points)) 
            let geo2 = AsNTSPolygon(closePolygon (other.Points)) 
            let diff = geo1.Difference(geo2) 
            if diff = null then
                raiseException("Difference(" + this.Name + "+" + other.Name + ")" + "  without result")
            let points = FromNTSGeometry(diff)
            let name = "Difference(" + this.Name + "+" + other.Name + ")"
            new Polygon(name, points, this.Representation) 

