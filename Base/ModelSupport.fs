namespace Base
//
//  ModelSupport.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
//

open System.Collections.Generic

open SharpDX
open SharpDX.Direct3D 
open SharpDX.Direct3D12

open Base.Framework

open ShaderSupport 
open MathSupport
open MeshObjects
open VertexDefs
open GameTimer

// ----------------------------------------------------------------------------------------------------
// 
// 
// 
// ----------------------------------------------------------------------------------------------------
module ModelSupport =

    type Quality =
        | Low
        | Medium
        | High
        | Original

    let qualityFactor(count, quality) =
        let mutable targetCount = 
            match quality with
            | Original  -> count
            | High      -> 1000
            | Medium    -> 100
            | Low       -> 10 

        if count <= targetCount then 
            1
        else 
            targetCount / count


    let ApplyQuality(vertexe:List<'T>, quality:Quality) =
        let factor = qualityFactor(vertexe.Count, quality)
        everyNth factor vertexe |> ResizeArray<'T>

    type Augmentation =
        | Hilite
        | Dotted
        | Blinking
        | None

    type Visibility =
        | Opaque
        | Transparent
        | Invisible

    let  blendTypeFromVisibility(visibility:Visibility) =
        match visibility with 
        | Visibility.Opaque       -> BlendType.Opaque
        | Visibility.Transparent  -> BlendType.Transparent
        | Visibility.Invisible    -> BlendType.Transparent

    let blendStateFromVisibility(visibility:Visibility) =
        match visibility with 
        | Visibility.Opaque       -> blendStateOpaque
        | Visibility.Transparent  -> blendStateTransparent
        | Visibility.Invisible    -> blendStateTransparent

    let blendDescriptionFromVisibility(visibility:Visibility) =
        match visibility with 
        | Visibility.Opaque       -> BlendDescription(BlendType.Opaque, blendStateOpaque)
        | Visibility.Transparent  -> BlendDescription(BlendType.Transparent, blendStateTransparent)
        | Visibility.Invisible    -> BlendDescription(BlendType.Transparent, blendStateTransparent)

    let TransparenceFromVisibility(visibility:Visibility) =
        match visibility with 
        | Visibility.Opaque       -> false
        | Visibility.Transparent  -> true
        | _ -> false

    type TesselationMode = 
        TRI | QUAD | BEZIER | NONE

    [<AllowNullLiteral>]
    type Texture(name: string, fileName: string, pathName: string) =
        let mutable name=name
        let mutable fileName=fileName
        let mutable path = pathName 

        new() = Texture("", "", "") // Null Declaration

        member this.Name = name
        member this.FileName = fileName
        member this.Path =
            if path <> "" then path
            else "textures/" + this.FileName

        member this.isEmpty = this.Name = "" 

        override this.ToString() = "Texture " + (if this.isEmpty then " Empty " else this.Name)
    
    // ----------------------------------------------------------------------------------------------------
    //  Material - Parameter zur Beschreibung der Lichtsituation
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type Material
        (
            name: string,
            diffuseAlbedo: Vector4,
            fresnelR0: Vector3,
            roughness: float32,
            ambient: Color4,
            diffuse: Color4,
            specular: Color4,
            specularPower: float32,
            emissive: Color4
        ) =
        let mutable ambient = ambient
        let mutable diffuse = diffuse
        let mutable specular = specular
        let mutable emissive = emissive
        let mutable name = name
        member this.DiffuseAlbedo = diffuseAlbedo
        member this.FresnelR0 = fresnelR0
        member this.Roughness = roughness
        member this.SpecularPower = specularPower

        new() = Material("", Vector4.Zero, Vector3.Zero, 0.0f)
        new(name: string) = Material(name, Vector4.Zero, Vector3.Zero, 0.0f)

        new(name: string, diffuseAlbedo: Vector4, fresnelR0: Vector3, roughness: float32) =
            Material(
                name,
                diffuseAlbedo,
                fresnelR0,
                roughness,
                Color4.White,
                Color4.White,
                Color4.White,
                0.0f,
                Color4.White
            )

        new( name:string, ambient: Color4, diffuse: Color4, specular: Color4, specularPower: float32, emissive: Color4) =
            Material(name, Vector4.Zero, Vector3.Zero, 0.0f, ambient, diffuse, specular, specularPower, emissive)

        override this.ToString() = "Material: " + this.Name
                
        member this.Name
            with get () = name
            and set (value) = name <- value

        member this.Ambient
            with get () = ambient
            and set (value) = ambient <- value

        member this.Diffuse
            with get () = diffuse
            and set (value) = diffuse <- value

        member this.Emissive
            with get () = emissive
            and set (value) = emissive <- value

        member this.Specular
            with get () = specular
            and set (value) = specular <- value

        member this.isEmpty = this.Name = ""


    [<AllowNullLiteral>]
    [<AbstractClass>]
    type Shape(name: string, origin: Vector3, vertices:List<Vertex>, indices:List<int>, color: Color, tessFactor: float32, raster: int, size: float32, quality:Quality) =
        let mutable (world: Matrix) = Matrix.Identity
        let mutable color = color
        let mutable name = name
        let mutable origin = origin
        let mutable size = size
        let mutable animated = false
        let mutable primitiveTopology : PrimitiveTopology = PrimitiveTopology.TriangleList
        let mutable primitiveTopologyType : PrimitiveTopologyType = PrimitiveTopologyType.Triangle
        let mutable rasterFactor = raster
        let mutable tessFactor = tessFactor
        let mutable quality = quality
        let mutable meshData = new MeshData<Vertex>()
        let mutable vertices = vertices
        let mutable indices = indices
        
        static let mutable raster = 8 
        static let mutable tesselation = 8.0f 
        static member Raster with get() = raster and set(value) = raster <- value
        static member Tesselation with get() = tesselation and set(value) = tesselation <- value

        member this.Vertices
            with get () = vertices
            and set (value) = vertices <- value

        abstract member AddVertices: List<Vertex>-> Unit
        default this.AddVertices(vertexe:List<Vertex>) =
            vertices.AddRange(vertexe)

        abstract member AddIndices: List<int>-> Unit
        default this.AddIndices(indexe: List<int>) = 
            indices.AddRange(indexe)

        member this.Points = 
            vertices |> Seq.map (fun v -> v.Position)

        member this.Indices
            with get () = indices
            and set (value) = indices <- value

        member this.World
            with get () = world
            and set (value) = world <- value

        member this.Name
            with get () = name
            and set (value) = name <- value

        member this.Animated
            with get () = animated
            and set (value) = animated <- value

        member this.Origin
            with get () = origin
            and set (value) = origin <- value

        member this.Size
            with get () = size
            and set (value) = size <- value 

        abstract member Center : Vector3 with get , set
        default this.Center 
            with get () = raise (System.Exception("Nicht implementiert"))
            and set(value) = raise (System.Exception("Nicht implementiert"))

        abstract member Update: GameTimer -> Unit
        default this.Update(gt: GameTimer) = raise (System.Exception("Nicht implementiert"))

        member this.Color
            with get () = color
            and set (value) = color <- value

        member this.RasterFactor
            with get () = rasterFactor
            and set (value) = rasterFactor <- value

        member this.TessFactor
            with get () = tessFactor
            and set (value) = tessFactor <- value

        member this.MeshData
            with get () = meshData
            and set (value) = meshData <- value

        abstract Maximum : Vector3 with get, set
        default this.Maximum 
            with get() = Vector3.UnitX
            and  set(value) = ()

        abstract Minimum : Vector3 with get, set
        default this.Minimum 
            with get() = Vector3.UnitX
            and set(value) = ()

        abstract member resize : float32 -> unit
        default this.resize(size)  = ()

        abstract member _Boundaries:Vector3 * Vector3
        default this._Boundaries = this.Minimum, this.Maximum

        // Displayable ruft diese Methode mit seiner Position auf
        // Die Grenzen sind um die Position verschoben
        abstract member Boundaries : Vector3 -> Vector3 * Vector3        
        default this.Boundaries(objectPosition) =
            objectPosition + this.Minimum, objectPosition + this.Maximum 

        abstract member TopologyType : PrimitiveTopologyType with get, set
        default this.TopologyType  
            with get() = primitiveTopologyType
            and set(value) = primitiveTopologyType <- value  

        abstract member Topology : PrimitiveTopology with get, set 
        default this.Topology
            with get() = primitiveTopology 
            and set(value) = primitiveTopology  <- value   

        abstract member BoundingBox : Vector3 -> BoundingBox
        default this.BoundingBox(objectPosition) =
            let mutable box = BoundingBox()
            box.Minimum <- fst (this.Boundaries (objectPosition))
            box.Maximum <- snd (this.Boundaries (objectPosition))
            box

        member this.Sizes =
            let mutable box = BoundingBox()
            box.Minimum <- fst this._Boundaries
            box.Maximum <- snd this._Boundaries
            Vector3(box.Width, box.Height, box.Depth)

        member this.OuterLimit(objectPosition) =
            let mutable box = this.BoundingBox(objectPosition)
            let xMax = box.Maximum.X - box.Minimum.X
            let yMax = box.Maximum.Y - box.Minimum.Y
            let zMax = box.Maximum.Z - box.Minimum.Z
            max xMax (max yMax zMax)

        abstract member CreateVertexData : Visibility -> MeshData<Vertex> 
        default this.CreateVertexData(visibility) =
            raise (new System.Exception("Not Implemented"))

    // ----------------------------------------------------------------------------------------------------
    //  type Geometriy: Basis für das GeometricModel
    // ----------------------------------------------------------------------------------------------------
    [<AbstractClass>]
    [<AllowNullLiteral>]
    type Geometry(name: string, origin: Vector3, color: Color, tessFactor: float32, raster: int, size: float32) =
        inherit Shape(name, origin, List<Vertex>(), List<int>(), color, Shape.Tesselation, Shape.Raster, size, Quality.Original)
        let mutable minimum = Vector3.Zero
        let mutable maximum = Vector3.Zero

        override this.AddVertices(vertexe:List<Vertex>) =
            raise (new System.Exception("Not appliccable"))

        override this.AddIndices(indexe:List<int>) =
            raise (new System.Exception("Not appliccable"))

        override this.Maximum
            with get () = maximum
            and set (value) = maximum <- value

        override this.Minimum
            with get () = minimum
            and set (value) = minimum <- value 

    // ----------------------------------------------------------------------------------------------------
    //  FileBased: Alle Grafik-Informationen werden aus einer Datei gelesen
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    [<AbstractClass>]
    type FileBased(name: string, origin: Vector3, vertices:List<Vertex>, indices:List<int>, size: float32, quality:Quality) =
        inherit Shape(name, origin, vertices, indices, Color.Transparent, Shape.Tesselation, Shape.Raster, size, quality)        
        let mutable minimum = Vector3.Zero
        let mutable maximum = Vector3.Zero

        override this.Maximum
            with get () = 
                maximum <- Base.MathSupport.computeMaximum (Seq.map (fun (v:Vertex) -> v.Position) vertices |>  Seq.toList)
                maximum
            and set (value) = maximum <- value

        override this.Minimum
            with get () =     
                minimum <- Base.MathSupport.computeMinimum (Seq.map (fun (v:Vertex) -> v.Position) vertices |>  Seq.toList) 
                minimum
            and set (value) = minimum <- value 

        member this.Positions () =
            vertices 
            |> Seq.map(fun v -> v.Position)
            |> Seq.toArray
            
        override this.resize(factor:float32) =
            this.Size <- factor

        override this.CreateVertexData(visibility: Visibility) =            
            this.MeshData <- new MeshData<Vertex>(this.Vertices |> Seq.toArray, this.Indices|> Seq.toArray)  
            this.MeshData

        override this.Center 
            with get () = 
                computeSchwerpunkt ( 
                    Seq.map (fun (v:Vertex) -> v.Position) vertices |> Seq.toList
                )

        override this.ToString() = "FileBased " + this.Name 
 
    [<AllowNullLiteral>]
    type Part(name: string, shape: Shape, material: Material, texture: Texture, visibility: Visibility, shaders:ShaderConfiguration) =
        let mutable name = name
        let mutable shape = shape
        let mutable material = material
        let mutable texture = texture
        let mutable visibility = visibility
        let mutable shaders = shaders

        new(name, shape, material, texture, visibility) = Part(name, shape, material, texture, visibility, ShaderConfiguration.CreateNoTesselation()) 
        new(name, shape, material, visibility, shaders) = Part(name, shape, material, new Texture(), visibility, shaders)
        new(name, shape, material, visibility) = Part(name, shape, material, new Texture(), visibility,  ShaderConfiguration.CreateNoTesselation()) 
        new(name, material, texture, visibility, shaders) = Part(name, null, material, texture, visibility, shaders)
        new(name, material, texture, visibility) = Part(name, null, material, texture, visibility,  ShaderConfiguration.CreateNoTesselation())
        new(name, shape, material, texture, shaders) = Part(name, shape,  material, texture, Visibility.Opaque, shaders)
        new(name, shape, material, texture) = Part(name, shape,  material, texture, Visibility.Opaque,  ShaderConfiguration.CreateNoTesselation())
        new(shape, material, texture, shaders) = Part("", shape,  material, texture, Visibility.Opaque, shaders)
        new(shape, material, texture) = Part("", shape,  material, texture, Visibility.Opaque,   ShaderConfiguration.CreateNoTesselation())
        new(name, shape, material, shaders) = Part(name, shape, material, new Texture(), Visibility.Opaque, shaders)
        new(name, shape, material ) = Part(name, shape, material, new Texture(), Visibility.Opaque,  ShaderConfiguration.CreateNoTesselation())
        new(name, shape, texture, shaders) = Part(name, shape, new Material(), texture, Visibility.Opaque, shaders)
        new(name, shape, shaders) = Part(name, shape, new Material(), new Texture(), Visibility.Opaque, shaders)
        new(name, shape) = Part(name, shape, new Material(), new Texture(), Visibility.Opaque,  ShaderConfiguration.CreateNoTesselation())
        new(material, shaders) = Part("", null, material, new Texture(), Visibility.Opaque, shaders)
        new(name, material, texture, shaders) = Part(name, null, material, texture, Visibility.Opaque, shaders)
        new() = Part("", null, new Material(), new Texture(), Visibility.Opaque,  ShaderConfiguration.CreateNoTesselation()) 

        member this.Copy() =
            new Part(name, shape, material, texture, visibility, shaders) 
        
        member this.Name
            with get() = name
            and set(value) = name <- value 

        member this.Shape  
            with get() = shape
            and set(value) = shape <- value
        
        member this.Shaders  
            with get() = shaders 
            and set(value) = shaders  <- value 

        member this.Texture
            with get() = texture
            and set(value) = texture <- value

        member this.TextureName() =
            if texture = null then ""
            else texture.Name
        
        member this.Material        
            with get() = material
            and set(value) = material <- value

        member this.Visibility
            with get() = visibility
            and set(value) = visibility <- value

        member this.Center  
            with get() = shape.Center

        member this.hasTexture()  = this.Texture <> null && not (this.Texture.isEmpty )
        member this.hasMaterial() = not (this.Material.isEmpty )

        member this.isEmpty() =
            this.Texture.isEmpty 
            && this.Material.isEmpty  

        member this.Transparent =
            this.Visibility = Visibility.Transparent 

        member this.resize(generalSizeFactor)=
            shape.resize(generalSizeFactor)

        member this.OfSize(generalSizeFactor)=
            shape.resize(generalSizeFactor)
            this

        override this.ToString() = shape.ToString() + " | " + material.ToString() + " | " + this.Texture.ToString()

    [<AllowNullLiteral>]
    type Display(parts:Part list, visibility: Visibility, size:float32, augmentation) =
        let mutable size = size
        let mutable parts = parts
        let mutable visibility = visibility
        let mutable augmentation=augmentation

        new() = new Display([], Visibility.Opaque, 1.0f, Augmentation.None)
        new(parts) = new Display(parts, Visibility.Opaque, 1.0f, Augmentation.None)
        new(parts, visibility) = new Display(parts, visibility, 1.0f, Augmentation.None)
        new(parts, augmentation) = new Display(parts, Visibility.Opaque, 1.0f, augmentation)

        member this.Size
            with get () = size
            and set (value) = size <- value 

        abstract member Parts:Part list with get,set
        default this.Parts
            with get() = parts
            and set(value) = parts <- value

        member this.AddPart(part:Part) =
            parts <- parts @ [part]

        member this.HasNoParts() =
            parts.IsEmpty

        member this.Augmentation 
            with get()= augmentation
            and set(value) = augmentation <- value
                    
        member this.Visibility
            with get() = visibility
            and set(value) = visibility <- value

        member this.Center =                 
            let alleZentren = this.Parts |> Seq.map (fun p -> p.Center) |> Seq.toList
            computeSchwerpunkt(alleZentren)

        member this.Minimum =
            MathSupport.computeMinimum (Seq.map (fun (p:Part) -> p.Shape.Minimum) parts |>  Seq.toList) 

        member this.Maximum =
            MathSupport.computeMaximum (Seq.map (fun (p:Part) -> p.Shape.Maximum) parts |>  Seq.toList) 

        member this.Position =
            MathSupport.computeMinimum (Seq.map (fun (p:Part) -> p.Shape.Origin) parts |>  Seq.toList) 

        member this.BoundingBox(position:Vector3) =
            let mutable box = BoundingBox()
            box.Minimum <- position + this.Minimum 
            box.Maximum <- position + this.Maximum 
            box

        member this.isTransparent =
            match visibility with
            | Transparent -> true
            | _ -> not (
                    parts
                    |> Seq.exists (fun p -> p.Visibility = Visibility.Opaque)
                )

        member this.isOpaque =
            not this.isTransparent

        member this.resize(generalSizeFactor) =
            for part in parts do
                part.resize(generalSizeFactor)

        member this.HiliteBox() =
            this.BoundingBox(this.Position)

        override this.ToString() = " " + this.Parts.Length.ToString() + " Part(s)" 

    let mutable DEFAULT_RASTER = Shape.Raster
    let DEFAULT_TESSELATION = Shape.Tesselation