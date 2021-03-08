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

/// <summary>
/// Kollisionserkennung über Umgebungen
///  Der Gedanke ist, dass Bereiche, in denen nur ein oder kein bewegliches Objekt vorhanden sind,
///  nicht geprüft werden müssen und somit keine Prüfung jeder gegen jeden stattfindet, was bei vielen Objekten 
///  zu einer Explosion von Prüfungen führt.
///
///  Jedes Simulations-Objekt befindet sich in einer Umgebung
///  Bei einer Bewegung (neue Position) bleibt es in der Umgebung, 
///      wenn alle Koordinaten innerhalb der Begrenzungen bleiben 
///      oder geht in eine neue Umgebung über, wenn eine Koordinate ausserhalb liegt 
///
///  Wenn in einer Umgebung mindestens 2 Objekte vorhanden sind und eines davon bewglich ist, wird für
///  diese Umgebung eine Kollisions-Workflow gestartet.
///
/// </summary>
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
        let mutable visible = false
        let mutable workflowActive = false
        let mutable ID = ""
        let mutable cancelWorkflow     = new CancellationTokenSource()

        ///  
        ///  Kollisionen innerhalb dieser Umgebung  
        ///  WF wird gestartet, wenn mindestens 2 Objekte in dieser Umgebung vorhanden sind
        ///  
        let collisionWorkflow (umgebung:Umgebung) = async { 
            let start = clock.ElapsedMilliseconds
            ID <- start.ToString()
            let mutable changed = true 
            logInfo("CollisionWorkflow started for: " + umgebung.Name)
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

        /// <summary>
        /// Workflow
        /// </summary>
        member this.startWorkflow() = 
            cancelWorkflow <- new CancellationTokenSource()  
            let starteable = collisionWorkflow this
            Async.Start(starteable, cancelWorkflow.Token)
            workflowActive <- true
            logInfo(this.Name + " - WF started ")

        member this.stopWorkflow() =
            cancelWorkflow.Cancel()
            workflowActive <- false
            logInfo(this.Name + " - WF stopped ")

        member this.controlWorkflows() = 
            this.stopWorkflow()         // in jedem Fall stoppen, denn die anz moveables hat sich geändert
            if this.hasElements() then
               this.startWorkflow()
        
        member this.Objekte
            with get() = objekte.Values

        member this.Moveables =
            objekte.Values |> Seq.filter (fun x -> x.isMoveable()) |> Seq.map(fun(x) -> x :?> Moveable) 

        member this.Visible 
            with get() = visible
            and set(value) = visible <- value

        member this.ToggleSurface() = 
            this.Visible <- not this.Visible
            this.Changed <- true
            this.Refresh()

        member this.HideSurface() = 
            this.Visible <- false
            this.Changed <- true
            this.Refresh()

        member this.UnhideSurface() = 
            this.Visible <- true
            this.Changed <- true
            this.Refresh()

        /// <summary>
        /// Bei der Neuanlage die Unterscheidung
        /// das Moveable soll möglichst nur in einer Umgebung liegen (Mittelpunkt)
        /// Ein Immoveable kann in mehreren Umgebungen sein.
        /// </summary>
        member this.enthaelt(object:Displayable) =
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
                logDebug(this.ToString() + " wurde hinzugefügt " + object.ToString())
                    
        /// <summary>
        /// Steuerung des Übergangs eines beweglichen Objekts von einer Umgebung in eine andere
        /// Wenn Objekt nicht in Umgebung  : keine Aktion
        /// Wenn Objekt eine Umgebung neu betritt: zu objekten hinzufügen
        /// Wenn Objekt eine Umgebung verlässt: aus objekten entfernen
        /// </summary>
        member this.Monitor(object:Moveable) = 

            let istJetztDrin = this.umgibt(object.Center) 
            let istJetztDraussen = not istJetztDrin
            let warVorherDrin = objekte.ContainsKey(object.Name)
            let warVorherDraussen = not warVorherDrin

            let istUnverändert = (istJetztDrin && warVorherDrin) || (istJetztDraussen && warVorherDraussen)
            let betritt = (istJetztDrin && warVorherDraussen)
            let verlässt = (istJetztDraussen && warVorherDrin)

            let statusToString() =
                if istUnverändert then 
                    "unverändert"
                    else 
                        if betritt then 
                            "betritt"
                        else 
                            if verlässt then 
                                "verlässt"
                            else "" 

            if istUnverändert then
                ()
            else
                logDebug(this.Name + " monitoring " + object.Name + " status " + statusToString())
                if betritt then 
                    logWarn(object.Name + " - betritt Umgebung " + this.ToString() + " jetzt mit: "+ (objekte.Count + 1).ToString() + " Objekten")
                    objekte.Add(object.Name, object)
                else
                    if verlässt then 
                        logWarn(object.Name + " - verlässt Umgebung " + this.ToString() + " jetzt mit: "+ (objekte.Count - 1).ToString() + " Objekten")
                        let res = objekte.Remove(object.Name) 
                        if not res then
                            ()
                this.Refresh()
                this.controlWorkflows()

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

        member this.moveables() =
            objekte.Values |> Seq.filter (fun x -> x.isMoveable()) 

        member this.anzahlMoveables() =
            this.moveables()|> Seq.length
        
        member this.anzahlObjects() =
            objekte.Values |> Seq.length

        member this.hasElements() =
            this.anzahlObjects() > 0 

        member this.isEmpty() =
            this.anzahlObjects() = 0 

        override this.ToString() =
            let act = if workflowActive then "-Active" else "-Inactive"
            this.Name 