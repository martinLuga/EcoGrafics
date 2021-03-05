namespace Simulation

//
//  Umgebung.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//
open System 
open System.Threading

open log4net
open SharpDX
open System.Collections.Generic

open ApplicationBase.DisplayableObject
open ApplicationBase.MoveableObject
open ApplicationBase.WindowControl

open Base.Logging

open Geometry.GeometricModel 

// ----------------------------------------------------------------------------------------------------
// Kollisionserkennung über Umgebungen
//  Der Gedanke ist, dass Bereiche, in denen nur ein oder kein bewegliches Objekt vorhanden sind,
//  nicht geprüft werden müssen und somit keine Prüfung jeder gegen jeden stattfindet, was bei vielen Objekten 
//  zu einer Explosion von Prüfungen führt.
//
//  Jedes Simulations-Objekt befindet sich in einer Umgebung
//  Bei einer Bewegung (neue Position) bleibt es in der Umgebung, 
//      wenn alle Koordinaten innerhalb der Begrenzungen bleiben 
//      oder geht in eine neue Umgebung über, wenn eine Koordinate ausserhalb liegt 
//
//  Wenn in einer Umgebung mindestens 2 Objekte vorhanden sind und eines davon bewglich ist, wird für
//  diese Umgebung eine Kollisions-Workflow gestartet.
//
// ----------------------------------------------------------------------------------------------------
module UmgebungModul =

    type System.Single with
        member x.ToInt() = Math.Truncate(float x) |> int 

    let MAT_UMGEBUNG(name)  = 
        new Material(
            name="MAT-UMG-" + name,
            ambient=Color4(0.2f),
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=20.0f,
            emissive=Color.White.ToColor4()
        )

    let SURFACE_UMG(mat) = 
        new Surface(mat, visibility=Visibility.Transparent)

    let logger = LogManager.GetLogger("Simulation.Umgebung")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)
    let logWarn  = Warn(logger)
    let logError  = Error(logger)

    type Umgebung (name: string, ursprung: Vector3, laenge:float32) =
        inherit Permeable(
            name, 
            Quader("UMG-" + name, laenge, laenge, laenge, Color.Transparent),            
            SURFACE_UMG(MAT_UMGEBUNG(name)),
            Color.White,
            ursprung)

        let mutable laenge = laenge
        let mutable objekte = new Dictionary<String, Displayable>() 
        let mutable moveables = 0 
        let mutable visible = true
        let mutable workflowActive = false
        let mutable ID = ""

        // 
        // Kollisionen innerhalb dieser Umgebung  
        // WF wird gestartet, wenn mindestens 2 Objekte in dieser Umgebung vorhanden sind
        // 
        let collisionUmgebungWorkflow (umgebung:Umgebung) = async { 
            let start = clock.ElapsedMilliseconds
            ID <- start.ToString()
            let mutable changed = true 
            logInfo("CollisionUmgebungWorkflow started for: " + umgebung.Name)
            let immoveables = umgebung.Objekte |> Seq.filter (fun (x:Displayable) -> not(x.isMoveable())&&not(x.isPermeable()))            
            while true do 
                do! Async.Sleep 1
                let moveables = umgebung.Objekte |> Seq.filter (fun x -> (x :? Moveable))|> Seq.map(fun x -> (x:?>Moveable))
                if not (Seq.isEmpty(moveables)) then 
                    for movbl in moveables do
                        if movbl.isMoveable()  then
                            for imm in immoveables do
                                movbl.CheckNear(imm)

        }

        member this.Objekte
            with get() = objekte.Values

        member this.Moveables =
            objekte.Values |> Seq.filter (fun x -> x.isMoveable()) |> Seq.map(fun(x) -> x :?> Moveable) 

        member this.CollisionWorkflow =
            collisionUmgebungWorkflow this

        member this.Visible 
            with get() = visible
            and set(value) = visible <- value

        member this.WorkflowActive 
            with get() = workflowActive
            and set(value) = workflowActive <- value

        member this.ToggleSurface() = 
            this.Visible <- not this.Visible
            this.Changed <- true
            this.Refresh()

        member this.HideSurface() = 
            this.Visible <- false
            this.Changed <- true
            this.Refresh()

        // 
        // Bei der Neuanlage die Unterscheidung
        // das Moveable soll möglichst nur in einer Umgebung liegen (Mittelpunkt)
        // Ein Immoveable kann in mehreren Umgebungen sein
        // 
        member this.enthaelt(object:Displayable) =
            logDebug( this.ToString() + " - NEU/ENTHAELT " + object.ToString()  )
            if object.isMoveable() then 
                 this.umgibt(object.Center)
            else
                object.hits(this)

        member this.umgibt(punkt:Vector3) = 
            (ursprung.X <= punkt.X && punkt.X < ursprung.X + laenge) &&
            (ursprung.Y <= punkt.Y && punkt.Y < ursprung.Y + laenge) &&
            (ursprung.Z <= punkt.Z && punkt.Z < ursprung.Z + laenge)  

        member this.Add(object:Displayable) =
            if objekte.ContainsKey(object.Name) then
                ()
            else
                objekte.Add(object.Name, object)
                logDebug( this.ToString() + " - ENTHAELT: " + object.ToString()  )
                if object.isMoveable() then
                    moveables <- moveables + 1
                    
        // ----------------------------------------------------------------------------------------------------
        //  Steuerung
        // 
        //  Wenn ein bewegliches Objekt 
        //      keine Aktion
        //
        //  Wenn ein bewegliches Objekt eine Umgebung neu betritt:
        //      zu objekten hinzufügen
        //
        //  Wenn ein bewegliches Objekt eine Umgebung verlässt:
        //      aus objekten entfernen
        //
        // ----------------------------------------------------------------------------------------------------
        member this.Control(object:Moveable) =
            logDebug(this.Name + " - Control Waiting to shift " + object.Name)  
            lock object.Mutex (fun () ->
                let istJetztDrin = this.umgibt(object.Center) 
                let istJetztDraussen = not istJetztDrin
                let warVorherDrin = objekte.ContainsKey(object.Name)
                let warVorherDraussen = not warVorherDrin

                let istUnverändert = (istJetztDrin && warVorherDrin) || (istJetztDraussen && warVorherDraussen)
                let betritt = (istJetztDrin && warVorherDraussen)
                let verlässt = (istJetztDraussen && warVorherDrin)

                if istUnverändert then
                    ()
                else 
                    if betritt then 
                        logWarn(object.Name + " - betritt Umgebung " + this.ToString() + " jetzt mit: "+ (objekte.Count + 1).ToString() + " Objekten")
                        objekte.Add(object.Name, object)
                        moveables <- moveables + 1  
                        object.ResetCollision()
                    else
                        if verlässt then 
                            logWarn(object.Name + " - verlässt Umgebung " + this.ToString() + " jetzt mit: "+ (objekte.Count - 1).ToString() + " Objekten")
                            let res = objekte.Remove(object.Name) 
                            if not res then
                                ()
                            moveables <- moveables - 1
                            object.ResetCollision()
                    this.Refresh()
            )

        member this.Refresh() =
            if this.Visible then 
                if  this.isEmpty() then                    
                    this.Surface.Material.Diffuse  <- Color4.White
                    this.Surface.Material.Ambient  <- Color4(0.2f) 
                    this.Surface.Material.Emissive <- Color.White.ToColor4() 
                else
                    this.Surface.Material.Diffuse  <- Color4.White
                    this.Surface.Material.Ambient  <- Color.LightBlue.ToColor4() 
                    this.Surface.Material.Emissive <- Color.Blue.ToColor4() 
            else 
                this.Surface.Material.Diffuse  <- Color.Transparent.ToColor4()
                this.Surface.Material.Ambient  <- Color.Transparent.ToColor4()
                this.Surface.Material.Emissive <- Color.Transparent.ToColor4()

        member this.isEmpty() =
            moveables = 0 

        member this.hasElements() =
            moveables > 0 

        override this.ToString() =
            let act = if workflowActive then "-Active" else "-Inactive"
            this.Name 