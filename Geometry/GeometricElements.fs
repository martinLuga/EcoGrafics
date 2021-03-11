namespace Geometry
//
//  GeometricElements.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open SharpDX 
open SharpDX.Direct3D 

open Base.GlobalDefs
open Base.Logging

open DirectX.MeshObjects
open DirectX.MathHelper

open VertexSphere
open VertexCube
open VertexCylinder
open VertexPyramid
open VertexPatch
open VertexDreiD

/// <summary>
/// Geometrische Elemente
/// Material
/// Textur
/// Surface
/// </summary>
module GeometricElements = 
    let logger = LogManager.GetLogger("Geometric.Elements")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)
    let logWarn  = Warn(logger)
    let logError = Error(logger)

    /// <summary>
    /// Material
    /// </summary>
    type Material(name:string, diffuseAlbedo:Vector4, fresnelR0:Vector3, roughness:float32, 
        ambient:Color4, diffuse:Color4, specular:Color4, specularPower:float32, emissive: Color4) =
        let mutable ambient = ambient
        let mutable diffuse = diffuse
        let mutable specular = specular
        let mutable emissive = emissive
        member this.Name=name
        member this.DiffuseAlbedo=diffuseAlbedo 
        member this.FresnelR0=fresnelR0
        member this.Roughness=roughness 
        member this.SpecularPower=specularPower

        new () = Material("", Vector4.Zero, Vector3.Zero, 0.0f)

        new (name:string, diffuseAlbedo:Vector4, fresnelR0:Vector3, roughness:float32) =
            Material(name,  diffuseAlbedo, fresnelR0, roughness, 
                Color4.White, Color4.White, Color4.White, 0.0f, Color4.White)

        new (name:string, ambient:Color4, diffuse:Color4, specular:Color4, specularPower:float32, emissive: Color4) =
            Material(name,  Vector4.Zero, Vector3.Zero, 0.0f, 
                ambient, diffuse, specular, specularPower, emissive)

        override this.ToString() =
            "Material: " + this.Name
            
        member this.Ambient
            with get() = ambient
            and set(value) = ambient <- value
                
        member this.Diffuse
            with get() = diffuse
            and set(value) = diffuse <- value

        member this.Emissive
            with get() = emissive
            and set(value) = emissive <- value
        
        member this.Specular
            with get() = specular
            and set(value) = specular <- value

        member this.isEmpty() =
           this.Name = ""
    
    [<AllowNullLiteral>] 
    type Texture(name:string, application:string, directory:string, fileName:string) =
        member this.Name=name
        member this.Application=application
        member this.Directory=directory   
        member this.FileName=fileName
        new () = Texture("", "", "", "")
        member this.isEmpty() =
           this.Name = "" 
        member this.PathName() =
            this.Application + this.Directory + this.FileName

    type Visibility = | Opaque | Transparent 

    type Surface(texture:Texture, material:Material, visibility:Visibility) =
        member this.Texture=texture
        member this.Material=material 
        member this.Visibility=visibility
        
        new () = Surface(Texture(), Material(), Visibility.Opaque)
        new (material:Material) = Surface(null, material, Visibility.Opaque)
        new (material:Material, visibility) = Surface(null, material, visibility)
        new (material, texture) = Surface(material, texture, Visibility.Opaque)

        member this.Copy () = new Surface(texture, material, visibility)
        member this.hasTexture() =
           not (this.Texture = null)
        member this.hasMaterial() =
           not (this.Material.isEmpty())
        member this.isEmpty() =
           this.Texture.isEmpty() && this.Material.isEmpty()

    let seiteVonQuadrat( p1: Vector3, p2: Vector3, p3: Vector3, p4: Vector3) =
        max (max ((p2 - p1).Length()) ((p3 - p2).Length()))
            (max ((p4 - p3).Length()) ((p1 - p4).Length()))