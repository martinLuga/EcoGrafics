namespace Gltf2Base
//
//  Reader.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//
 
open System.IO

// ----------------------------------------------------------------------------------------------------
// Einlesen von glb-Files mit Gltf2
// ---------------------------------------------------------------------------------------------------- 
module Gltf2Reader =
    
    open glTFLoader

    let getGltf (fileName: string) =
        let result = Interface.LoadModel(fileName)
        result