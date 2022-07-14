namespace Builder
//
//  GlTf2.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

 

open SharpDX

open Base.ModelSupport
open Base.ShaderSupport
open Base.VertexDefs

open Geometry.GeometricModel3D

open Gltf2Base.Deployment
open Gltf2Base.Gltf2Reader
open Gltf2Base.BaseObject

open BuilderSupport

type Material = Base.ModelSupport.Material
type Texture = Base.ModelSupport.Texture

// ----------------------------------------------------------------------------------------------------
// Support für das Einlesen von glb-Files in der EcoGrafics Technologie
// ----------------------------------------------------------------------------------------------------
module GlTf2 =

    [<AllowNullLiteral>]
    type GlTf2Builder(name, fileName) = 
        inherit ShapeBuilder(name, fileName)
        let mutable objekt:Objekt = null
                
        member this.Build(_scale:Vector3, _material:Material, _visibility:Visibility, _augment:Augmentation, _quality:Quality, _shaders:ShaderConfiguration) =
            this.Size <- _scale            
            let gltf = getGltf(this.FileName)
            objekt <- new Objekt(this.Name, gltf, Vector3.Zero, Matrix.Identity, Vector3.One)
            Deployer.Deploy(gltf, objekt, this.FileName)

            for node in objekt.LeafNodes() do
                let mutable mesh = Deployer.Instance.MeshKatalog.GetMesh(this.Name, node.Node.Mesh.Value)
                let material = Deployer.Instance.MeshKatalog.Material(this.Name, node.Node.Mesh.Value)
                let textures = Deployer.Instance.TextureKatalog.GetTextures(this.Name, material)
                let texture  = textures |> List.find (fun text -> text.Kind = TextureTypePBR.baseColourTexture)
                this.AddPart(node.Node.Name, mesh.Vertices , mesh.Indices, _material, texture, _visibility, _shaders) 

            this.adjustXYZ()

            this.Normalize()

            this.Resize()

            this.Augment(_augment) 

        member this.Initialize() =  
            objekt <- null
            this.Name <- "" 

        member this.Read(_objectName, _path) = 
            this.Initialize()
            this.Name  <- _objectName 

        member this.AddPart(_name, _vertices, _indices, _material, _texture, _visibility, _shaders) =
            let mutable texture = Texture(_texture.Name, _texture.Info.MimeType.ToString(), _texture.Data) 
            this.Part <- 
                new Part(
                    _name,
                    new TriangularShape(_name, Vector3.Zero, _vertices, _indices, Vector3.One, Quality.High),
                    _material,
                    texture,
                    _visibility,
                    _shaders
                )
            this.Parts.Add(this.Part)

        override this.Vertices  
            with get() =
                this.Parts 
                |> Seq.map(fun p -> p.Shape.Vertices)   
                |> Seq.concat
                |> ResizeArray

        // ----------------------------------------------------------------------------------------------------
        // Normierung. Größe und Position
        // Für alle Parts und Vertices
        // ----------------------------------------------------------------------------------------------------
        override this.adjustXYZ()=
           let min = computeMinimum(this.Vertices|> Seq.toList) 
           for part in this.Parts do 
                part.Shape.Vertices <- part.Shape.Vertices |> Seq.map (fun v -> v.Shifted(-min.Position)) |> ResizeArray

        override this.Normalize() =
            let mutable aFactor = this.ComputeFactor() 
            let factor = Vector3(aFactor, aFactor, aFactor)
            for part in this.Parts do 
                part.Shape.Vertices <- part.Shape.Vertices |> Seq.map (fun v -> v.Resized(factor)) |> ResizeArray 

        override this.Resize() =  
            for part in this.Parts do 
                part.Shape.Vertices <- part.Shape.Vertices |> Seq.map (fun v -> v.Resized(this.Size)) |> ResizeArray 

        override this.ComputeFactor() =
            let minimum = computeMinimum(this.Vertices|> Seq.toList)
            let maximum = computeMaximum(this.Vertices|> Seq.toList)
            let actualHeight = maximum.Position.Y - minimum.Position.Y
            let actualDepth = maximum.Position.Z - minimum.Position.Z
            let actualWidt = maximum.Position.X - minimum.Position.X
            let mutable actualSize = max actualHeight actualWidt             
            actualSize <- max actualSize actualDepth 
            let standardHeight = 1.0f
            standardHeight / actualSize  