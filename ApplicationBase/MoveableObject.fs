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

open Base.Logging

open Geometry.GeometricModel
open Geometry.GeometryUtils
open Geometry.CollisionDetection

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
    let DEF_RANDOM_INTERVAL = 4000L 

    let logger = LogManager.GetLogger("objects.MoveableObject")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)
    let logWarn  = Warn(logger)

    let formatVector(v:Vector3) =
        let xs = sprintf "%4.2f" v.X
        let ys = sprintf "%4.2f" v.Y
        let zs = sprintf "%4.2f" v.Z
        "(" + xs + "," + ys  + "," + zs + ")"

    type Motion =
       struct 
           val position:  Vector3 
           val direction: Vector3 
           val velocity : float32 
           val randMove : bool
           new (position:  Vector3, direction: Vector3 , velocity : float32, randMove : bool) = { position = position; direction = direction; velocity = velocity; randMove = randMove}
           new (position:  Vector3, direction: Vector3 , velocity : float32) = { position = position; direction = direction; velocity = velocity; randMove = false}
           override this.ToString() = "Motion(" + this.position.ToString() + "|" + this.direction.ToString()+ "|" + this.velocity.ToString() + ")" 
       end

    /// <summary>
    /// Oberklasse für alle beweglichen Objekte
    /// Subklassen: 
    /// SimulationObject
    /// </summary>
    type Moveable(name: string, geometry:Geometric, surface: Surface, color:Color , position: Vector3, direction:Vector3, velocity: float32,  moveRandom: bool) =
        inherit Displayable(name, geometry, surface, color, position)
    
        static let mutable randomInterval: int64 = DEF_RANDOM_INTERVAL       // Zeit zwischen 2 Richtungsänderungen bei Random         
    
        let mutable moveRandom = moveRandom
        let mutable lastPosition = position
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
        let mutable cancelWorkflow = new CancellationTokenSource()
        let mutable workflowActive = false
        let mutex = new Mutex() 
 
        /// <summary>
        /// Motion WF
        /// </summary>
        let motionWorkflow(mov:Moveable)  = async {
            let ID = System.DateTime.Now.ToString() 
            logInfo("Motion WF started for "  + mov.Name + formatVector(mov.Position)) 
            clock.Reset()
            while true do            
                do! Async.Sleep 1
                mov.Move(clock.ElapsedMilliseconds)
        }

        static let mutable randomDirectionFunc = updateDirectionRandom
         
        static member RandomInterval 
             with get() = randomInterval
             and set(aValue) = randomInterval <- aValue 

        static member RandomDirectionFunc
            with get() = randomDirectionFunc
            and set (aValue) = randomDirectionFunc <- aValue

        new () = Moveable("", Kugel("", 1.0f, Color.Transparent), Surface(), Color.Transparent , Vector3.Zero, Vector3.Zero, 0.0f, false)  
        new (name , geometry, surface, color, position) =  Moveable(name, geometry, surface, color, position, Vector3.Zero, 0.0f, false)  

        override this.isMoveable() = true
     
        override this.ToString() = 
            name + formatVector(this.Position) + "/" + formatVector(this.Direction) + "/" + this.Velocity.ToString()

        override this.isMoving() =
            this.Velocity > 0.0f

        override this.hits(other:Displayable) =
            let state = this.Geometry.intersects this.Position other.Geometry other.Position
            if state.collides then                
                logInfo(this.Name + " <<<HITS>>> " + other.Name + " at " + this.Position.ToString())
            state.collides

        /// <summary>
        /// Kollisions-Logik: Stellt fest, ob das Moveable ein Displayable berührt 
        /// IsColliding : Entsprechende Aktion, doActionWith : default: Reflektieren
        /// 
        /// Darf nicht concurrent laufen, deshalb mutex
        /// Wird in folgenden Threads aufgerufen
        ///     SimulationSystem umgebungWorkflow        
        ///     Umgebung collisionWorkflow
        /// </summary>
        override this.CheckNear(other:Displayable) =
            let collisionState = this.Geometry.intersects this.Position other.Geometry other.Position
            if collisionState.collides then 
                collides <- true
                logDebug(this.Name + " - Collision detected with " + other.Name + " at " + formatVector(collisionState.closest)) 
                mutex.WaitOne() |> ignore 
                this.doActionWith(other) 
                logDebug(this.Name + " - Collision action performed with " + other.Name + " at " + formatVector(collisionState.closest))
                mutex.ReleaseMutex()
                collides <- false

        abstract doActionWith: Displayable -> unit
        default this.doActionWith (other:Displayable) =  
            if other.isPermeable() then 
                logDebug(this.Name + " <--- Is permeable " + other.Name + " Do Nothing= " )
            else this.reflect(other)

        // Am anderen Objekt reflektieren
        member this.reflect(other:Displayable) =
            let hitPoint = this.hitPoint(other)
            logDebug(this.Name + " --- reflect at " + other.Name + " P= " + formatVector(hitPoint))
            let anotherNormal = other.getNormalAt(hitPoint)
            logDebug(this.Name + " --- reflected at " + other.Name + " N= " + formatVector(anotherNormal))
            this.Direction <- Vector3.Reflect(this.Direction, anotherNormal)
            logDebug( " !!! Is now: "  + this.ToString())

        /// <summary>
        /// Move
        /// </summary>
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

        /// <summary>
        /// Moveable: Motion
        /// Steuerung der Bewegung über Direction Position, Speed
        /// </summary>
        member this.Move(time: int64) = 
            this.Lifetime <- time
            if not collides then
                if moveRandom && lifetime%randomInterval = 0L then
                    this.updateDirectionRandom()
                this.computePosition()
                logDebug(this.Name + " - Moved to " + this.Position.ToString())

        // Active movement
        member this.MoveRandom
            with get() = moveRandom
            and set (aValue) = moveRandom <- aValue

        member this.SetMotion(motion:Motion) =
            this.Position <- motion.position
            this.Direction <- motion.direction
            this.Velocity <- motion.velocity

        override this.MoveDirection(newDirection:Vector3) (newSpeed:float32) =
            this.Direction <- newDirection
            this.Velocity <- newSpeed

        override this.Stop() = 
            this.LastSpeed <- this.Velocity
            this.Velocity <- 0.0f
            this.Stopped <- true

        member this.Stop(stoptime) =
            // TODO
            this.Stoptime <- this.Lifetime + stoptime
            printfn "Gestoppt %O bis %O" this.Name this.Stoptime
            base.Stop()

        member this.updateDirectionRandom() =
            // TODO
            ()          

        member this.invertDirection() =
            this.Direction <- this.Direction * -1.0f     

        member this.computePosition() =
            this.LastPosition <- this.Position
            let mutable pos = this.Position + this.Direction * this.Velocity 
            if moveOnGround then
                let groundPosition = this.groundPositionAt(this.Position)
                pos.Y <- groundPosition.Y + (this.height / 2.0f) + 0.2f 
            this.Position <-  pos              
        

        /// <summary>
        ///Ist das noch aktuell
        /// </summary>
        // TODO
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

        /// <summary>
        /// Workflow
        /// Start, Stop
        /// </summary>
        member this.MotionWorkflow =
            motionWorkflow this

        member this.startWorkflow() =  
            cancelWorkflow <- new CancellationTokenSource() 
            let starteable = motionWorkflow this
            Async.Start(starteable, cancelWorkflow.Token)
            workflowActive <- true

        member this.stopWorkflow() = 
            cancelWorkflow.Cancel()
            logInfo("Workflow stopped ")
            workflowActive <- false

    /// <summary>
    /// Oberklasse für alle unbeweglichen Objekte
    /// Subklassen: 
    ///     Landscape
    /// </summary>
    type Immoveable(name: string, geometry:Geometric, surface: Surface, color:Color, position: Vector3) =
        inherit Displayable(name, geometry, surface, color, position)
        new(name, geometry, color, position) = Immoveable(name, geometry, new Surface() , color, position)
        new() = Immoveable("", Kugel("", 1.0f, Color.Transparent), Surface(), Color.Transparent , Vector3.Zero)  
        
        override this.isMoveable() = false

        override this.hits(other:Displayable) =
            let state = this.Geometry.intersects this.Position other.Geometry other.Position
            state.collides

        override this.ToString() = 
            name + formatVector(this.Position) + " | " +  this.Geometry.BoundingBox(this.Position).ToString()   
