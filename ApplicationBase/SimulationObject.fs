namespace ApplicationBase
//
//  SimulationObject.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open DisplayableObject
open MoveableObject
open Base.GlobalDefs
open Base.Logging

module SimulationObject =

    type TaskStatus = | IDLE = 1 | SCHEDULED = 2 | STAGED = 3 | ACTIVE = 4 | COMPLETED_SUCCESS = 5 | COMPLETED_FAILED = 6 | STOPPED = 7

    let logger = LogManager.GetLogger("simulations")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)
    let logWarn  = Warn(logger)

    // ----------------------------------------------------------------------------------------------------
    // Task: Eine Aufgabe mit einem zu erreichenden Ziel
    //       enthält mehrere Actions  
    // ---------------------------------------------------------------------------------------------------- 
    type Task(description: string, object:Moveable, startAction: (unit->unit), executeAction: (unit->unit), terminateAction: (unit->unit)) = 
        static let NULLACTION = (fun x -> ())
        let mutable description=description
        let mutable object=object
        let mutable startAction=startAction
        let mutable executeAction=executeAction
        let mutable terminateAction=terminateAction

        new() = Task("", new Moveable(), NULLACTION, NULLACTION, NULLACTION)

        override this.ToString() = 
            "Task " + this.Description

        member this.Description 
            with get() = description
            and set (aValue) = description <- aValue

        member this.Object 
            with get() = object
            and set (aValue) = object <- aValue

        member this.Start() = 
            startAction()

        member this.Execute() = 
            executeAction()

        member this.Terminate() = 
            terminateAction()

    // ----------------------------------------------------------------------------------------------------
    // Simulateable: Eine Aufgabe mit einem zu erreichenden Ziel
    //       enthält mehrere Actions  
    // ---------------------------------------------------------------------------------------------------- 
    type Simulateable(name, geometry, surface, color, position, direction, speed, moveRandom, energy, capacity) = 
        inherit Moveable(name, geometry, surface, color, position, direction, speed, moveRandom ) 

        static let mutable LOOKAROUNDINTERVAL: int64 = 150L       // Zeit zwischen 2 Lookarounds 

        let mutable taskStatus = TaskStatus.IDLE
        let mutable task: Task = new Task()        
        let mutable capacity = capacity
        let mutable energy = energy        
        let mutable searchStatus = WorkResult( WorkStatus.WAIT, "")

        static member LookAroundInterval 
            with get() = LOOKAROUNDINTERVAL
            and set(aValue) = LOOKAROUNDINTERVAL <- aValue 

        member this.TaskStatus 
            with get() = taskStatus
            and set (aValue) = taskStatus <- aValue

        member this.SearchStatus 
            with get() = searchStatus
            and set(aValue) = searchStatus <- aValue 

        override this.isSimulation() = true

        abstract lookAround : unit -> unit
        default this.lookAround () = 
            ()

        // ----------------------------------------------------------------------------------------------------
        // SimulationObject: ENERGY
        // ----------------------------------------------------------------------------------------------------
        member this.Energy        
            with get() = energy
            and set (aValue) = energy <- aValue
        
        override this.hasEnergy() =
            this.Energy > 0.0f

        member this.addEnergy(newEnergy:float32) = 
            this.Energy <- this.Energy + newEnergy       
            logWarn(this.Name + " Energy bekommen. Jetzt " +  this.Energy.ToString())
 
        member this.removeEnergy(energyAmount:float32) = 
            this.Energy <- this.Energy - energyAmount 
            logWarn(this.Name + " " + energyAmount.ToString() + " Energy abgegeben. Jetzt " + this.Energy.ToString())
            if this.Energy <= 0.0f then
                this.stop()
                logger.Error(this.Name + " Alle Energy abgegeben. Tot " )

        override this.isAlive() =  
            this.Energy > 0.0f
                
        member this.Capacity        
            with get() = capacity
            and set (aValue) = capacity <- aValue

        member this.AvailableCapacity() =
            this.Capacity - this.Energy

        member this.hasCapacity() =
            this.AvailableCapacity() > 0.0f

        // ----------------------------------------------------------------------------------------------------
        // SimulationObject: TASK
        // ----------------------------------------------------------------------------------------------------
        member this.Task        
            with get() = task
            and set (aValue) = task <- aValue

        abstract scheduleTask: Task -> unit
        default this.scheduleTask(task:Task) =         
            logInfo("Schedule Task " + task.Description + " for " + this.Name)

        abstract stageTask: (unit) -> unit
        default this.stageTask() =          
            logInfo("Stage Task must be implemented by subclass"  )

        abstract startTask: (unit) -> unit
        default this.startTask() =  
            logInfo("Start Task " + this.Task.Description + " at time: " + this.Lifetime. ToString()) 
            this.Task.Start() 
            this.TaskStatus <- TaskStatus.ACTIVE   

        abstract restartTask: (unit) -> unit
        default this.restartTask() =  
            this.Task.Start() 
            this.TaskStatus <- TaskStatus.ACTIVE  

        abstract executeTask: (unit) -> unit
        default this.executeTask() =
            this.Task.Execute()

        abstract terminateTask: (unit) -> unit
        default this.terminateTask() = 
            this.Task.Terminate()

        abstract purgeTask: (unit) -> unit
        default this.purgeTask() =
            this.Task <- new Task()
            this.TaskStatus <- TaskStatus.IDLE

        // ----------------------------------------------------------------------------------------------------
        // SimulationObject: Heartbeat
        // Steuerung Tasks über den Status
        // Nur wenn nicht gerade eine Kollision vorliegt
        // ----------------------------------------------------------------------------------------------------
        member this.heartbeat(time: int64) = 
        
            base.Motion(time)               // Movement

            //if not this.Collides then
            //    match this.TaskStatus with

            //    | TaskStatus.IDLE ->
            //        this.stageTask()        // Sind Tasks vorhanden, Stage: this.ActiveTask setzen      
                
            //    | TaskStatus.STAGED ->      // Start Action ausführen, danach Status = Active
            //        this.startTask()            
        
            //    | TaskStatus.ACTIVE ->      // ExecuteAction wird solange ausgeführt, solange Status = Active
            //        this.executeTask()

            //    | TaskStatus.COMPLETED_SUCCESS 
            //    | TaskStatus.COMPLETED_FAILED ->
            //        this.terminateTask()    // TerminateAction wird ausgeführt
            //        this.purgeTask()

            //    | _ -> logInfo("Task???" + this.TaskStatus.ToString())

        // ----------------------------------------------------------------------------------------------------
        // SIMULATIONOBJCT: doActionWith
        // ----------------------------------------------------------------------------------------------------
        override this.doActionWith (other:Displayable) =  
            if logMode = LogMode.VERBOSE then
                printfn "%O doActionWith to %O" this.Name  other.Name

            // Ant: Andere Ant getroffen
            if other.isAnt() then
                printfn "Ant: %O getroffen " other.Name
                this.stepAside()   

            else base.doActionWith (other)

