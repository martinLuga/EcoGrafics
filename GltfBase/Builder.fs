namespace GltfBase
//
//  Builder.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open SharpDX

open Deployment

// ----------------------------------------------------------------------------------------------------
// Support für das Einlesen von glb-Files mit VGltf
// ---------------------------------------------------------------------------------------------------- 
module Build =

    open VGltf
    open VGltf.Types
    
    open VGltfReader
    open Gltf2Reader
    open ModelSupport
    open Running

    let correctorGltf(path) = getGltf (path)

    [<AllowNullLiteral>]
    type Builder() = 
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
                    instance <- new Builder()
                instance
            and set(value) = instance <- value

        static member Build(_objectName, _path:string, _position:Vector3, _scale:Vector3) =
            Builder.Instance.Build(_objectName, _path, _position, _scale)

        static member Reset() =
            Builder.Instance.Initialize()
            Runner.Reset()

        member this.Build( _objectName, _path:string, _position:Vector3, _scale:Vector3) =  
            let correctorGtlf = correctorGltf(_path)
            store <- Builder.Instance.Read(_objectName, _path)
            objekt <- new Objekt(objectName, store.Gltf, _position, _scale)  
            // Objekt ist durch gltf initialisiert
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