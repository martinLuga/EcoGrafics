namespace GraficBase
//
//  Material.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open SharpDX


open System
open System.Runtime.InteropServices
open SharpDX.Direct3D12

open System.IO
open System.Linq

open SharpDX
open SharpDX.D3DCompiler
open SharpDX.Direct3D
open SharpDX.Mathematics.Interop
open SharpDX.DXGI

// ----------------------------------------------------------------------------------------------------
//  Material 
//    
//    
// ----------------------------------------------------------------------------------------------------

// ----------------------------------------------------------------------------------------------------
// Material aus Cookbook  
// ----------------------------------------------------------------------------------------------------
module MaterialCookbook =
    open Base.ModelSupport

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type MaterialConstants =
        struct   
            val mutable Ambient:  Color4           // 16 bytes       
            val mutable Diffuse:  Color4           // 16 bytes         
            val mutable Specular: Color4           // 16 bytes  
            val mutable SpecularPower: float32     // 4 bytes
            val mutable HasTexture: RawBool        // 4 bytes          
            val mutable _padding0: Vector2         // 8 bytes
            val mutable Emissive: Color4           // 16 bytes 
            val mutable UVTransform: Matrix        // 16 bytes     
            new(ambient, diffuse, specular, specularPower, hasTexture, emissive, uVTransform) = 
                {Ambient = ambient; Diffuse = diffuse; Specular = specular; SpecularPower=specularPower; HasTexture=hasTexture; _padding0=Vector2.Zero; Emissive=emissive; UVTransform=uVTransform }
            end
    
    type Material1() =
        member this.getMaterialConstants(material:Material) =
            let mutable newMaterial = 
                new MaterialConstants( 
                    Ambient = material.Ambient,
                    Diffuse = material.Diffuse,
                    Specular = material.Specular,
                    SpecularPower = material.SpecularPower,
                    Emissive = material.Emissive,
                    HasTexture = RawBool(material.HasTexture),  
                    UVTransform = Matrix.Identity
                ) 
            newMaterial

module MaterialIntroduction =
    open Base.ModelSupport
    // ----------------------------------------------------------------------------------------------------
    // Material aus Introduction to 3D Programming 
    // ----------------------------------------------------------------------------------------------------
    [<type:StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type MaterialConstants =
        struct
        val  DiffuseAlbedo:Vector4
        val  FresnelR0:Vector3
        val  Roughness:float32
        val  MatTransform:Matrix            
        new (diffuseAlbedo, fresnelR0,roughness, matTransform) =
            {DiffuseAlbedo=diffuseAlbedo; FresnelR0=fresnelR0; Roughness=roughness; MatTransform=matTransform} 
    end

    type Material2() =
        member this.getMaterialConstants(material:Material) =
            let mutable newMaterial = 
                new MaterialConstants( 
                    diffuseAlbedo=material.DiffuseAlbedo,
                    fresnelR0=material.FresnelR0,
                    roughness=material.Roughness,
                    matTransform=material.MatTransform
                ) 
            newMaterial

// ----------------------------------------------------------------------------------------------------
// Material für Gltf 
// ----------------------------------------------------------------------------------------------------
module MaterialPBR =
    open VGltf
    open VGltf.Types
    open VGltf.Types  
    open VGltf.Ext
    open VGltf.Glb

    type Roughness() =
        let mutable BaseColorFactor: float32[] = [|0.0f; 0.0f|]        
        let mutable BaseColorTexture: Material.BaseColorTextureInfoType = null       
        let mutable MetallicFactor: float32  = 0.0f       
        let mutable MetallicRoughnessTexture: Material.MetallicRoughnessTextureInfoType  = null          
        let mutable RoughnessFactor: float32 = 0.0f

        member this.Gltf =
            let r = new Material.PbrMetallicRoughnessType()
            r.BaseColorFactor           <- BaseColorFactor       
            r.BaseColorTexture          <- BaseColorTexture      
            r.MetallicFactor            <- MetallicFactor       
            r.MetallicRoughnessTexture  <- MetallicRoughnessTexture      
            r.RoughnessFactor           <- RoughnessFactor

    type MaterialGltf() =
        let mutable AlphaCutoff:float32 = 0.0f
        let mutable AlphaMode = Material.AlphaModeEnum.Opaque
        let mutable DoubleSided = false
        let mutable EmissiveFactor: float32[]  = [|0.0f; 0.0f|] 
        let mutable EmissiveTexture:Material.EmissiveTextureInfoType = null 
        let mutable NormalTexture:Material.NormalTextureInfoType = null 
        let mutable OcclusionTexture:Material.OcclusionTextureInfoType = null  
        let mutable PbrMetallicRoughness:Material.PbrMetallicRoughnessType = null 

        member this.Gltf =
            let mat = new Material()
            mat.AlphaCutoff             <- 0.0f 
            mat.AlphaMode               <- Material.AlphaModeEnum.Opaque 
            mat.DoubleSided             <- false 
            mat.EmissiveFactor          <- [|0.0f; 0.0f|] 
            mat.EmissiveTexture         <- new Material.EmissiveTextureInfoType() 
            mat.NormalTexture           <- new Material.NormalTextureInfoType()
            mat.OcclusionTexture        <- new Material.OcclusionTextureInfoType() 
            mat.PbrMetallicRoughness    <- new Material.PbrMetallicRoughnessType()

// ----------------------------------------------------------------------------------------------------
// Material zusammen
// ----------------------------------------------------------------------------------------------------
module Material =
    open Base.ModelSupport
    open MaterialCookbook

    type MaterialWrapper (material:Material) =
        let mutable material = material
        let mutable wrapper = new Material1()
        member this.GetConstant(material:Material) =
            wrapper.getMaterialConstants(material)

            
            
        

 


