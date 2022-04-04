namespace GltfBase

//
//  ModelSupport.fs
//
//  Created by Martin Luga on 08.02.20.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open NodeAdapter
open Base.GeometryUtils
open SharpDX
open VGltf.Types

// ----------------------------------------------------------------------------------------------------
// Objekt-Modell 
//      mit Gltf 3D Support
//      und Bullet Physics 
// 
// ----------------------------------------------------------------------------------------------------
module BaseObject =

    exception ObjectDuplicateException of string 

    // ----------------------------------------------------------------------------------------------------
    // Basis Objekt für Darstellung und Physik
    // Objekt ist ein darzustellendes Objekt (AUto, Tier...)
    //  Node ist ein Teil davon (Räder , Flossen...) vorher Part
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type Objekt(_name: string, _gltf:Gltf, _position: Vector3, _rotation:Vector4, _scale:Vector3, _direction: Vector3, _velocity:float32, _moveRandom:bool) =

        let mutable name = _name 
        let mutable idx=0
        let mutable position=_position
        let mutable center=Vector3.Zero
        let mutable direction=_direction
        let mutable scale:float32[] =_scale.ToArray()
        let mutable velocity=_velocity
        let mutable rotation:float32[] = _rotation.ToArray() 
        let mutable translation = position.ToArray()

        let mutable tree:NodeAdapter = null

        do
            let rootNodes = _gltf.RootNodes |> Seq.toList
            let node:Node = rootNodes.Item(0)
            let root = node.Children[0]             
            tree <- new NodeAdapter(_gltf, root)

        new (objectName, gltf, position, rotation, scale) = new Objekt(objectName, gltf, position, rotation, scale, Vector3.Zero, 0.0f, false ) 
        new (objectName, gltf, position, rotation) = new Objekt(objectName, gltf, position, rotation, Vector3.One, Vector3.Zero, 0.0f,false )
        new (objectName, gltf, position) = new Objekt(objectName, gltf, position, Vector4.Zero, Vector3.One, Vector3.Zero, 0.0f,false )

        override this.ToString() =
            "BaseObject: " + this.Name
       
        member this.Name
            with get() = name
            and set(value)  = name <- value 

        member this.Tree
            with get() = tree
            and set(value)  = tree <- value 

        member this.Idx
            with get() = idx
            and set(value)  = idx <- value 
    
        member this.Position
            with get() = position
            and set(value)  = position <- value 

        abstract member Center: Vector3 with get
        default this.Center 
            with get() = position + center  

        member this.OriginCenter =
            this.Center - this.Position 

        member this.CenterOrigin =
            this.Position - this.Center  

        member this.LeafNodes() =
            tree.LeafAdapters() 

        member this.Nodes() =
            tree.Adapters() 

        member this.NodeCount =
            tree.Count 

        member this.LeafesCount =
            tree.LeafesCount 

        member this.Indexe() =
            tree.AllItems() 

        member this.World = 
             this.LocalTransform()

        member this.LocalTransform() =
            createLocalTransform (translation, rotation, scale, this.OriginCenter) 

        member this.GlobalTransforms() = 
            tree.UpdatePositionsDeep(this.World) 

        member this.Direction
            with get () = direction
            and set (aValue: Vector3) =
                direction <- aValue

        member this.Velocity
            with get () = velocity
            and set (aValue) = velocity <- aValue

        member this.Node(idx) =
            _gltf.Nodes[idx]

        member this.Adapter(idx) =
            tree.WithIdx(idx) 
