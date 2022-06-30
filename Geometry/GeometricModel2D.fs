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
open Base.VertexDefs
open Base.ObjectBase

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
        let mutable points = points

        new (name, geo:NTSMultiPoint, representation) = new Geometry2D (name, FromMultiPoints(geo), Color.White, representation)
        new (name, geo:NTSMultiPoint, color) = new Geometry2D (name, FromMultiPoints(geo),color , Representation.Surface)
        new (name, geo:NTSMultiPoint) = new Geometry2D (name, FromMultiPoints(geo), Color.White, Representation.Surface)

        abstract member Points: Vector3 []
        default this.Points = points 

        override this.Center =
            try
                let polygon = closePolygon(this.Points)
                calcCentroid(polygon)
            with :? CreateException as ex ->
                raiseException("Center of " + this.Name + " invalid: " + ex.Data0)

        member this.Union(other: Geometry2D) =
            let geo1 = AsMultiPoints(this.Points)
            let geo2 = AsMultiPoints(other.Points)
            let union = geo1.Union(geo2) :?> NTSMultiPoint
            if union = null then
                raiseException("Union(" + this.Name + "+" + other.Name + ")" + "  without result")
            this.AsPolygon(
                "Union(" + this.Name + "+" + other.Name + ")",
                union)

        member this.Intersection(other: Geometry2D) =
            let geo1 = AsMultiPoints(this.Points)
            let geo2 = AsMultiPoints(other.Points)
            let inter = geo1.Intersection(geo2) :?> NTSMultiPoint
            if inter = null then
                raiseException("Intersection(" + this.Name + "+" + other.Name + ")" + "  without result")
            this.AsPolygon(            
                "Intersection(" + this.Name + "+" + other.Name + ")",
                inter)

        member this.Difference(other: Geometry2D) =
            let geo1 = AsMultiPoints(this.Points)
            let geo2 = AsMultiPoints(other.Points)
            let diff = geo1.Difference(geo2) :?> NTSMultiPoint
            if diff = null then
                raiseException("Difference(" + this.Name + "+" + other.Name + ")" + "  without result")
            this.AsPolygon(            
                "Difference(" + this.Name + "+" + other.Name + ")",
                diff)

        member this.Alone( ) =
            let geo1 = AsMultiPoints(this.Points)  
            this.AsPolygon("Polygon(" + this.Name + ")",
                geo1)

        member this.AsBaseObject (name, position, material, texture) =
            new BaseObject(
                name = name,
                display =
                    new Display(
                    parts =
                        [ new Part(
                              name = name,
                              shape = this,
                              material = material,
                              texture = texture,
                              visibility = Visibility.Opaque
                          ) ]
                    ),
                position = position
            )

        member this.Representation
            with get() = representation
            and set(value) = representation <- value

        abstract member AsPolygon: string*NTSMultiPoint->Geometry2D

        abstract member CreateSurfaceVertexData: Visibility->List<Vertex>*List<int>
        default this.CreateSurfaceVertexData(visibility: Visibility) =
            raiseException("Not implemented")

        abstract member CreateLineVertexData: Visibility->List<Vertex>*List<int>
        default this.CreateLineVertexData(visibility: Visibility) =
            this.CreateSurfaceVertexData(visibility: Visibility)

        abstract member CreatePointVertexData: Visibility->List<Vertex>*List<int>
        default this.CreatePointVertexData(visibility: Visibility) =
            this.CreateSurfaceVertexData(visibility: Visibility)

        override this.CreateVertexData(visibility: Visibility) =
            match this.Representation with
            | Representation.Surface -> 
                this.Topology <- PrimitiveTopology.TriangleList
                this.TopologyType <- PrimitiveTopologyType.Triangle
                let vertices, indices = this.CreateSurfaceVertexData(visibility)
                MeshData.Create(vertices, indices )
                
            | Representation.Line -> 
                this.Topology <- PrimitiveTopology.LineList
                this.TopologyType <- PrimitiveTopologyType.Line
                let vertices, indices = this.CreateLineVertexData(visibility)
                MeshData.Create(vertices, indices )

            | Representation.Point -> 
                this.Topology <- PrimitiveTopology.PointList
                this.TopologyType <- PrimitiveTopologyType.Point
                let vertices, indices = this.CreatePointVertexData(visibility)
                MeshData.Create(vertices, indices )           

    // ----------------------------------------------------------------------------------------------------
    // Polygon (Kontur)
    // ----------------------------------------------------------------------------------------------------
    type Polygon(name: string, points, representation) =
        inherit Geometry2D(name, points, Color.White, representation)        
        let mutable vertices = new List<Vertex>()
        let mutable indices = new List<int>()
 
        new (name, geo:NTSMultiPoint) = new Polygon (name, FromMultiPoints(geo), Representation.Surface)
        new (name: string, points) = new Polygon (name, points, Representation.Surface)

        override this.Center =
            try
                calcCentroid(this.Points)
            with :? CreateException as ex ->
                raiseException("Center of " + this.Name + " invalid: " + ex.Data0)

        override this.Vertices
            with get() = 
                let (_vertices , _indices) = PolygonPatch.CreateVertexData(this.Center, points, Color.White, Visibility.Opaque)
                vertices <- _vertices
                vertices

        override this.Indices
            with get() = 
                let (_vertices , _indices) = PolygonPatch.CreateVertexData(this.Center, points, Color.White, Visibility.Opaque) 
                indices <- _indices
                indices

        override this.AsPolygon(name, plist) = 
            let points = FromMultiPoints(plist)
            new Polygon(name, points, this.Representation)
        
        override this.ToString() = "Polygon:" + this.Name 

        override this.CreateSurfaceVertexData(visibility: Visibility) =
            PolygonPatch.CreateVertexData(this.Center, points, this.Color, visibility)

        override this.CreateLineVertexData(visibility: Visibility) =
            PolygonPatch.CreateVertexData(this.Center, points, this.Color, visibility)

        override this.CreatePointVertexData(visibility: Visibility) =
            PolygonPatch.CreateVertexData(this.Center, points, this.Color, visibility)
            
    // ----------------------------------------------------------------------------------------------------
    // Kreis
    // ----------------------------------------------------------------------------------------------------
    type Kreis(name: string, origin, radius: float32, color: Color, representation) =
        inherit Geometry2D(name, [|origin|], color, representation)   
        let radius = radius 
        let origin = origin 
        let mutable points: Vector3 [] = [||]
        let mutable vertices = new List<Vertex>()
        let mutable indices = new List<int>()
        do
            points <- Circle2D.CreatePointData(origin, color, radius, Shape.Raster, Visibility.Opaque)|> Seq.toArray
            let (_vertices , _indices) = PolygonPatch.CreateVertexData(origin, points, color, Visibility.Opaque) 
            vertices <- _vertices
            indices <- _indices
        
        new (name, origin, radius, color) = new Kreis (name, origin, radius, color, Representation.Surface)

        member this.Radius  
            with get() = radius

        override this.Points =
            points

        override this.Vertices
            with get() = vertices

        override this.Indices
            with get() = indices

        override this.AsPolygon(name, plist) = 
            let points = FromMultiPoints(plist)            
            new Polygon(name, invertFace(points))

        override this.ToString() = "Kreis:" + this.Name + " r " + radius.ToString() 

        override this.CreateSurfaceVertexData(visibility: Visibility) =
            let points = invertFace(closePolygon(points))
            PolygonPatch.CreateVertexData(this.Center, points, this.Color, visibility)  

    // ----------------------------------------------------------------------------------------------------
    // Rechteck
    // ----------------------------------------------------------------------------------------------------
    type Rechteck(name: string, origin: Vector3, laenge: float32, hoehe: float32, color: Color, representation) =
        inherit Geometry2D(name, [|origin|], color, representation)  
        let p1 = origin
        let p2 = new Vector3(p1.X + laenge, 0.0f,   p1.Z )
        let p3 = new Vector3(p1.X + laenge, 0.0f,   p1.Z + hoehe)
        let p4 = new Vector3(p1.X ,         0.0f,   p1.Z + hoehe)

        new (name: string, origin: Vector3, laenge: float32, hoehe: float32, color:Color) = 
            new Rechteck(name , origin , laenge , hoehe , color , Representation.Surface)

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
        
        override this.CreateSurfaceVertexData(visibility: Visibility) =
            Square2D.CreateVertexData(p1, p4, p3, p2,  color, visibility, Quality.High)
