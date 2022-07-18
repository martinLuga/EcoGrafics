namespace Builder
//
//  Wavefront.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System
open System.Collections.Generic

open SharpDX
open SharpDX.Direct3D

open Base
open ModelSupport
open ObjectBase
open ShaderSupport

open Geometry
open GeometricModel3D

open PBRBase
open BaseObject
open NodeAdapter
open Builder

open BuilderSupport
open Conversion

open ShaderPBR
open Structures
open Shaders

// ----------------------------------------------------------------------------------------------------
// Einlesen von gltf-Files
// Objekt-Baum erzeugen
// Spezielle Model-Klassen(Textur, Material) für PBR (physically based rendering)
// ----------------------------------------------------------------------------------------------------
module PBR =

    let shaders = 
        new ShaderConfiguration(
            vertexShaderDesc=vertexShaderDesc,
            pixelShaderDesc=pixelShaderDesc,
            domainShaderDesc=ShaderDescription.CreateNotRequired(ShaderType.Domain),
            hullShaderDesc=ShaderDescription.CreateNotRequired(ShaderType.Hull)
        ) 

    // ----------------------------------------------------------------------------------------------------
    // PBRBuilder
    // ----------------------------------------------------------------------------------------------------
    type PBRBuilder(name, fileName: string, position: Vector3, rotation, scale) =
        inherit ShapeBuilder(name, fileName)
        let fileName = fileName
        let builder = new GltfBuilder(fileName)
        let gltf = builder.Gltf
        let objekt = new Objekt(name, gltf, position, rotation, scale)
        let tree = objekt.Tree

        let mutable mainObject: BaseObject = null
        let mutable display: Display = null

        member this.Build(position: Vector3, sizeFactor: Vector3, visibility: Visibility, augmentation: Augmentation) =
            objekt.Tree.printAll ()
            objekt.Tree.printAllGltf ()

            this.Convert(visibility, augmentation)
            this.adjustXYZ ()
            this.Normalize()
            this.Resize()
            this.Augment(augmentation)

            this.Result

        member this.Convert(visibility: Visibility, augmentation: Augmentation) =
            mainObject <- objekt.ToBaseObject()
            display <- Display(visibility, augmentation)
            this.Parts <- this.ToParts(tree, visibility )

        member this.ToShape(name, vertices, indices, topology, material: int) =
            let material = ToMaterial(builder, material)
            let shape =
                match topology with
                | PrimitiveTopology.TriangleList ->
                    new TriangularShape(name, Vector3.Zero, vertices, indices, Vector3.One, Quality.High) :> Shape
                | _ -> new PatchShape(name, Vector3.Zero, vertices, indices, Vector3.One, Quality.High) :> Shape

            shape, material

        member this.ToShapes(tree: NodeAdapter) =
            tree.LeafAdapters()
            |> List.map (fun node -> builder.CreateMeshData(node.Node.Name, node.Mesh))
            |> List.map (fun nameVerIndTopIdx -> this.ToShape(nameVerIndTopIdx))

        member this.ToParts(tree: NodeAdapter, visibility: Visibility ) =
            this.ToShapes(tree)
            |> List.map (fun shapeAndMaterial -> Part((fst shapeAndMaterial).Name, fst shapeAndMaterial, snd shapeAndMaterial, visibility, shaders))
            |> ResizeArray

        member this.Result =
            display.Parts <- this.Parts |> Seq.toList
            mainObject.Display <- display
            mainObject