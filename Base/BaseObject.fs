namespace Base
//
//  Displayable.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open SharpDX
open GeometryUtils
open ModelSupport

module ObjectBase = 

    exception ObjectDuplicateException of string

    // ----------------------------------------------------------------------------------------------------
    // Oberklasse für alle graphisch anzeigbaren Objekte
    // Subklassen: 
    // Moveable, Immoveable 
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>] 
    type BaseObject(name: string, display: Display, position:Vector3) =  
        let mutable position=position
        let mutable display = display
        let mutable name=name
        let mutable idx=0
        let mutable changed=true
        let mutable orientation=Vector3.UnitX           // HACK , muss je nach Objekt eingestellt werden
        let mutable world = Matrix.Translation(position - Vector3.Zero)        
        let mutable rotation = Matrix.Identity

        new (name) = BaseObject(name, new Display(), Vector3.Zero) 
        new (name, position) = BaseObject(name, new Display(), position)
        
        abstract member Copy:unit -> BaseObject  
        default this.Copy () = this

        abstract member DeepCopy:unit -> BaseObject
        default this.DeepCopy () =  this.MemberwiseClone():?> BaseObject 

        abstract member World: Matrix with get 
        default this.World = Matrix.Multiply(rotation, Matrix.Translation(position))

        abstract member Center: Vector3 with get
        default this.Center 
            with get() = position + display.Center  

        abstract member Orientation: Vector3 with get,set
        default this.Orientation
            with get() = Vector3.UnitX
            and set(value) = orientation <- value
        
        abstract member RealWorld: Vector3 -> Matrix
        default this.RealWorld(position) = 
           Matrix.Multiply(rotation, Matrix.Translation(position))

        abstract member Changed:bool with get, set 
        default this.Changed
            with get () = changed
            and set (aValue) = changed <- aValue

        // Body has changed: update Grafic
        abstract member OnUpdateBody:Unit -> Unit
        default this.OnUpdateBody () = ()

        member this.Position
            with get () = position
            and set (aValue) = position <- aValue

        member this.Display 
            with get () = display
            and set (aValue) = display <- aValue

        member this.Name
            with get () = name
            and set (aValue) = name <- aValue

        member this.Idx
            with get () = idx
            and set (aValue) = idx <- aValue

        member this.BoundingBox =
            display.BoundingBox(this.Position) 
        
        member this.Transparent = display.isTransparent   

        abstract member Moveable : bool
        default this.Moveable = false

        abstract member isPermeable: unit -> bool
        abstract member isSimulation: unit -> bool
        abstract member isAlive: unit -> bool
        abstract member isMoving: unit -> bool
        default this.isPermeable() = false
        default this.isSimulation() = false
        default this.isAlive() = false
        default this.isMoving() = false

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

        override this.ToString() = 
            this.Name

        member this.setRotationInXZ(angle) =
           rotation <- rotationMatrixHor(angle)  

        member this.setRotationInYZ(angle) =
           rotation <- rotationMatrixVert(angle)  

        // Ausrichtung zwischen zwei Punkten
        member this.setRotationBetween(point1:Vector3, point2) =
            let mutable _p1 = Vector3(point1.X, point1.Y + 1.0f, point1.Z)
            let mutable _p2 = point2
            _p1 <- _p1 - point1 
            _p2 <- point2 - point1 
            let quat = rotateBetween (_p1, _p2)
            rotation <- Matrix.RotationQuaternion(quat)

        member this.Rotation
            with get() = rotation
            and set(value) = rotation <- value