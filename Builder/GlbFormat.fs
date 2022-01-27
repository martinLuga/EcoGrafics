namespace Builder
//
//  RecordFormatOBJ.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Collections.Generic
open System.IO

open log4net

open SharpDX

open Base.RecordSupport 
open Base.LoggingSupport
open Base.VertexDefs

open VGltf

// ----------------------------------------------------------------------------------------------------
// Verarbeiten von glb-Files
// ---------------------------------------------------------------------------------------------------- 

module GlbFormat =

    let fileLogger = LogManager.GetLogger("File")
    let logFile  = Debug(fileLogger)

    let getGlbContainer (fileName: string) =
        let mutable container = 
            using (new FileStream(fileName, FileMode.Open, FileAccess.Read) )(fun fs ->
                GltfContainer.FromGlb(fs)  
            )
        container

    let getGltfContainer (fileName: string) =
        let mutable container = 
            using (new FileStream(fileName, FileMode.Open, FileAccess.Read) )(fun fs ->
                GltfContainer.FromGltf(fs) 
            )
        container
    
    let loader = new ResourceLoaderFromEmbedOnly() 
    
    let getStore(c:GltfContainer, loader:ResourceLoaderFromEmbedOnly) = 
        new ResourcesStore(c, loader) 

    let GetMeshes(fileName: string) =
        let container = 
            getGlbContainer (fileName )
        container.Gltf.Meshes

    let GetNodes(fileName: string) =
        let meshes = GetMeshes(fileName )
        let firstMesh = meshes.Item(0)
        let nodes = firstMesh.Primitives
        nodes

