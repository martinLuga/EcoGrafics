namespace Builder
//
//  RecordFormatOBJ.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//


open System
open System.Collections.Generic
open System.IO

open log4net



open Base
open Base.LoggingSupport 
open Base.VertexDefs

open SharpDX
open SharpDX.Direct3D
open SharpDX.Direct3D12

open VGltf
open VGltf.Types

open VJson.Schema

// ----------------------------------------------------------------------------------------------------
// Verarbeiten von glb-Files
// ---------------------------------------------------------------------------------------------------- 

module GlbFormat =

    let myMaterial(mat:Material) = 
        let a = mat.EmissiveFactor.[0]
        new ModelSupport.Material(
            name=mat.Name,
            diffuseAlbedo=Vector4.Zero,
            fresnelR0=Vector3.Zero,
            roughness=0.0f,
            ambient=Color4.White,
            diffuse=Color4.White,
            specular=Color4.White,
            specularPower=0.0f,
            emissive=Color4.White,
            hasTexture=false
        )

    let myTexture(tex:Texture) = 
        new ModelSupport.Texture(tex.Name)

    let myTopologyType(src_typ:Nullable<Types.Mesh.PrimitiveType.ModeEnum>) =
        if src_typ.HasValue then
            let typ = src_typ.Value
            match typ with
            | Types.Mesh.PrimitiveType.ModeEnum.POINTS      -> PrimitiveTopologyType.Point
            | Types.Mesh.PrimitiveType.ModeEnum.LINES       -> PrimitiveTopologyType.Line
            | Types.Mesh.PrimitiveType.ModeEnum.TRIANGLES   -> PrimitiveTopologyType.Triangle
            | _ -> raise(SystemException("Not supported"))
        else 
            PrimitiveTopologyType.Triangle

    let myTopology(src_typ:Nullable<Types.Mesh.PrimitiveType.ModeEnum>) =
        if src_typ.HasValue then
            let typ = src_typ.Value
            match typ with
            | Types.Mesh.PrimitiveType.ModeEnum.POINTS      -> PrimitiveTopology.PointList
            | Types.Mesh.PrimitiveType.ModeEnum.LINES       -> PrimitiveTopology.LineList
            | Types.Mesh.PrimitiveType.ModeEnum.TRIANGLES   -> PrimitiveTopology.TriangleList
            | _ -> raise(SystemException("Not supported"))
        else 
            PrimitiveTopology.TriangleList

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

    let getContainer (fileName: string) =

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