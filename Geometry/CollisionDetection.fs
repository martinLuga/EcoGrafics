namespace Geometry
//
//  CollisionDetection.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net
open SharpDX

open Base.Logging
open GeometricModel

module CollisionDetection = 
 
    let logger = LogManager.GetLogger("Geometric.Extensions")
    let logDebug = Debug(logger)
    let logError = Error(logger)

    let mutable maxRange = 0.0f

    // ----------------------------------------------------------------------------------------------------
    //  Intersection of GeometricTypes
    // ----------------------------------------------------------------------------------------------------
    let kugelKugel (kugel1:Kugel) (posKugel1:Vector3)(kugel2:Kugel)(posKugel2:Vector3) =
        let mutable sphere1 = BoundingSphere() 
        sphere1.Center <-  posKugel1
        sphere1.Radius <-  kugel1.Radius 
        let mutable sphere2 = BoundingSphere()
        sphere2.Center <- posKugel2
        sphere2.Radius <- kugel2.Radius         
        Collision.SphereIntersectsSphere(&sphere1, &sphere2)

    let kugelPlane (kugel1:Kugel) (posKugel1:Vector3)(plane2:QuadPlane)(posPlane2:Vector3) =
        let mutable sphere1 = BoundingSphere() 
        sphere1.Center <-  posKugel1
        sphere1.Radius <-  kugel1.Radius 
        let mutable plane1 = Plane()
        plane1.Normal <- plane2.getNormalAt(Vector3.Zero, posPlane2)    
        let result = Collision.PlaneIntersectsSphere(&plane1, &sphere1)
        result = PlaneIntersectionType.Intersecting

    let kugelCylinder (kugel1:Kugel) (posKugel1:Vector3)(cylinder2:Cylinder)(posCylinder2:Vector3) =
        let (b1min, b1max) = kugel1.Boundaries(posKugel1)
        let (b2min, b2max) = cylinder2.Boundaries(posCylinder2)             
        b1max.X < b2min.X || b2max.X < b1min.X || 
        b1max.Y < b2min.Y || b2max.Y < b1min.Y || 
        b1max.Z < b2min.Z || b2max.Z < b1min.Z 

    let boxBox (box1:Geometric) (posBox1:Vector3)(box2:Geometric)(posBox2:Vector3) =
        let mutable bb1 = box1.BoundingBox(posBox1) 
        let mutable bb2 = box2.BoundingBox(posBox2) 
        Collision.BoxIntersectsBox(&bb1, &bb2)

    let boxKugel (box:Geometric) (posBox:Vector3)(kugel:Kugel)(posKugel:Vector3) =
        let mutable bb1 = box.BoundingBox(posBox) 
        let mutable bs2 = BoundingSphere()
        bs2.Center <- posKugel
        bs2.Radius <- kugel.Radius  
        Collision.BoxIntersectsSphere(&bb1, &bs2) 

    let boxPlane (box1:Geometric) (posBox1:Vector3)(plane2:QuadPlane)(posPlane2:Vector3) =
        let mutable bb1 = box1.BoundingBox(posBox1) 
        let mutable plane1 = Plane()
        plane1.Normal <- plane2.getNormalAt(Vector3.Zero, posPlane2)
        let result =  Collision.PlaneIntersectsBox(&plane1, &bb1)
        result = PlaneIntersectionType.Intersecting

    let boxCylinder (box1:Geometric) (posBox1:Vector3)(cylinder2:Cylinder)(posCylinder2:Vector3) =
        let mutable bb1 = box1.BoundingBox(posBox1) 
        let (b2min, b2max) = cylinder2.Boundaries(posCylinder2)             
        bb1.Maximum.X < b2min.X || b2max.X < bb1.Minimum.X || 
        bb1.Maximum.Y < b2min.Y || b2max.Y < bb1.Minimum.Y || 
        bb1.Maximum.Z < b2min.Z || b2max.Z < bb1.Minimum.Z 

    let pointsAt(objectPosition:Vector3)(objectDirection:Vector3)(another:Geometric) (anotherPosition: Vector3) =
        let mutable bb = another.BoundingBox(anotherPosition) 
        let mutable ray = Ray(objectPosition, objectDirection)
        let intersects = Collision.RayIntersectsBox(&ray, &bb, &maxRange)
        intersects 

    // ----------------------------------------------------------------------------------------------------
    //  Erweiterung für Geometric für Kollisionen
    // ----------------------------------------------------------------------------------------------------
    type Geometric with

        member this.intersects (objectPosition:Vector3) (another:Geometric) (anotherPosition: Vector3) =
            match this with
            | :? Würfel | :? Quader | :? QuadPatch | :? Pyramide | :? Corpus->
                match another with
                |  :? Kugel -> 
                    boxKugel this objectPosition (another:?> Kugel) anotherPosition
                | :? Würfel | :? Quader | :? QuadPatch | :? Pyramide| :? Corpus->
                    boxBox this objectPosition another anotherPosition
                | :? QuadPlane -> 
                    boxPlane this objectPosition (another:?> QuadPlane) anotherPosition
                |  :? Cylinder-> 
                    boxCylinder this objectPosition (another:?> Cylinder) anotherPosition
                | _ -> 
                    logDebug("Collision Quader, Adobe, QuadPatch <->" + another.ToString() + " not implemented ")
                    false
            |  :? Kugel -> 
                let kugel1 = (this:?> Kugel)
                match another with
                |  :? Kugel -> 
                    kugelKugel kugel1 objectPosition (another:?> Kugel) anotherPosition         
                | :? Würfel | :? Quader | :? QuadPatch | :? Pyramide | :? Corpus->
                    boxKugel another anotherPosition kugel1 objectPosition
                | :? QuadPlane -> 
                    //logDebug("Intersect Kugel %O at %O with Plane %O at %O" kugel1 objectPosition another anotherPosition
                    kugelPlane kugel1 objectPosition (another:?> QuadPlane) anotherPosition
                |  :? Cylinder-> 
                    kugelCylinder kugel1 objectPosition (another:?> Cylinder) anotherPosition
                | _ -> 
                    logDebug("Collision Kugel <-> " + another.ToString() + " not implemented ") 
                    false 
            |  :? QuadPlane -> 
                let plane1 = (this:?> QuadPlane)
                match another with
                |  :? Kugel -> 
                    kugelPlane  (another:?> Kugel) anotherPosition plane1 objectPosition        
                | :? Würfel | :? Quader | :? QuadPatch | :? Pyramide | :? Corpus->
                    boxPlane another anotherPosition plane1 objectPosition
                | _ -> 
                    logDebug("Collision QuadPlane <-> " + another.ToString() + " not implemented ") 
                    false 
            | _ -> 
                logError("Collision for Class not implemented %O" + another.ToString())
                false 

        member this.canSee(objectPosition:Vector3)(objectDirection:Vector3)(another:Geometric) (anotherPosition: Vector3) =
            pointsAt(objectPosition:Vector3)(objectDirection:Vector3)(another:Geometric) (anotherPosition: Vector3) 