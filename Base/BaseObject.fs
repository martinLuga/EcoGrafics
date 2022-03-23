﻿namespace Base
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
    type BaseObject(name: string, display: Display, position:Vector3, rotation:Vector4, scale:Vector3) =  
        let mutable position=position
        let mutable display = display
        let mutable name=name
        let mutable idx=0
        let mutable changed=true
        let mutable orientation=Vector3.UnitX         
        let mutable rotation = rotation.ToArray()
        let mutable translation = position.ToArray()
        let mutable scale = scale.ToArray()

        new (name) = BaseObject(name, new Display(), Vector3.Zero, Vector4.Zero, Vector3.One) 
        new (name, position) = BaseObject(name, new Display(), position, Vector4.Zero, Vector3.One)
        new (name, display, position) = BaseObject(name, display, position, Vector4.Zero, Vector3.One)
        new (name, display, position, scale) = BaseObject(name, display, position, Vector4.Zero, scale)

        member this.LocalTransform() =
            createLocalTransform (translation, rotation, scale) 
        
        abstract member Copy:unit -> BaseObject  
        default this.Copy () = this

        abstract member DeepCopy:unit -> BaseObject
        default this.DeepCopy () =  this.MemberwiseClone():?> BaseObject 

        abstract member World: Matrix with get 
        default this.World = this.LocalTransform()

        abstract member Center: Vector3 with get
        default this.Center 
            with get() = position + display.Center  

        abstract member Orientation: Vector3 with get,set
        default this.Orientation
            with get() = Vector3.UnitX
            and set(value) = orientation <- value
        
        abstract member Changed:bool with get, set 
        default this.Changed
            with get () = changed
            and set (aValue) = changed <- aValue

        // Body has changed: update Grafic
        abstract member OnUpdateBody:Unit -> Unit
        default this.OnUpdateBody () = ()

        member this.CenterOrigin() =
            this.Center - this.Position 

        member this.OriginCenter() =
            this.Position - this.Center  

        member this.Position
            with get () = position
            and set (aValue) = 
                position <- aValue
                translation <- position.ToArray()

        member this.Translation
            with get () = translation
            and set (aValue) = 
                translation <- aValue 

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

        member this.Rotation
            with get() = rotation
            and set(value) = rotation <- value