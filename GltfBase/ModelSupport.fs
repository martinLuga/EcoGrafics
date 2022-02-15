namespace GltfBase

//
//  ModelSupport.fs
//
//  Created by Martin Luga on 08.02.20.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open Base.GeometryUtils
open Common 
open SharpDX
open System
open System.Collections.Generic
open VGltf.Types

// ----------------------------------------------------------------------------------------------------
// Objekt-Modell 
//      mit Gltf 3D Support
//      und Bullet Physics 
// 
// ----------------------------------------------------------------------------------------------------
module ModelSupport =

    exception ObjectDuplicateException of string 

    // ----------------------------------------------------------------------------------------------------
    // Basis Objekt für Darstellung und Physik
    // Objekt ist ein darzustellendes Objekt (AUto, Tier...)
    //  Node ist ein Teil davon (Räder , Flossen...) vorher Part
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type Objekt(_name: string, _gltf:Gltf, _position: Vector3, _direction: Vector3, _velocity:float32, _moveRandom:bool) =

        let mutable name = _name 
        let mutable idx=0
        let mutable position=_position
        let mutable direction=_direction
        let mutable velocity=_velocity
        let mutable rotation = Matrix.Identity
        let mutable translation = Matrix.Translation(position)

        let mutable tree:NodeAdapter = null

        do
            let rootNodes = _gltf.RootNodes |> Seq.toList
            let node:Node = rootNodes.Item(0)
            let root = node.Children[0]             
            tree <- new NodeAdapter(_gltf, root)

        new (objectName, gltf, position ) = new Objekt(objectName, gltf, position, Vector3.Zero, 0.0f,false ) 
       
        member this.Name
            with get() = name
            and set(value)  = name <- value 

        member this.Idx
            with get() = idx
            and set(value)  = idx <- value 
    
        member this.Position
            with get() = position
            and set(value)  = position <- value 

        member this.LeafNodes() =
            tree.LeafAdapters() 

        member this.Nodes() =
            tree.Adapters() 

        member this.NodeCount =
            tree.Count 

        member this.Indexe() =
            tree.AllItems() 

        member this.World = Matrix.Multiply(rotation, Matrix.Translation(position))

        member this.GlobalTransforms() = 
            let node = tree.Node            
            let transform = createLocalTransform (node.Translation , node.Rotation,  node.Scale)
            tree.UpdatePositionsDeep(transform)

        member this.Direction
            with get () = direction
            and set (aValue: Vector3) =
                direction <- aValue

        member this.Velocity
            with get () = velocity
            and set (aValue) = velocity <- aValue

        member this.Node(idx) =
            _gltf.Nodes[idx]

    // ----------------------------------------------------------------------------------------------------
    //  Scene -
    //      enthält Objekte
    //      lesen und speichern des Modells 
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type Szene(name) =
        let mutable name = name

        let mutable objects:Objekt list = []

        override this.ToString() = "Scene: " + this.Name

        member this.Name = name
                
        member this.Objects
            with get () = name
            and set (value) = name <- value

    // ----------------------------------------------------------------------------------------------------
    //  Model -
    //      enthält Scenes
    //      lesen und speichern des Modells
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type Modell(name: string) =
        let mutable name=name
        let mutable scenes=new Dictionary<string,Szene>()

        member this.Name = name
        
        member this.Scenes 
            with get() = scenes 

        member this.SceneWithName(name)= 
            scenes.TryGetValue(name)

        member this.AddSceneWithName(name, scene)= 
            scenes.Add(name, scene)

        member this.Load(name)=
            raise (new Exception("Not implemented"))

        member this.Save(name)=
            raise (new Exception("Not implemented"))
 
        override this.ToString() = "Model " + this.Name