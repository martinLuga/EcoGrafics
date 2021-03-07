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
    [<AllowNullLiteral>] 
    type Displayable(name: string, geometry:Geometric, surface: Surface, color:Color, start:Vector3) = 
        let mutable geometry = geometry
        let mutable surface = surface
        let mutable name=name
        let mutable color=color
        let mutable position=start
        let mutable world = Matrix.Translation(start - Vector3.Zero)
        let mutable lastColor=color
        let mutable changed=false
        
        static let mutable highValue = 0  

        do
            highValue <- highValue + 1

        new(name, geometry, color, position) =  Displayable(name, geometry, new Surface(), color, position)
        new () = Displayable("", Kugel("", 1.0f, Color.Transparent), Surface(), Color.Transparent , Vector3.Zero)  

        static member HighValue
            with get() = highValue
            and set(value) = highValue <- value
        
        abstract member Copy:unit -> Displayable  
        default this.Copy () = this

        abstract member DeepCopy:unit -> Displayable
        default this.DeepCopy () =  this.MemberwiseClone():?> Displayable 

        abstract member World: Matrix with get 
        default this.World        
            with get() = 
                Matrix.Translation(position)

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

        member this.isTransparent() =
            this.Surface.Visibility = Visibility.Transparent  

        // hits if not disjoint
        abstract member hits: Displayable -> Boolean
        default this.hits(other:Displayable) =
            false

        // complete collide in one procecss 
        abstract member CheckNear: Displayable -> unit
        default this.CheckNear(other:Displayable) =
            ()

        // hitpoint berechnen
        // Kann zu einem Zeitpunkt nur von einer Seite (links, rechts oder oben, unten .. ) kommen
        // theoretisch auch auf die Kante, aber eher unwahrscheinlich
        // Allgemeinste Implementierung basierend auf dem Mittelpunkt un der BoundingBox 
        abstract member hitPoint: Displayable -> Vector3
        default this.hitPoint(someDisplayable:Displayable) =
            let mutable thisPoint = this.Center 
            let mutable otherbb = someDisplayable.Geometry.BoundingBox(someDisplayable.Position)
            Collision.ClosestPointBoxPoint(&otherbb, &thisPoint)

        abstract member isMoveable: unit -> bool
        abstract member isPermeable: unit -> bool
        abstract member isSimulation: unit -> bool
        abstract member isAlive: unit -> bool
        abstract member isMoving: unit -> bool
        default this.isMoveable() = false
        default this.isPermeable() = false
        default this.isSimulation() = false
        default this.isAlive() = false
        default this.isMoving() = false

        abstract member MoveDirection : Vector3 -> float32 -> unit  
        default this.MoveDirection (newDirection:Vector3) (newSpeed:float32) = ()

        abstract member Stop : unit -> unit  
        default this.Stop() = ()

        abstract member IsColliding: Displayable -> unit 
        default this.IsColliding (another:Displayable) = ()

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

        //
        // Hilite eines Displayables durch eine transparente Box 
        //
        member this.createHilite () = 
            let color = Color.Yellow
            let adjust = 1.0f             
            let adjustVector = Vector3(adjust, adjust, adjust)
            let (minimum, maximum) = this.Boundaries
            let laenge = abs(maximum.X - minimum.X)  + 2.0f * adjust
            let hoehe  = abs(maximum.Y - minimum.Y)  + 2.0f * adjust 
            let breite = abs(maximum.Z - minimum.Z)  + 2.0f * adjust 
            let position = minimum - adjustVector
            let dispName = "HI:" + ":" + this.Name
            let adobeGeometry = new Quader(dispName, laenge, hoehe, breite, Color.Green) 
            let adobe = 
                Displayable(
                    name=dispName,
                    geometry=adobeGeometry,                              
                    surface=Surface(
                        Texture (
                            "water_texture",
                            "AntBehaviourApp",
                            "textures",
                            "water_texture.jpg"
                        ),
                        Material(
                            name="HILITE-" + dispName,
                            ambient=Color4(0.2f),
                            diffuse=Color4.White,
                            specular=Color4.White,
                            specularPower=20.0f,
                            emissive=color.ToColor4()                       // Farbe aus emissive -> Material Buffer
                        ),
                        Visibility.Transparent                              // Blendstate Transparent
                    ),
                    color=Color.White,                                      // Farbe aus Displayable, unused
                    start=position
                )  
            adobe

    // Macht die Klasse Sinn?
    // Vielleicht später. 
    // Rauch, Nebel etc
    type Permeable (name: string, geometry:Geometric, surface:Surface,  color:Color, position: Vector3) =
        inherit Displayable(name, geometry, surface, color, position)

        override this.isPermeable() = true