namespace Simulation
//
//  WeltModul.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic
open System.Threading

open log4net
open SharpDX

open Base.Logging
open Base.Framework

open ApplicationBase.DisplayableObject
open ApplicationBase.MoveableObject

open Geometry.GeometricModel
open Geometry.ObjectConvenience
open Geometry.GeometryUtils

open WeltObjects

open UmgebungModul

// ----------------------------------------------------------------------------------------------------
// Kollisionserkennung über Umgebungen
// Die Welt enthät alle Umgebungen
// ----------------------------------------------------------------------------------------------------
module WeltModul = 

    let logger = LogManager.GetLogger("Simulation.Welt")
    let logDebug = Debug(logger)

    type WeltDaten =
       struct 
           val ursprung: Vector3       
           val laenge: float32     
           val malX  :  int       
           val malY  :  int   
           val malZ  :  int    
           new (ursprung: Vector3, laenge: float32, malX:int, malY:int, malZ:int) = { ursprung = ursprung; laenge = laenge; malX = malX; malY = malY; malZ = malZ}

           member this.toPoints() =
                toPoints(this.ursprung, this.laenge, this.malX, this.malY, this.malZ)
           
           member this. toBoundaries() =
                toBoundary(this.ursprung, this.laenge, this.malX, this.malY, this.malZ) 

            member this.Randomizer () =
                let mutable (MIN, MAX) = this.toBoundaries()
                let minV = MIN + new Vector3(1.0f, 1.0f, 1.0f)
                let maxV = MAX - new Vector3(1.0f, 1.0f, 1.0f)
                let pos = randomPositionFromTo(minV, maxV)
                let dir = randomDirectionFromTo(minV, maxV)
                let velocity = randomSpeed(3)
                new Motion(pos, dir, velocity)

           override this.ToString() = "Welt(" + this.ursprung.ToString() + ")"
       end

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

        static member Instance
            with get() = 
                if instance = null then
                    instance <- new Welt()
                instance
            and set(value) = instance <- value

        member this.Initialize(ursprung:Vector3, laenge:float32, malX:int, malY:int, malZ:int) = 
            let (MIN, MAX) = toBoundary(ursprung, laenge, malX, malY, malZ)
            weltUrsprung <- ursprung
            umgebungsLaenge <- laenge 
            einheitenX <- malX
            einheitenY <- malY
            einheitenZ <- malZ
            xMIN <- MIN.X
            xMAX <- MAX.X
            yMIN <- MIN.Y
            yMAX <- MAX.Y
            zMIN <- MIN.Z
            zMAX <- MAX.Z
            this.createUmgebungen()
            this.HideUmgebungen() 

        member this.InitFromPoints(xmin:float32, xmax:float32, ymin:float32, ymax:float32, zmin:float32, zmax:float32, laenge:float32) = 
            weltUrsprung <- Vector3(xmin, ymin, zmin)
            umgebungsLaenge <- laenge 
            einheitenX <- (int)((xmax-xmin)/umgebungsLaenge) 
            einheitenY <- (int)((ymax-ymin)/umgebungsLaenge) 
            einheitenZ <- (int)((zmax-zmin)/umgebungsLaenge) 
            xMIN <- xmin
            xMAX <- xmax
            yMIN <- ymin
            yMAX <- ymax
            zMIN <- zmin
            zMAX <- zmax
            this.createUmgebungen() 
            this.HideUmgebungen() 

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

        member this.NichtLeereUmgebungen() =
            umgebungen.Values |> Seq.filter (fun u  -> u.hasElements())

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

        member this.MotionWorkflows =
            this.Moveables 
            |> Seq.map(fun x ->( x.MotionWorkflow))

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

        member this.UnhideUmgebungen() =
            for umgebung in umgebungen.Values do
                umgebung.UnhideSurface()

        member this.ToggleUmgebungen() =
            for umgebung in umgebungen.Values do
                umgebung.ToggleSurface()