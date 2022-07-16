namespace Builder
//
//  Wavefront.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System
open System.Collections.Generic
open System.Drawing

open SharpDX
open SharpDX.Direct3D
open SharpDX.Direct3D12

open glTFLoader.Schema

open Base
open Base.ModelSupport
open Base.VertexDefs

open BuilderSupport

open Gltf2Base
open Gltf2Reader
open BaseObject

// ----------------------------------------------------------------------------------------------------
// Support für das Einlesen von gltf-Files (physically based rendering)
// ----------------------------------------------------------------------------------------------------
module PBR =
    [<AllowNullLiteral>]
    // ----------------------------------------------------------------------------------------------------
    // Conver gltf structure to BaseObject
    // ----------------------------------------------------------------------------------------------------
    type Converter(object: Objekt) =
        let mutable parts: List<Part> = new List<Part>()
        let mutable generalSizeFactor = Vector3.One
        let mutable augmentation = Augmentation.None
        let mutable isTransparent = false
        let mutable vertices = new List<Vertex>()
        let mutable indices = new List<int>()
        let mutable materials = new Dictionary<string, ModelSupport.Material>()
        let mutable textures = new Dictionary<string, ModelSupport.Texture>()
        let mutable images = new Dictionary<string, Drawing.Image>()
        let mutable topologyType = PrimitiveTopologyType.Triangle

        member this.Convert(sizeFactor, visibility, augmentation) = [ new Part() ]

    // ----------------------------------------------------------------------------------------------------
    // GeometryBuilder
    // ----------------------------------------------------------------------------------------------------
    type PBRBuilder(name, fileName: string) =
        inherit ShapeBuilder(name, fileName)
        let fileName = fileName
        let gltf: Gltf = getGltf (fileName)

        let objekt: Objekt =
            new Objekt(name, gltf, Vector3.Zero, Matrix.Identity, Vector3.One)

        let mutable converter: Converter = new Converter(objekt)

        member this.Build(sizeFactor: Vector3, visibility: Visibility, augmentation: Augmentation) =

            objekt.Tree.printAll ()

            objekt.Tree.printAllGltf ()

            // Create ecoGrafics Objects
            base.Parts <-
                converter.Convert(visibility, augmentation, sizeFactor)
                |> ResizeArray

        member this.Parts = base.Parts |> Seq.toList
