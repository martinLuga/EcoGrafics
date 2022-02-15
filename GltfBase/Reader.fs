namespace GltfBase
//
//  Reader.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//
 
open System.IO

// ----------------------------------------------------------------------------------------------------
// Einlesen von glb-Files mit VGltf
// ---------------------------------------------------------------------------------------------------- 
module VGltfReader =
    
    open VGltf
    open VJson.Schema

    let getStore (fileName: string) =

       using (new FileStream(fileName, FileMode.Open, FileAccess.Read) ) (fun fs ->
            let container = GltfContainer.FromGlb(fs) 
            let schema = VJson.Schema.JsonSchema.CreateFromType<Types.Gltf>(container.JsonSchemas) 
            let ex = schema.Validate(container.Gltf, container.JsonSchemas) 
            if ex <> null then
                raise ex
            let loader = new ResourceLoaderFromEmbedOnly() 
            let store = new ResourcesStore(container, loader) 
            store
        )

// ----------------------------------------------------------------------------------------------------
// Einlesen von glb-Files mit Gltf2
// ---------------------------------------------------------------------------------------------------- 
module Gltf2Reader =
    
    open glTFLoader

    let getGltf (fileName: string) =
        let result = Interface.LoadModel(fileName)
        result