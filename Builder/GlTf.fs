namespace Builder
//
//  Wavefront.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open Base
open Base.Framework 
open Base.ModelSupport
open Base.ShaderSupport 
open Base.VertexDefs
open Geometry.GeometricModel
open VGltf
open VGltf.Types
open GlbFormat
open SharpDX
open SharpDX.Direct3D
open SharpDX.Direct3D12 
open System.Collections.Generic 
open GltfBase.Deployment

open VGltf
open VGltf.Types
    
open GltfBase.VGltfReader
open GltfBase.Gltf2Reader
open GltfBase.BaseObject
open GltfBase.Running

// ----------------------------------------------------------------------------------------------------
// Support für das Einlesen von glb-Files
// ----------------------------------------------------------------------------------------------------
module GlTf =

    let correctorGltf(path) = getGltf (path)

    [<AllowNullLiteral>]
    type GlTfBuilder() = 
        let mutable objekt:Objekt = null
        let mutable objectName = "" 
        let mutable gltf:Gltf = null
        let mutable container:GltfContainer = null
        let mutable store:ResourcesStore = null

        // ----------------------------------------------------------------------------------------------------
        // Singleton
        // ----------------------------------------------------------------------------------------------------         
        static let mutable instance = null 
        static member Instance
            with get() = 
                if instance = null then
                    instance <- new GlTfBuilder()
                instance
            and set(value) = instance <- value

        static member Build(_objectName, _path:string, _position:Vector3, _rotation:Vector4, _scale:Vector3) =
            GlTfBuilder.Instance.Build(_objectName, _path, _position, _rotation, _scale)

        member this.Build( _objectName, _path:string, _position:Vector3, _rotation:Vector4, _scale:Vector3) =  
            let correctorGtlf = correctorGltf(_path)
            store <- GlTfBuilder.Instance.Read(_objectName, _path)
            objekt <- new Objekt(objectName, store.Gltf, _position, _rotation, _scale)   // Objekt ist durch gltf initialisiert
            Deployer.Deploy(objekt, store, correctorGtlf)
            Runner.AddObject(objekt)

        member this.Initialize() =  
            objekt <- null
            objectName <- "" 
            gltf <- null
            store <- null

        member this.Read(_objectName, _path) = 
            this.Initialize()
            objectName  <- _objectName
            store       <- getStore(_path)
            gltf        <- store.Gltf
            container   <- store.Container 
            store