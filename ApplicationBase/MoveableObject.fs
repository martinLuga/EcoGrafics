namespace ApplicationBase
//
//  MoveableObject.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open System 
open System.Threading
open System.Diagnostics

open SharpDX

open Geometry.GeometricModel
open Geometry.GeometryUtils
open Geometry.CollisionDetection

open Base.Logging
open Base.GlobalDefs

open DisplayableObject

module MoveableObject = 

    let random = Random(0)   
    let clock = new Stopwatch()

    let mutable OBJECT_VELOCITY = 0.1f
    let VELOCITY_STANDARD = 0.1f
    let VELOCITY_STANDSTILL = 0.0f
    let STOPTIME: int64 = 80L  
    let TURN_AMOUNT = 0.08f             // Stärke einer Bewegungsänderung  
    let FARVALUE: float32 = 5.0f        // Abstand zwischen zwei Objekten
    let NEAR_DISTANCE: float32 = 2.0f   // Objekte sind nah, wenn Abstand < NEAR_DISTANCE

    let logger = LogManager.GetLogger("objects.MoveableObject")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)
    let logWarn  = Warn(logger)

    let formatVector(v:Vector3) =
        let xs = sprintf "%4.2f" v.X
        let ys = sprintf "%4.2f" v.Y
        let zs = sprintf "%4.2f" v.Z
        "(" + xs + "," + ys  + "," + zs + ")"

    // ----------------------------------------------------------------------------------------------------
    // MOVEABLE
    // Oberklasse für alle beweglichen Objekte
    // Subklassen: 
    //  SimulationObject
    // ----------------------------------------------------------------------------------------------------
    type Moveable(name: string, geometry:Geometric, surface: Surface, color:Color , position: Vector3, direction:Vector3, velocity: float32,  moveRandom: bool) =
        inherit Displayable(name, geometry, surface, color, position)
    
        static let mutable randomInterval: int64 = 10L       // Zeit zwischen 2 Richtungsänderungen bei Random         
    
        let mutable moveRandom = moveRandom
        let mutable lastPosition = position
        let mutable maxStep = 0.1f
        let mutable direction = direction
        let mutable lastDirection = direction
        let mutable velocity = velocity
        let mutable lastVelocity = velocity
        let mutable lifetime:int64 = 0L
        let mutable neartime:int64 = 0L
        let mutable stopped = false
        let mutable collides = false
        let mutable stoptime:int64 = 0L
        let mutable moveOnGround=false
        let mutable collidesWith=null
        let mutable collisionState = {collides=false; closest=Vector3.Zero}
        let mutable cancelWorkflow = new CancellationTokenSource()
        let mutable workflowActive = false
        let mutex = new Mutex(false, name)
        let u_mutex = new Mutex(true, name)

        // 
        // Motion 
        // 
        let motionWorkflow(mov:Moveable)  = async {
            let ID = System.DateTime.Now.ToString() 
            logInfo("Motion WF started for "  + mov.Name + formatVector(mov.Position)) 
            clock.Reset()
            while true do            
                do! Async.Sleep 1
                mov.Move(clock.ElapsedMilliseconds)
        }

        static let mutable randomDirectionFunc = updateDirectionRandom2

        static member RandomInterval 
            with get() = randomInterval
            and set(aValue) = randomInterval <- aValue 
            
        static member RandomDirectionFunc
            with get() = randomDirectionFunc
            and set (aValue) = randomDirectionFunc <- aValue

        new () = Moveable("", Kugel("", 1.0f, Color.Transparent), Surface(), Color.Transparent , Vector3.Zero, Vector3.Zero, 0.0f, false)  
        new (name , geometry, surface, color, position) =  Moveable(name, geometry, surface, color, position, Vector3.Zero, 0.0f, false)  
     
        override this.ToString() = 
            name + formatVector(this.Position) + "/" + formatVector(this.Direction) + "/" + this.Velocity.ToString()

        override this.isMoving() =
            this.Velocity > 0.0f

        override this.hitPoint(someDisplayable:Displayable) =
            collisionState.closest

        member this.canSee(other:Displayable, inDirection:Vector3) =
            let result = this.Geometry.canSee this.Position inDirection other.Geometry other.Position
            if result then                
                logDebug(this.Name + " <<<SEES>>> " + other.Name)
            result

        member this.MotionWorkflow =
            motionWorkflow(this)

        member this.Mutex 
            with get() = mutex
        
        member this.U_Mutex 
            with get() = u_mutex

        member this.WorkflowActive 
            with get() = workflowActive
            and set(value) = workflowActive <- value


        member this.ActivateWorkflow() =
            logWarn(this.Name + " Activate WF: " + this.Name)
            workflowActive <- true

        member this.DeactivateWorkflow() =
            logWarn(this.Name + " Deactivate WF: " + this.Name)
            workflowActive <- false

        // ----------------------------------------------------------------------------------------------------
        // Kollisions-Logik
        // ----------------------------------------------------------------------------------------------------
        // Hits         : Stellt fest, ob das Moveable ein Displayable berührt 
        // IsColliding : Entsprechende Aktion, im allgemeinen reflect
        //              : Wenn bereits in Kollision , keine weitere
        //              : doActionWith
        //              : default: Reflektieren
        // ----------------------------------------------------------------------------------------------------
        override this.CheckNear(other:Displayable) =
            logWarn(this.Name + " CheckNear waiting: " + other.Name)
            lock this.Mutex (fun () ->
                collisionState <- this.Geometry.intersects this.Position other.Geometry other.Position
                let distance = Vector3.Distance(this.Position, collisionState.closest)
                if collisionState.collides then 
                    logInfo(this.Name + " - Collision detected with " + other.Name + " at " + formatVector(collisionState.closest))          
                    this.IsColliding(other)
                else   
                    this.informFarTo(other)
            )  
            logWarn(this.Name + " CheckNear done: " + other.Name)   

        override this.hits(other:Displayable) =
            let state = this.Geometry.intersects this.Position other.Geometry other.Position
            if state.collides then                
                logInfo(this.Name + " <<<HITS>>> " + other.Name + " at " + formatVector(collisionState.closest))
            state.collides

        override this.IsColliding (other:Displayable) =
            if this.InCollision = false then
                logInfo(this.ToString() + " --- Collision Start " + other.Name + " at " + formatVector(collisionState.closest))
                this.InCollision <- true
                this.CollidesWith <- other    
                this.doActionWith(other)
            else
                if this.CollidesWith = other then 
                    logInfo(this.ToString() + " --- Still Colliding with " + other.Name)
                    ()
                else 
                    logWarn(this.Name + " --- Collision with other " + other.Name + " !!!")
                    
            logInfo(this.ToString() + " --- Collision with " + other.Name + " ended")
            this.ResetCollision() // HACK

        member this.ResetCollision() =
            this.InCollision <- false
            this.CollidesWith <- null   

        override this.informFarTo (other:Displayable) = 
            if this.InCollision = true && other = this.CollidesWith then
                logDebug(this.Name + "--- farTo " + other.Name  )
                this.InCollision <- false
                this.CollidesWith <- null

        abstract doActionWith: Displayable -> unit
        default this.doActionWith (other:Displayable) =  
            //logDebug(this.Name + " --- Action " + other.Name  ) 
            if other.isPermeable() then 
                logDebug(this.Name + " <--- Is permeable " + other.Name + " Do Nothing= " )
            else this.reflect(other)

        // ----------------------------------------------------------------------------------------------------
        // Move
        // ----------------------------------------------------------------------------------------------------
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

        member this.LastSpeed
            with get() = lastVelocity
            and set (aValue) = lastVelocity <- aValue
        
        member this.Velocity
            with get() = velocity
            and set (aValue) = velocity <- aValue

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

        member this.InCollision
            with get() = collides
            and set (aValue) = collides <- aValue

        member this.CollidesWith
            with get() = collidesWith
            and set (aValue) = collidesWith <- aValue
 
        abstract member proceed : unit -> unit
        default this.proceed() =
            this.Velocity <- this.LastSpeed
            if this.Velocity = 0.0f then
               this.Velocity <- OBJECT_VELOCITY 
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
        // Moveable: Motion
        // Steuerung der Bewegung über Direction Position, Speed
        // ----------------------------------------------------------------------------------------------------
        member this.Move(time: int64) = 
            
            logInfo(this.Name + " - Move Waiting ... ")
            lock this.Mutex (fun () ->

                this.Lifetime <- time

                if moveRandom && lifetime%Moveable.RandomInterval = 0L then
                   this.updateDirectionRandom()

                this.computePosition()

                logDebug(this.ToString() + " moved ")

                let stopped = this.Stoptime - this.Lifetime 
                if stopped <> (- this.Lifetime) then
    
                    match stopped with
                    | s when s > 0L ->          //  Noch gestoppt
                        ()
                    | s when s <= 0L ->         //  Läuft genau jetzt ab
                        this.proceed()
                    | _ -> ()
            )

        // Active movement
        member this.MoveRandom
            with get() = moveRandom
            and set (aValue) = moveRandom <- aValue

        override this.MoveDirection(newDirection:Vector3) (newSpeed:float32) =
            this.Direction <- newDirection
            this.Velocity <- newSpeed

        override this.stop() = 
            this.LastSpeed <- this.Velocity
            this.Velocity <- 0.0f
            this.Stopped <- true

        member this.stop(stoptime) =
            this.Stoptime <- this.Lifetime + stoptime
            printfn "Gestoppt %O bis %O" this.Name this.Stoptime
            base.stop()

        member this.updateDirection (newDirection) =    
            if not this.InCollision then    
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
            let mutable pos = this.Position + this.Direction * this.Velocity 
            if moveOnGround then
                let groundPosition = this.groundPositionAt(this.Position)
                pos.Y <- groundPosition.Y + (this.height / 2.0f) + 0.2f 
            this.Position <-  pos               
        
        // Normale am Treffpunkt bilden
        member this.reflect(other:Displayable) =
            let anotherNormal = other.getNormalAt(this.hitPoint(other))
            logInfo(this.ToString() + " --- refl at " + other.Name + " N= " + formatVector(anotherNormal))
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
            let state = this.Geometry.intersects this.Position another.Geometry another.Position
            state.collides

        member this.startWorkflow() =  
            cancelWorkflow <- new CancellationTokenSource() 
            let starteable = motionWorkflow this
            Async.Start(starteable, cancelWorkflow.Token)
            workflowActive <- true

    type Immoveable(name: string, geometry:Geometric, surface: Surface, color:Color, position: Vector3) =
        inherit Displayable(name, geometry, surface, color, position)
        new(name, geometry, color, position) = Immoveable(name, geometry, new Surface() , color, position)
        new() = Immoveable("", Kugel("", 1.0f, Color.Transparent), Surface(), Color.Transparent , Vector3.Zero)  
        
        override this.isMoveable() = false

        override this.hits(other:Displayable) =
            let state = this.Geometry.intersects this.Position other.Geometry other.Position
            state.collides

        override this.ToString() = 
            name + ": " + this.Position.ToString() 
