namespace ApplicationBase
//
//  MoveableObject.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net
open System
open SharpDX

open Geometry.GeometricModel
open Geometry.GeometryUtils
open Geometry.CollisionDetection
open Base.Logging

open DisplayableObject

module MoveableObject = 

    let random = Random(0)   

    let mutable OBJECT_SPEED = 0.1f
    let STANDARD_SPEED = 0.1f
    let STOPTIME: int64 = 80L  
    let TURN_AMOUNT = 0.08f         // Stärke einer Bewegungsänderung  
    let FARVALUE: float32 = 5.0f    // Abstand zwischen zwei Objekten

    let logger = LogManager.GetLogger("objects.MoveableObject")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)

    // ----------------------------------------------------------------------------------------------------
    // MOVEABLE
    // Oberklasse für alle beweglichen Objekte
    // Subklassen: 
    //  SimulationObject
    // ----------------------------------------------------------------------------------------------------
    type Moveable(name: string, geometry:Geometric, surface: Surface, color:Color , position: Vector3, direction:Vector3, speed: float32,  moveRandom: bool) =
        inherit Displayable(name, geometry, surface, color, position)
    
        static let mutable randomInterval: int64 = 10L       // Zeit zwischen 2 Richtungsänderungen bei Random         
    
        let mutable moveRandom = moveRandom
        let mutable lastPosition = Vector3.Zero
        let mutable direction = direction
        let mutable lastDirection = direction
        let mutable speed = speed
        let mutable lastSpeed = speed
        let mutable lifetime:int64 = 0L
        let mutable neartime:int64 = 0L
        let mutable stopped = false
        let mutable collides = false
        let mutable stoptime:int64 = 0L
        let mutable moveOnGround=false
        let mutable collidesWith=new Displayable()

        static let mutable randomDirectionFunc = updateDirectionRandom2

        static member RandomInterval 
            with get() = randomInterval
            and set(aValue) = randomInterval <- aValue 

        new () = Moveable("", Kugel("", 1.0f, Color.Transparent), Surface(), Color.Transparent , Vector3.Zero, Vector3.Zero, 0.0f, false)  
        new (name , geometry, surface, color, position) =  Moveable(name, geometry, surface, color, position, Vector3.Zero, 0.0f, false)  
     
        override this.ToString() = 
            name + ": " + this.Position.ToString() + " D " + this.Direction.ToString() + " S " + this.Speed.ToString()

        static member RandomDirectionFunc
            with get() = randomDirectionFunc
            and set (aValue) = randomDirectionFunc <- aValue

        override this.hits(other:Displayable) =
            let result = this.Geometry.intersects this.Position other.Geometry other.Position
            if result then                
                logInfo(this.Name + " <<<HITS>>> " + other.Name + " at " + this.Position.ToString())
            result

        member this.canSee(other:Displayable, inDirection:Vector3) =
            let result = this.Geometry.canSee this.Position inDirection other.Geometry other.Position
            if result then                
                logDebug(this.Name + " <<<SEES>>> " + other.Name)
            result

        // ----------------------------------------------------------------------------------------------------
        // Moveable: informNearTo
        // Wenn bereits in Kollision , keine weitere
        // ----------------------------------------------------------------------------------------------------
        override this.informNearTo (other:Displayable) =
            if this.Collides = false then
                this.Collides <- true
                this.CollidesWith <- other                 
                logDebug(this.Name + " COLLIDES / ON " + other.Name)
                this.doActionWith(other)
            else
                logDebug(this.Name + " DOUBLE COLLIDES WITH " + other.Name)

        override this.informFarTo (other:Displayable) =
            if this.Collides = true && other = this.CollidesWith then
                this.Collides <- false
                logDebug(this.Name + " COLLIDES / OFF " + other.Name)

        member this.MoveOnGround
            with get() = moveOnGround
            and set (aValue) = moveOnGround <- aValue

        member this.LastPosition 
            with get() = lastPosition
            and set (aValue) = lastPosition <- aValue

        member this.Direction 
            with get() = direction
            and set (aValue:Vector3) = 
                direction <- aValue
                direction.Normalize()

        member this.LastDirection
            with get() = lastDirection
            and set (aValue) = 
                lastDirection <- aValue
                lastDirection.Normalize()

        member this.Speed
            with get() = speed
            and set (aValue) = speed <- aValue

        member this.LastSpeed
            with get() = lastSpeed
            and set (aValue) = lastSpeed <- aValue

        member this.Lifetime
            with get() = lifetime
            and set (aValue) = lifetime <- aValue

        member this.Neartime 
            with get() = neartime
            and set (aValue) = neartime <- aValue
        
        member this.Stopped 
            with get() = stopped
            and set (aValue) = stopped <- aValue
        
        member this.Stoptime 
            with get() = stoptime
            and set (aValue) = stoptime <- aValue

        member this.Collides 
            with get() = collides
            and set (aValue) = collides <- aValue

        member this.CollidesWith
            with get() = collidesWith
            and set (aValue) = collidesWith <- aValue
 
        abstract member proceed : unit -> unit
        default this.proceed() =
            this.Speed <- this.LastSpeed
            if this.Speed = 0.0f then
               this.Speed <- OBJECT_SPEED 
            this.Stopped <- false
            this.Stoptime <- 0L

        member this.stepAside() =
            this.LastDirection <- this.Direction
            let mutable newDirection =  this.Direction
            newDirection.X <- newDirection.X + 0.5f
            this.Direction <- newDirection

        abstract member groundPositionAt : Vector3 -> Vector3
        default this.groundPositionAt(position:Vector3) =
            Vector3(position.X, position.X, position.Z)

        // ----------------------------------------------------------------------------------------------------
        // Moveable: doActionWith
        //  default: Reflektieren
        // ----------------------------------------------------------------------------------------------------
        abstract doActionWith: Displayable -> unit

        default this.doActionWith (other:Displayable) =   
            if other.isPermeable() then 
                ()
            else this.reflect(other)

        // ----------------------------------------------------------------------------------------------------
        // Moveable: Motion
        // Steuerung der Bewegung über Direction Position, Speed
        // ----------------------------------------------------------------------------------------------------
        member this.Motion(time: int64) =  

            this.Lifetime <- time
            
            logInfo("Move " + this.Name + " at " + time.ToString())

            if moveRandom && lifetime%Moveable.RandomInterval = 0L then
               this.updateDirectionRandom()

            this.computePosition()

            //this.stayInBounds()            

            let stopped = this.Stoptime - this.Lifetime 
            if stopped <> (- this.Lifetime) then
    
                match stopped with
                | s when s > 0L ->          //  Noch gestoppt
                    ()
                | s when s <= 0L ->         //  Läuft genau jetzt ab
                    this.proceed()
                | _ -> ()
 

        // Active movement
        member this.MoveRandom
            with get() = moveRandom
            and set (aValue) = moveRandom <- aValue

        override this.move(newDirection:Vector3) (newSpeed:float32) =
            this.Direction <- newDirection
            this.Speed <- newSpeed

        override this.stop() = 
            this.LastSpeed <- this.Speed
            this.Speed <- 0.0f
            this.Stopped <- true

        member this.stop(stoptime) =
            this.Stoptime <- this.Lifetime + stoptime
            printfn "Gestoppt %O bis %O" this.Name this.Stoptime
            base.stop()

        member this.updateDirection (newDirection) =    
            if not this.Collides then    
                logDebug(this.Name + "<<<UPDATEDIR>>> TO " + newDirection.ToString())
                this.Direction <- newDirection

        member this.updateDirectionRandom() =
            let dir = Moveable.RandomDirectionFunc random direction 
            this.updateDirection(dir)            

        member this.invertDirection() =
            this.Direction <- this.Direction * -1.0f     

        override this.isMoveable() = true

        member this.computePosition() =
            this.LastPosition <- this.Position
            let mutable pos = this.Position + this.Direction * this.Speed 
            if moveOnGround then
                let groundPosition = this.groundPositionAt(this.Position)
                pos.Y <- groundPosition.Y + (this.height / 2.0f) + 0.2f 
            this.Position <-  pos               
        
        // Nicht mehr benutzt, nur zur Übung,  ist bei Vector3 vorhanden
        member this.reflectAtNormal (normal:Vector3) =
            let scalarP = Vector3.Dot(normal, this.Direction)
            let phi = (2.0f * scalarP) * normal
            let result = (this.Direction - phi)  
            result              
    
        // Normale am Treffpunkt bilden
        // Einen Schritt zurück 
        member this.reflect(other:Displayable) =
            let anotherNormal = other.getNormalAt(this.hitPoint(other))
            logDebug(this.Name + "<<<REFLECTED at " + other.Name + " Normal= " + anotherNormal.ToString())
            this.Direction <- Vector3.Reflect(this.Direction, anotherNormal)
            this.Position <- this.LastPosition 
        
        member this.gotoPosition(targetPosition) = 
            this.LastPosition <- this.Position 
            this.Direction <- targetPosition - this.Position 

        // Von der aktuellen Richtung 
        // in eine Zielrichtung bewegen (amount ist die Stärke der Änderung
        member this.turnToPosition(targetPosition) = 
            let mutable sourceDir = this.Direction
            let mutable targetDir = targetPosition - this.Position
            let mutable nextDir = Vector3.Zero
            Vector3.SmoothStep(&sourceDir, &targetDir, TURN_AMOUNT, &nextDir)
            this.Direction <- nextDir
    
        member this.goto(another:Displayable) =
            this.gotoPosition(another.Center)   

        member this.turnTo(another:Displayable) =
            this.turnToPosition(another.Center)

        member this.hasReached(another:Displayable) =
            this.Geometry.intersects this.Position another.Geometry another.Position


    type Immoveable(name: string, geometry:Geometric, surface: Surface, color:Color, position: Vector3) =
        inherit Displayable(name, geometry, surface, color, position)
        new(name, geometry, color, position) = Immoveable(name, geometry, new Surface() , color, position)
        new() = Immoveable("", Kugel("", 1.0f, Color.Transparent), Surface(), Color.Transparent , Vector3.Zero)  
        
        override this.isMoveable() = false

        override this.hits(other:Displayable) =
            this.Geometry.intersects this.Position other.Geometry other.Position

        override this.ToString() = 
            name + ": " + this.Position.ToString() 
