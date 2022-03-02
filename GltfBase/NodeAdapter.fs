namespace GltfBase
//
//  NodeAdapter.fs
//
//  Created by Martin Luga on 10.09.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System.Collections.Generic

open Base.GeometryUtils
open Base.ShaderSupport
open Base.PrintSupport

open log4net

open SharpDX

open Base.LoggingSupport

open VGltf.Types
 
module NodeAdapter = 

    let TEST_MESH_IDX = 0  

    let logger = LogManager.GetLogger("Adapter")
    let logDebug = Debug(logger)
    let logInfo = Info(logger)
    let logError = Error(logger)
    let logWarn = Warn(logger)

    // ----------------------------------------------------------------------------------------------------
    // NodeAdapter
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteralAttribute>]
    type NodeAdapter(_gltf:Gltf, _idx:int) =
        let gltf = _gltf
        let idx  = _idx
        let mutable node:Node = null
        let mutable children: NodeAdapter list = []
        let mutable shaderDefines = new List<ShaderDefinePBR>()

        do 
            node <- gltf.Nodes[idx]
            let childreni = node.Children 
            children <- 
                if childreni <> null then
                    childreni |> Seq.map(fun i -> new NodeAdapter(gltf, i) )|> Seq.toList
                else 
                    []

        override this.ToString() =
            if node = null then this.instantiate()
            node.Name

        member this.Idx = _idx

        member this.Node = node

        member this.ShaderDefines
            with get() = shaderDefines
            and set(value) = shaderDefines <- value

        member this.Children = children

        member this.GetChildren() = 
            this.instantiate()
            children

        member this.AllItems()      = 
            this.Items(this.Idx) |> Seq.toList

        member this.AllNodes()      = this.Nodes(this.Idx) |> Seq.toList

        member this.Count           = this.AllItems().Length

        member this.LeafesCount     = (this.LeafAdapters() ).Length

        member this.instantiate() = 
            if node = null then 
                node <- gltf.Nodes[idx]
                let childreni = node.Children 
                children <- 
                    if childreni <> null then
                        childreni |> Seq.map(fun i -> new NodeAdapter(gltf, i) )|> Seq.toList
                    else 
                        []

        member this.instantiateAll() = 
            this.instantiate()
            for child in this.GetChildren() do
                child.instantiateAll()

        member this.printAll() = 
            printfn " All Nodes " 
            this.printAllIdent("---")

        member this.printAllIdent(ident:string) = 
            let m = if node.Mesh.HasValue then node.Mesh.Value.ToString() else ""
            printfn "%s %s %s" ident node.Name m 
            printfn " %s" (dmatrix(node.Matrix, 4,4))

            let nextIdent = ident + ident
            for child in this.GetChildren() do
                child.printAllIdent(nextIdent)

        member this.printAllGltf() =             
            printfn " All Leaf-Nodes from Gltf" 
            this.printAllIdentGltf("--", idx)

        member this.printAllIdentGltf(ident:string, idx:int) = 
            let mutable meshStr = ""
            let mutable matStr = ""
            let mutable texStr = ""
            let mutable nextIdent = ""
            let node =  gltf.Nodes[idx]

            let childreni = node.Children 
            if childreni <> null then
                for childi in childreni do
                    this.printAllIdentGltf(nextIdent, childi)
            else
                if node.Mesh.HasValue then 
                    meshStr <- node.Mesh.Value.ToString() 
                    let mesh = gltf.Meshes[node.Mesh.Value]
                    let prim = mesh.Primitives[0]
                    if prim.Material.HasValue then
                        let mati = prim.Material.Value
                        let mat = gltf.Materials[mati]
                        matStr <- mat.Name
                        texStr <- mat.PbrMetallicRoughness.BaseColorTexture.Index.ToString() 
                    else
                        matStr <- ""
                else             
                    meshStr <- "" 
                    matStr <- ""
                printfn "%s %s %s %s %s" ident node.Name meshStr matStr texStr
                nextIdent <- ident + ident

        // All leafes as int[] recursively
        member this.Items(idx) =
            let node = gltf.Nodes[ idx ]
            if node.Children <> null then
                node.Children
                |> Seq.append (
                    node.Children
                    |> Seq.collect (fun child -> this.Items(child))
                )
            else
                [ idx ]

        // All gltf-nodes recursively
        member this.Nodes(idx)  = 
            let mynode = gltf.Nodes[ idx ]
            if mynode.Children <> null then
                mynode.Children |> Seq.map (fun i -> gltf.Nodes[i])
                |> Seq.append (                   
                    mynode.Children
                    |> Seq.collect (fun i  -> this.Nodes(i))                
                )
            else
                [ mynode ]

        // All Adapters (leafes) recursively
        member this.LeafAdapters():NodeAdapter list  =  
            this.instantiate()
            if this.Children.Length > 0 then                 
                this.Children
                |> List.collect (fun ada -> ada.LeafAdapters())            
            else
                [this]

        // All Adapters recursively
        member this.Adapters()  =  
            this.instantiate()
            if this.Children.Length > 0 then 
                [this]
                |> List.append( 
                    this.Children
                    |> List.collect (fun ada -> ada.Adapters())
                )            
            else
                [this]

        member this.WithIdx(_idx)  =  
            this.Adapters()
                |> List.find (fun ada -> ada.Idx = _idx)

        member this.UpdatePositionsDeep(objectWorld) =
            this.UpdatePos (this.Idx, objectWorld)

        member this.UpdatePos (idx, _parentMatrix:Matrix) =
            let mynode = gltf.Nodes[ idx ]
            let myTransform = createLocalTransform (mynode.Translation , mynode.Rotation,  mynode.Scale) 
            let newMatrix = myTransform * _parentMatrix
            mynode.Matrix <- newMatrix.ToArray()

            if mynode.Children <> null then
                mynode.Children 
                    |> Seq.iter (fun i -> this.UpdatePos(i, newMatrix))

        member this.Mesh = 
            if node.Mesh.HasValue then 
                gltf.Meshes[node.Mesh.Value] 
            else null
