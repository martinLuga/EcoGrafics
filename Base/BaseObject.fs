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
open GameTimer

module ObjectBase = 

    exception ObjectDuplicateException of string

    let UNDEF = Vector3(7777.0f, 8888.0f, 9999.0f)

    // ----------------------------------------------------------------------------------------------------
    // Oberklasse für alle graphisch anzeigbaren Objekte
    // Subklassen: 
    // Moveable, Immoveable 
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>] 
    type BaseObject(name: string, display: Display, position:Vector3, rotation:Matrix, scale:Vector3) =  
        let mutable position=position
        let mutable display = display
        let mutable name=name
        let mutable idx=0
        let mutable changed=true       
        let mutable rotation = rotation 
        let mutable scale = scale
        let mutable originCenter = UNDEF

        new (name) = BaseObject(name, new Display(), Vector3.Zero, Matrix.Identity, Vector3.One) 
        new (name, position) = BaseObject(name, new Display(), position, Matrix.Identity, Vector3.One)
        new (name, display, position) = BaseObject(name, display, position, Matrix.Identity, Vector3.One)
        new (name, display, position, rotation) = BaseObject(name, display, position, rotation, Vector3.One)
        new (name, display, position, scale) = BaseObject(name, display, position, Matrix.Identity, scale)

        member this.LocalTransform() =
            createLocalTransform (this.Position, this.Rotation, this.Scale, this.CenterOrigin) 
        
        abstract member Copy:unit -> BaseObject  
        default this.Copy () = this

        abstract member DeepCopy:unit -> BaseObject
        default this.DeepCopy () =  this.MemberwiseClone():?> BaseObject 

        abstract member World: Matrix with get 
        default this.World = this.LocalTransform()

        abstract member Center: Vector3 with get
        default this.Center 
            with get() = position + display.Center  

        abstract member Changed:bool with get, set 
        default this.Changed
            with get () = changed
            and set (aValue) = changed <- aValue

        // Body has changed: update Grafic
        abstract member OnUpdateBody:Unit -> Unit
        default this.OnUpdateBody () = ()

        // Grafic has changed: notify Body
        abstract member OnUpdateGrafic:Unit -> Unit
        default this.OnUpdateGrafic () = ()

        // Simulation step
        abstract member Step:GameTimer -> Unit
        default this.Step (timer:GameTimer) = ()

        // Origin to Center
        member this.OriginCenter =
            if originCenter = UNDEF then
                originCenter <- this.Center - this.Position 
            originCenter

        // Center to Origin
        member this.CenterOrigin =
            - this.OriginCenter  

        member this.Position
            with get () = position
            and set (aValue) = position <- aValue

        member this.Scale
            with get () = scale
            and set (aValue) = scale <- aValue

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

        abstract member isCenter: unit -> bool
        default this.isCenter() =
            false

        abstract member isGround: unit -> bool
        default this.isGround() =
            false

        override this.ToString() = 
            this.Name

        abstract member Rotation : Matrix with get, set
        default this.Rotation
            with get() = rotation
            and set(value) = rotation <- value