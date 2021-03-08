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

    exception CollisionException of string
 
    let logger = LogManager.GetLogger("Geometric.Extensions")
    let logDebug = Debug(logger)
    let logError = Error(logger)

    let mutable maxRange = 0.0f
    let mutable result = false

    type CollisionState = 
        {closest: Vector3;  collides: bool; distance:float32}

    /// <summary>
    /// Intersection of GeometricTypes
    /// </summary>

    /// <summary>
    /// Kugel mit Kugel
    //  Point of intersection
    //  Distance between center and center
    //  True, if intersection
    /// </summary>
    let kugelKugel (kugel1:Kugel) (posKugel1:Vector3)(kugel2:Kugel)(posKugel2:Vector3) =
        let mutable sphere1 = BoundingSphere( posKugel1, kugel1.Radius )
        let mutable sphere2 = BoundingSphere()
        sphere2.Center <- posKugel2
        sphere2.Radius <- kugel2.Radius 
        let mutable closest = Vector3.Zero
        Collision.ClosestPointSphereSphere (&sphere1, &sphere2, &closest) 
        let result = Collision.SphereIntersectsSphere(&sphere1, &sphere2)
        let distance = Collision.DistanceSphereSphere(&sphere1, &sphere2)
        let state = {
            collides = result;
            distance = distance;
            closest = closest; 
            }
        state

    /// <summary>
    /// Kugel mit Plane
    /// Point of intersection
    /// Distance between center and center
    /// True, if intersection
    /// </summary>
    /// <param name="kugel">Kugel </param>
    /// <param name="posKugel">Position der Kugel</param>
    /// <param name="plane">Eine Plane</param>
    /// <param name="posPlane">Position der Plane</param>
    let kugelPlane (kugel:Kugel) (posKugel:Vector3)(plane:QuadPlane)(posPlane:Vector3) =
        let mutable sphere1 = BoundingSphere(posKugel, kugel.Radius) 
        let mutable plane1 = Plane()  
        plane1.Normal <- plane.getNormalAt(Vector3.Zero, posPlane)    
        let result = Collision.PlaneIntersectsSphere(&plane1, &sphere1)
        let mutable closest = Vector3.Zero
        Collision.ClosestPointPlanePoint(&plane1, &sphere1.Center, &closest)
        let distance = Collision.DistancePlanePoint(&plane1, &closest)
        let state = {
            collides = (result = PlaneIntersectionType.Intersecting)
            distance = distance;
            closest = closest;
            }
        state  

    let kugelCylinder (kugel:Kugel) (posKugel:Vector3)(cylinder:Cylinder)(posCylinder:Vector3) =
        let mutable bs2 = BoundingSphere(posKugel, kugel.Radius)
        let mutable bb = cylinder.BoundingBox(posCylinder)  
        let mutable closest = Vector3.Zero
        let result = Collision.BoxIntersectsSphere(&bb, &bs2)
        let distance = Collision.DistanceSpherePoint(&bs2, &closest)
        let state = {
                distance = distance;
                closest = closest;
                collides = result 
            }
        state  

    let boxBox (box1:Geometric) (posBox1:Vector3)(box2:Geometric)(posBox2:Vector3) =
        let mutable bb1 = box1.BoundingBox(posBox1) 
        let mutable bb2 = box2.BoundingBox(posBox2) 
        let mutable center = box1.Center
        let mutable closest = Vector3.Zero
        Collision.ClosestPointBoxPoint(&bb2, &center, &closest)
        let result = Collision.BoxIntersectsBox(&bb1, &bb2)
        let distance = Collision.DistanceBoxPoint(&bb1, &closest)
        let state = {
                distance = distance;
                closest = closest;
                collides = result
            }
        state   

    let boxKugel (box:Geometric) (posBox:Vector3)(kugel:Kugel)(posKugel:Vector3) =
        let mutable bb = box.BoundingBox(posBox) 
        let mutable bs = BoundingSphere(kugel.CenterAtPosition(posKugel), kugel.Radius)
        let mutable closest = Vector3.Zero
        Collision.ClosestPointBoxPoint(&bb, &bs.Center, &closest)
        let result = Collision.BoxIntersectsSphere(&bb, &bs) 
        let distance = Collision.DistanceSpherePoint(&bs,  &closest)
        let state = {
                distance = distance;
                closest = closest;
                collides = result
            }
        state   

    let kugelBox(kugel:Kugel)(posKugel:Vector3)(box:Geometric) (posBox:Vector3) =
        let mutable bb = box.BoundingBox(posBox) 
        let mutable bs = BoundingSphere(kugel.CenterAtPosition(posKugel), kugel.Radius)
        let mutable closest = Vector3.Zero
        Collision.ClosestPointBoxPoint(&bb, &bs.Center, &closest)
        let result = Collision.BoxIntersectsSphere(&bb, &bs) 
        let distance = Collision.DistanceSpherePoint(&bs,  &closest)
        let state = {
                collides = result;
                closest = closest;
                distance = distance
            }
        state  

    let boxPlane (box1:Geometric) (posBox1:Vector3)(plane2:QuadPlane)(posPlane2:Vector3) =
        let mutable bb1 = box1.BoundingBox(posBox1) 
        let mutable plane1 = Plane()
        plane1.Normal <- plane2.getNormalAt(Vector3.Zero, posPlane2)
        let result =  Collision.PlaneIntersectsBox(&plane1, &bb1)
        let mutable closest = Vector3.Zero
        let distance = Collision.DistanceBoxPoint(&bb1,  &closest)
        let state = {
                distance = distance;
                closest = closest;
                collides = (result = PlaneIntersectionType.Intersecting)
            }
        state 

    let boxCylinder (box1:Geometric) (posBox1:Vector3)(cylinder2:Cylinder)(posCylinder2:Vector3) =
        let mutable bb1 = box1.BoundingBox(posBox1) 
        let (b2min, b2max) = cylinder2.Boundaries(posCylinder2)  
        let mutable closest = Vector3.Zero           
        let result =  
            bb1.Maximum.X < b2min.X || b2max.X < bb1.Minimum.X || 
            bb1.Maximum.Y < b2min.Y || b2max.Y < bb1.Minimum.Y || 
            bb1.Maximum.Z < b2min.Z || b2max.Z < bb1.Minimum.Z 

        let distance = Collision.DistanceBoxPoint(&bb1,  &closest)
        let state = {
                collides = result; 
                distance = distance;
                closest = closest; 
            }
        state 

    let pointsAt(objectPosition:Vector3)(objectDirection:Vector3)(another:Geometric) (anotherPosition: Vector3) =
        let mutable bb = another.BoundingBox(anotherPosition) 
        let mutable ray = Ray(objectPosition, objectDirection)
        let intersects = Collision.RayIntersectsBox(&ray, &bb, &maxRange)
        intersects 

    /// <summary>
    //  Erweiterung für Geometric für Kollisionen
    /// <summary>
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
                    logError("Collision Quader, Adobe, QuadPatch <->" + another.ToString() + " not implemented ")
                    raise (CollisionException("Collision not implemented"))
            |  :? Kugel -> 
                let kugel1 = (this:?> Kugel)
                match another with
                |  :? Kugel -> 
                    kugelKugel kugel1 objectPosition (another:?> Kugel) anotherPosition         
                | :? Würfel | :? Quader | :? QuadPatch | :? Pyramide | :? Corpus->
                    kugelBox  kugel1 objectPosition another anotherPosition
                | :? QuadPlane -> 
                    //logDebug("Intersect Kugel %O at %O with Plane %O at %O" kugel1 objectPosition another anotherPosition
                    kugelPlane kugel1 objectPosition (another:?> QuadPlane) anotherPosition
                |  :? Cylinder-> 
                    kugelCylinder kugel1 objectPosition (another:?> Cylinder) anotherPosition
                | _ -> 
                    logError("Collision Kugel <-> " + another.ToString() + " not implemented ") 
                    raise (CollisionException("Collision not implemented")) 
            // TODO Reihenfolge Parameter  : planeKugel etc
            |  :? QuadPlane -> 
                let plane1 = (this:?> QuadPlane)
                match another with
                |  :? Kugel -> 
                    kugelPlane  (another:?> Kugel) anotherPosition plane1 objectPosition        
                | :? Würfel | :? Quader | :? QuadPatch | :? Pyramide | :? Corpus->
                    boxPlane another anotherPosition plane1 objectPosition
                | _ -> 
                    logDebug("Collision QuadPlane <-> " + another.ToString() + " not implemented ") 
                    raise (CollisionException("Collision not implemented")) 
            | _ -> 
                logError("Collision for Class not implemented %O" + another.ToString())
                raise (CollisionException("Collision not implemented")) 

        member this.canSee(objectPosition:Vector3)(objectDirection:Vector3)(another:Geometric) (anotherPosition: Vector3) =
            pointsAt(objectPosition:Vector3)(objectDirection:Vector3)(another:Geometric) (anotherPosition: Vector3) 