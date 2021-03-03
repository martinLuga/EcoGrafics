namespace Simulation
//
//  WeltModul.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

open log4net
open SharpDX

open Base.Logging

open ApplicationBase.DisplayableObject
open ApplicationBase.MoveableObject

open Geometry.GeometricModel
open Geometry.ObjectConvenience

open WeltObjects

open UmgebungModul

// ----------------------------------------------------------------------------------------------------
// Kollisionserkennung über Umgebungen
// Die Welt enthät alle Umgebungen
// ----------------------------------------------------------------------------------------------------
module WeltModul = 

    let logger = LogManager.GetLogger("Simulation.Welt")
    let logDebug = Debug(logger)
    let cancelMoveablesWorkflows = new CancellationTokenSource()

    [<AllowNullLiteral>] 
    type Welt() =         
        static let mutable instance = null
        let mutable weltUrsprung=Vector3.Zero
        let mutable umgebungen = new Dictionary<Vector3, Umgebung>()
        let mutable umgebungsLaenge=0.0f
        let mutable einheitenX=0
        let mutable einheitenY=0
        let mutable einheitenZ=0
        let mutable xMIN= -20.0f 
        let mutable xMAX=  20.0f
        let mutable yMIN= -20.0f
        let mutable yMAX=  20.0f
        let mutable zMIN= -20.0f
        let mutable zMAX=  20.0f

        member this.Initialize(ursprung:Vector3, laenge:float32, malX:int, malY:int, malZ:int) = 
            weltUrsprung <- ursprung
            umgebungsLaenge <- laenge 
            einheitenX <- malX
            einheitenY <- malY
            einheitenZ <- malZ
            xMIN <- ursprung.X
            xMAX <- ursprung.X + (float32) malX * umgebungsLaenge
            yMIN <- ursprung.Y
            yMAX <- ursprung.Y + (float32) malY * umgebungsLaenge
            zMIN <- ursprung.Z
            zMAX <- ursprung.Z + (float32) malZ * umgebungsLaenge
            this.createUmgebungen() 

        member this.InitFromPoints(xmin, xmax, ymin, ymax, zmin, zmax) = 
            weltUrsprung <- Vector3(xmin, ymin, zmin)
            umgebungsLaenge <- (xmax-xmin)/4.0f
            xMIN <- xmin
            xMAX <- xmax
            yMIN <- ymin
            yMAX <- ymax
            zMIN <- zmin
            zMAX <- zmax
            this.createUmgebungen() 

        static member Instance
            with get() = 
                if instance = null then
                    instance <- new Welt()
                instance
            and set(value) = instance <- value

        member this.createUmgebungen() =
            umgebungen <- new Dictionary<Vector3, Umgebung>()
            for i = 0 to einheitenX-1 do 
                for j = 0 to einheitenY-1  do 
                    for k = 0 to einheitenZ-1  do 
                        let x = weltUrsprung.X + (float32)i * umgebungsLaenge
                        let y = weltUrsprung.Y + (float32)j * umgebungsLaenge
                        let z = weltUrsprung.Z + (float32)k * umgebungsLaenge
                        let punkt = new Vector3(x, y, z) 
                        let is = sprintf "%03i" i
                        let js = sprintf "%03i" j
                        let ks = sprintf "%03i" k
                        this.erzeugeUmgebung(is + "-" + js + "-" + ks, punkt)

        member this.XMIN
            with get() = xMIN
            and set(v) = xMIN <- v 
        member this.XMAX
            with get() = xMAX
            and set(v) = xMAX <- v
        member this.YMIN
            with get() = yMIN
            and set(v) = yMIN <- v
        member this.YMAX
            with get() = yMAX
            and set(v) = yMAX <- v
        member this.ZMIN
            with get() = zMIN
            and set(v) = zMIN <- v
        member this.ZMAX
            with get() = zMAX
            and set(v) = zMAX <- v

        member this.GroundHeight
            with get() = yMIN

        member this.Umgebungen
            with get() = umgebungen
            and set(value) = umgebungen <- value 

        member this.GetUmgebungenAsDisplayables() =
            this.Umgebungen.Values |> Seq.toList |> List.map (fun umgebung -> umgebung :> Displayable)

        member this.UmgebungsLaenge
            with get() = umgebungsLaenge
            and set(value) = umgebungsLaenge <- value 

        member this.umgebungZu(umgebungsUrsprung:Vector3) = 
            umgebungen.Item(umgebungsUrsprung)

        member this.erzeugeUmgebung(umgIdx, ursprung:Vector3) = 
            let umgebung = Umgebung("U("+umgIdx+")" , ursprung, umgebungsLaenge) 
            if umgebungen.ContainsKey(umgebung.Position) then
                 raise (ObjectDuplicateException("Umgebung bereits vorhanden"))
            else
                umgebungen.Add(umgebung.Position, umgebung)

        member this.umgebungenZuObjekt(objekt:Displayable) = 
            umgebungen.Values |> Seq.filter (fun umg -> umg.enthaelt(objekt))

        member this.registriereObjekt(objekt:Displayable) = 
            let umgebungen = this.umgebungenZuObjekt(objekt) 
            for umgebung in umgebungen do
                umgebung.Add(objekt)

        member this.registriereObjektListe list  =
            List.map (fun o ->  this.registriereObjekt o) list |> ignore 

        member this.Moveables =
            umgebungen.Values 
            |> Seq.collect (fun x -> (x.Moveables))

        member this.CollisionWorkflows =
            umgebungen.Values 
            |> Seq.map (fun x -> (x.CollisionWorkflow))

        member this.MotionWorkflows =
            this.Moveables |> Seq.map(fun x -> x.MotionWorkflow)

        // ----------------------------------------------------------------------------------------------------
        // Welt-Displayables
        // ----------------------------------------------------------------------------------------------------   
        member this.Ground =
            new Landscape(
                name="ground",
                geometry=new Quader("WeltGround", xMAX - xMIN, 2.0f, zMAX - zMIN, Color.Transparent),        
                surface=SURFACE_GROUND,
                position=Vector3(xMIN, yMIN-2.0f, zMIN),
                color=Color.Transparent
            )

        member this.leftLimit =
            new Landscape(
                name="leftLimit",
                geometry=new Quader("leftLimit", 2.0f, yMAX - yMIN, zMAX - zMIN, Color.Transparent),        
                surface=SURFACE_LIMIT("Limit", Color.Transparent),
                position=Vector3(xMIN-2.0f, yMIN, zMIN),
                color=Color.Transparent
            )
                
        member this.rightLimit =
            new Landscape(
                name="rightLimit",
                geometry=new Quader("rightLimit", 2.0f, yMAX - yMIN, zMAX - zMIN, Color.Transparent),        
                surface=SURFACE_LIMIT("Limit", Color.Transparent),
                position=Vector3(xMAX, yMIN, zMIN),
                color=Color.Transparent
            )

        member this.topLimit =
            new Landscape(
                name="topLimit",
                geometry=new Quader("topLimit", xMAX - xMIN, 2.0f, zMAX - zMIN, Color.Transparent),        
                surface=SURFACE_LIMIT("Limit", Color.Transparent),
                position=Vector3(xMIN, yMAX, zMIN),
                color=Color.Transparent
            )

        member this.backLimit =
            new Landscape(
                name="backLimit",
                geometry=new Quader("backLimit", xMAX - xMIN,  yMAX - yMIN,  2.0f, Color.Transparent),        
                surface=SURFACE_LIMIT("Limit", Color.Transparent),
                position=Vector3(xMIN, yMIN, zMAX),
                color=Color.Transparent
            )

        member this.frontLimit =
            new Landscape(
                name="frontLimit",
                geometry=new Quader("frontLimit", xMAX - xMIN,  yMAX - yMIN,  2.0f, Color.Transparent),        
                surface=SURFACE_LIMIT("Limit", Color.Transparent),
                position=Vector3(xMIN, yMIN, zMIN-2.0f),
                color=Color.Transparent
            )

        member this.WorldLimits =
            [
                this.Ground:>Displayable;
                this.topLimit:>Displayable;
                this.leftLimit:>Displayable;
                this.rightLimit:>Displayable;
                this.backLimit:>Displayable;
                this.frontLimit:>Displayable;
            ]

        member this.GetDisplayables() =
            List.concat [this.GetUmgebungenAsDisplayables(); this.WorldLimits ]

        // ----------------------------------------------------------------------------------------------------
        // Collision
        // ----------------------------------------------------------------------------------------------------    
        member this.HideUmgebungen() =
            for umgebung in umgebungen.Values do
                umgebung.HideSurface()

        member this.ToggleUmgebungen() =
            for umgebung in umgebungen.Values do
                umgebung.ToggleSurface()