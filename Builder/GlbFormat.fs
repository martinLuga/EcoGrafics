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

    let fileLogger = LogManager.GetLogger("File")
    let logFile  = Debug(fileLogger)

    // ----------------------------------------------------------------------------------------------------
    // Conversion
    // ----------------------------------------------------------------------------------------------------     
    let fromArray3(x:float32[]) =
        Vector3( x.[0],   x.[1],   x.[2])

    let fromArray2(x:float32[]) =
        Vector2( x.[0], x.[1])

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

    let GetMeshes(fileName: string) =
        let container = 
            getGlbContainer (fileName )
        container.Gltf.Meshes

    let GetNodes(fileName: string) =
        let meshes = GetMeshes(fileName )
        let firstMesh = meshes.Item(0)
        let nodes = firstMesh.Primitives
        nodes

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

    // ---------------------------------------------------------------------------------------------------- 
    // Provide GlTF Services
    // ---------------------------------------------------------------------------------------------------- 
    type Worker(fileName: string) =
        
        let mutable store:ResourcesStore = null
        let mutable gltf:Gltf = null
        let mutable loader:ResourceLoaderFromEmbedOnly = null

        let mutable size = Vector3.One

        let mutable fileName        = fileName 
        let mutable vertices        = new List<Vertex>()
        let mutable indices         = new List<int>()        
        let mutable materials       = new Dictionary<string, ModelSupport.Material>()
        let mutable textures        = new Dictionary<string, ModelSupport.Texture>()
        let mutable topologyType    = PrimitiveTopologyType.Triangle

        do 
            let _store = getContainer(fileName)
            store       <- _store           // Der Store enthält alles
            gltf        <- store.Gltf       // Gltf zeigt in den Store

        member this.Vertices 
            with get() = vertices

        member this.Indices 
            with get() = indices

        member this.Initialize(_generalSizeFactor) =
            size <- _generalSizeFactor

            // Node
            let gltf = store.Gltf 
            let sceneIdx = gltf.Scene 
            let scenes = gltf.Scenes 
            let scene = scenes.Item(sceneIdx.Value)

            // Node
            let rootNodes = gltf.RootNodes |> Seq.toList
            let node:Node = rootNodes.Item(0)
            let childNode = gltf.Nodes.Item(node.Children[0])

            // Mesh
            let mesh = gltf.Meshes[childNode.Mesh.Value]
            let primitive = mesh.Primitives[0]
            topologyType <- myTopologyType(primitive.Mode)

            // Material
            let material = gltf.Materials[primitive.Material.Value]
            let roughness = material.PbrMetallicRoughness 
            let bct = roughness.BaseColorTexture 
            let bcti = bct.Index 
            let mf = roughness.MetallicFactor 

            // Material
            let material = gltf.Materials[primitive.Material.Value]
            let roughness = material.PbrMetallicRoughness 
            let bct = roughness.BaseColorTexture 
            let bcti = bct.Index 
            let mf = roughness.MetallicFactor 

            // Textures
            let texture = gltf.Textures[bcti]

            // Vertex
            let normalBuffer = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["NORMAL"])             
            let normalen = normalBuffer.GetEntity<float32, Vector3> (fromArray3) 
            let ueberAlleNormalen  = normalen.GetEnumerable().GetEnumerator()

            let posBuffer  = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["POSITION"])
            let positionen = posBuffer.GetEntity<float32, Vector3> (fromArray3) 
            let ueberAllePositionen  = positionen.GetEnumerable().GetEnumerator()

            let texCoordBuffer = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Attributes["TEXCOORD_0"])
            let alleTexCoord = texCoordBuffer.GetEntity<float32, Vector2> (fromArray2) 
            let ueberAlleTexCoords  = alleTexCoord.GetEnumerable().GetEnumerator()

            while ueberAllePositionen.MoveNext() && ueberAlleNormalen.MoveNext() && ueberAlleTexCoords.MoveNext()  do
                let pos = ueberAllePositionen.Current * size
                let norm = ueberAlleNormalen.Current
                let tex = ueberAlleTexCoords.Current
                let vertex = new Vertex(pos, norm , Color4.White, tex)
                vertices.Add(vertex)

            // Index
            let indicies = store.GetOrLoadTypedBufferByAccessorIndex(primitive.Indices.Value)
            indices.AddRange(indicies.GetPrimitivesAsCasted<int>())

        member this.CreateMaterials(cmaterials) =
            materials.Clear()
            for cmat in gltf.Materials do
                let myMaterial = myMaterial(cmat)
                materials.Add(myMaterial.Name, myMaterial)   
                
        member this.CreateTextures(ctextures) =
            materials.Clear()
            for ctex in gltf.Textures do
                let myTexture = myTexture(ctex)
                textures.Add(myTexture.Name, myTexture)

        member this.CreateImages () =
            for  i in 0.. gltf.Images.Count-1 do
                let img = gltf.Images[i];
                let imgResN = store.GetOrLoadImageResourceAt(i) 
                let myTexture = new ModelSupport.Texture(img.Name, img.Uri, "", i, false)
                textures.Add(myTexture.Name, myTexture)