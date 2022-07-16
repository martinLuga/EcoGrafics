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
open Base.MeshObjects 
open Base.VertexDefs

open SharpDX
open SharpDX.Direct3D
open SharpDX.Direct3D12

open glTFLoader.Schema

open Gltf2Base.Builder

// ----------------------------------------------------------------------------------------------------
// Verarbeiten von glb-Files
// ---------------------------------------------------------------------------------------------------- 
module PBRFormat =

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

    let CreateMesh(gltfBuilder:GltfBuilder, mesh) =
        let  (_name, _vertices, _indices, _topology, _mat)  = gltfBuilder.CreateMeshData(mesh)
        let vertices = _vertices
        let indices = _indices
        MeshData(vertices.ToArray(), indices.ToArray())

    let Mesh(gltfBuilder, gltf:Gltf, imesh) =
        let mesh = gltf.Meshes[imesh] 
        CreateMesh(gltfBuilder, mesh)

    //let Material(gltf, imaterial) =
    //    // Material
    //    let material = gltf.Materials[imaterial]
    //    let roughness = material.PbrMetallicRoughness 
    //    let bct = roughness.BaseColorTexture 
    //    let bcti = bct.Index 
    //    let mf = roughness.MetallicFactor 
    //    new Material()

    //let Texture(gltf:Gltf, itexture) =
    //    let texture = gltf.Textures[itexture]
    //    new Texture(texture)

    //let CreateMaterials(gltf:Gltf, cmaterials) = 
    //    for cmat in gltf.Materials do
    //        let myMaterial = Material(cmat.Name)
    //        materials.Add(myMaterial.Name, myMaterial)   
            
    //let CreateTextures(ctextures) =
    //    materials.Clear()
    //    for ctex in gltf.Textures do
    //        let myTexture = Texture(ctex)
    //        textures.Add(myTexture.Name, myTexture)

    //let CreateImages () =
    //    for  i in 0.. gltf.Images.Length-1 do
    //        let img = gltf.Images[i];
    //        let image = gltfBuilder.CreateImage(img.Name)  
    //        images.Add(img.Name, snd image) 