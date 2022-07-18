namespace Builder
//
//  Wavefront.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open SharpDX
open SharpDX.Direct3D

open Base
open ModelSupport
open ObjectBase
open ShaderSupport  

open Geometry
open GeometricModel3D

open Gltf2Base
open BaseObject
open NodeAdapter
open Builder

open BuilderSupport
open Conversion

open ShaderRenderingCookbook 
open Shaders

// ----------------------------------------------------------------------------------------------------
// Einlesen von gltf-Files (EcoGrafics)
// Objekt-Baum erzeugen
// Konvertieren
// ----------------------------------------------------------------------------------------------------
module GLTF2 =

    let shaders = 
        new ShaderConfiguration(
            vertexShaderDesc=vertexShaderDesc,
            pixelShaderDesc=pixelShaderPhongDesc,
            domainShaderDesc=ShaderDescription.CreateNotRequired(ShaderType.Domain),
            hullShaderDesc=ShaderDescription.CreateNotRequired(ShaderType.Hull)
        ) 

    let log(objekt:Objekt) = 
        objekt.Tree.printAll ()
        objekt.Tree.printAllGltf ()

    // ----------------------------------------------------------------------------------------------------
    // PBRBuilder
    // ----------------------------------------------------------------------------------------------------
    type GlTf2Builder(name:string, fileName: string) =
        inherit ShapeBuilder(name, fileName)
        let fileName = fileName
        let builder = new GltfBuilder(fileName)
        let gltf = builder.Gltf
        let mutable objekt:Objekt = null
        let mutable tree:NodeAdapter = null
        let mutable mainObject: BaseObject = null
        let mutable display: Display = null

        member this.Build(position: Vector3, rotation:Matrix, scale: Vector3, visibility: Visibility, augmentation: Augmentation) =            
            objekt <- Objekt(name, gltf, position, rotation, scale)

            this.ToBaseObject(visibility, augmentation)

            this.adjustXYZ ()
            this.Normalize()
            this.Resize()
            this.Augment(augmentation)

            this.Result

        member this.ToBaseObject(visibility: Visibility, augmentation: Augmentation) =
            mainObject <- objekt.ToBaseObject()
            display <- Display(visibility, augmentation)            
            tree <- objekt.Tree
            this.Parts <- this.ToParts(tree, visibility )

        member this.ToParts(tree: NodeAdapter, visibility: Visibility ) =
            this.ToShapes(tree, visibility)
            |> List.map (fun shapeMaterialTexture -> this.ToPart(shapeMaterialTexture, visibility))
            |> ResizeArray

        member this.ToShapes(tree: NodeAdapter, visibility: Visibility) =
            let isTransparent = TransparenceFromVisibility(visibility)
            tree.LeafAdapters()
            |> List.map (fun node -> builder.CreateMeshData(node.Node.Name, node.Mesh, isTransparent))
            |> List.map (fun nameVerIndTopoMat -> this.ToShape(nameVerIndTopoMat))

        member this.ToShape(name, vertices, indices, topology, material: int) =
            let material, texture = ToMaterialAndTexture(builder, material)
            let shape =
                match topology with
                | PrimitiveTopology.TriangleList ->
                    new TriangularShape(name, Vector3.Zero, vertices, indices, Vector3.One, Quality.High) :> Shape
                | _ -> new PatchShape(name, Vector3.Zero, vertices, indices, Vector3.One, Quality.High) :> Shape

            name, shape, material, texture

        member this.ToPart((name, shape:Shape, material:MaterialBase, texture:TextureBase), visibility: Visibility) =
            Part(name, shape, material, texture, visibility)

        member this.Result =
            display.Parts <- this.Parts |> Seq.toList
            mainObject.Display <- display
            mainObject