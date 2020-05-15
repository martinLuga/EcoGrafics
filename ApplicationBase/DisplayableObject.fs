namespace ApplicationBase
//
//  Displayable.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System

open SharpDX

open Geometry.GeometricModel

module DisplayableObject = 

    exception ObjectDuplicateException of string

    // ----------------------------------------------------------------------------------------------------
    // DISPLAYABLE
    // Oberklasse für alle graphisch anzeigbaren Objekte
    // Subklassen: 
    //  Moveable, Immoveable 
    // ----------------------------------------------------------------------------------------------------
    type Displayable(name: string, geometry:Geometric, surface: Surface, color:Color, start:Vector3) = 
        let mutable geometry = geometry
        let mutable surface = surface
        let mutable name=name
        let mutable color=color
        let mutable position=start
        let mutable world = Matrix.Translation(start - Vector3.Zero)
        let mutable lastColor=color
        let mutable changed=false

        new(name, geometry, color, position) =  Displayable(name, geometry, new Surface(), color, position)
        new () = Displayable("", Kugel("", 1.0f, Color.Transparent), Surface(), Color.Transparent , Vector3.Zero)  
        
        abstract member Copy:unit -> Displayable  
        default this.Copy () = this

        abstract member World: Matrix with get 
        default this.World        
            with get() = world

        abstract member Center: Vector3 with get 
        default this.Center 
            with get() = position + geometry.Center

        member this.Changed
            with get() = changed
            and set (aValue) = changed <- aValue 

        member this.Position 
            with get() = position
            and set (aValue) = 
                position <- aValue
                world <- Matrix.Translation(position)

        member this.Geometry 
            with get() = geometry
            and set (aValue) = geometry <- aValue

        member this.Name 
            with get() = name
            and set (aValue) = name <- aValue     
            
        member this.Color  
            with get () = color
            and set (value) = color <- value   
            
        member this.LastColor 
            with get () = lastColor
            and set (value) = lastColor <- value

        member this.Surface 
            with get() = surface
            and set (aValue) = surface <- aValue

 
        abstract member isDisjunct: Displayable -> Boolean
        default this.isDisjunct (someDisplayable:Displayable) =   
            not (this.hits(someDisplayable))

        member this.isTransparent() =
            this.Surface.Visibility = Visibility.Transparent  

        // is far if disjoint
        member this.isFar(someDisplayable:Displayable) =
            this.isDisjunct someDisplayable 

        // hits if not disjoint
        abstract member hits: Displayable -> Boolean
        default this.hits(other:Displayable) =
            false

        // hitpoint berechnen
        // Kann zu einem Zeitpunkt nur von einer Seite (links, rechts oder oben, unten .. ) kommen
        // theoretisch auch auf die Kante, aber eher unwahrscheinlich
        // Allgemeinste Implementierung basierend auf dem Mittelpunkt un der BoundingBox 
        member this.hitPoint(someDisplayable:Displayable) =
            let mutable thisPoint = this.Center 
            let mutable otherbb = someDisplayable.Geometry.BoundingBox(someDisplayable.Position)
            Collision.ClosestPointBoxPoint(&otherbb, &thisPoint)

        abstract member isMoveable: unit -> bool
        abstract member isPermeable: unit -> bool
        abstract member isSimulation: unit -> bool
        abstract member isAlive: unit -> bool
        default this.isMoveable() = false
        default this.isPermeable() = false
        default this.isSimulation() = false
        default this.isAlive() = true

        abstract member move : Vector3 -> float32 -> unit  
        default this.move (newDirection:Vector3) (newSpeed:float32) = ()

        abstract member stop : unit -> unit  
        default this.stop() = ()

        abstract member informNearTo: Displayable -> unit 
        default this.informNearTo (another:Displayable) = ()

        abstract member informFarTo: Displayable -> unit 
        default this.informFarTo (another:Displayable) = ()
    
        abstract member isAnt: unit -> bool
        default this.isAnt() =
            false

        abstract member isFood: bool
        default this.isFood =
            false

        abstract member isEnemy: unit -> bool
        default this.isEnemy() =
            false

        abstract member isCenter: unit -> bool
        default this.isCenter() =
            false

        abstract member hasEnergy: unit -> bool
        default this.hasEnergy() =
            false

        abstract member isLandscape: unit -> bool
        default this.isLandscape() =
            false

        abstract member isGround: unit -> bool
        default this.isGround() =
            false

        // Minmaler und maximaler Punkt, die Beide einen Würfel beschreiben
        // Gerechnet wird vom Aufsetzpunkt (start)
        // Die Geometrie gibt einen (einschließenden) Würfel zurück
        member this.Boundaries =
            this.Geometry.Boundaries(position)

        member this.height =
            let (min, max) = this.Boundaries
            max.Y - min.Y 

        member this.currentMax =
            let (min, max) = this.Boundaries
            max  

        member this.currentMin =
            let (min, max) = this.Boundaries
            min  

        override this.ToString() = 
            name

        member this.hasTexture() =
            this.Surface.hasTexture()  

        member this.getNormalAt(hitPoint: Vector3) =
            this.Geometry.getNormalAt(hitPoint, this.Position)

        member this.getClosestAt(position: Vector3) =
            this.Geometry.getClosestAt(position)

        member this.getVertexData() =
            this.Geometry.getVertexData(this.isTransparent())

    // Macht die Klasse Sinn?
    // Vielleicht später. 
    // Rauch, Nebel etc
    type Permeable (name: string, geometry:Geometric, surface:Surface,  color:Color, position: Vector3) =
        inherit Displayable(name, geometry, surface, color, position)

        override this.isDisjunct (someDisplayable:Displayable) = true

        override this.isPermeable() = true
