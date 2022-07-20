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
        | ShowCenter
        | None

    type Visibility =
        | Opaque
        | Transparent
        | Invisible
        | NotSet

    let  blendTypeFromVisibility(visibility:Visibility) =
        match visibility with 
        | Visibility.Opaque       -> BlendType.Opaque
        | Visibility.Transparent  -> BlendType.Transparent
        | Visibility.Invisible    -> BlendType.Transparent
        | Visibility.NotSet       -> BlendType.Opaque

    let blendStateFromVisibility(visibility:Visibility) =
        match visibility with 
        | Visibility.Opaque       -> blendStateOpaque
        | Visibility.Transparent  -> blendStateTransparent
        | Visibility.Invisible    -> blendStateTransparent
        | Visibility.NotSet       -> blendStateOpaque

    let blendDescriptionFromVisibility(visibility:Visibility) =
        match visibility with 
        | Visibility.NotSet       -> BlendDescription(BlendType.Opaque, blendStateOpaque)
        | Visibility.Opaque       -> BlendDescription(BlendType.Opaque, blendStateOpaque)
        | Visibility.Transparent  -> BlendDescription(BlendType.Transparent, blendStateTransparent)
        | Visibility.Invisible    -> BlendDescription(BlendType.Transparent, blendStateTransparent)

    let rasterDescriptionFromVisibility(visibility:Visibility) =
        match visibility with 
        | Visibility.NotSet         -> RasterizerDescription(RasterType.Solid, rasterizerStateSolid)
        | Visibility.Opaque         -> RasterizerDescription(RasterType.Solid, rasterizerStateSolid)
        | Visibility.Transparent    -> RasterizerDescription(RasterType.Transparent, rasterizerStateSolid)
        | Visibility.Invisible      -> RasterizerDescription(RasterType.Transparent, rasterizerStateTransparent)

    let TransparenceFromVisibility(visibility:Visibility) =
        match visibility with
        | Visibility.NotSet         -> false 
        | Visibility.Opaque         -> false
        | Visibility.Transparent    -> true
        | Visibility.Invisible      -> true

    type TesselationMode = 
        TRI | QUAD | BEZIER | NONE
    
    let mutable textureIndex = new Dictionary<string, int>()
    [<AllowNullLiteral>]
    type Texture(name: string, fileName:string, pathName:string, _data:byte[], _mimeType, isCube:bool, samplerDesc:SamplerStateDescription) =
        let mutable name=name
        let mutable mimeType=_mimeType
        let mutable idx=0
        let mutable fileName=fileName
        let mutable path = pathName 
        let mutable isCube = isCube 
        let mutable data:byte[] = _data
        let mutable sampler:SamplerStateDescription = samplerDesc

        static let mutable text_count = 0 

        do  
            if not (textureIndex.ContainsKey(name)) then 
                textureIndex.Add(name, text_count)
                idx <- text_count
                text_count <- text_count + 1
            else 
                idx <- textureIndex.Item(name)

        new(name, fileName, pathName, data, mimeType, samplerDesc) = Texture(name, fileName, pathName, data, mimeType, false, samplerDesc)
        new(name, fileName, pathName, data, mimeType, isCube) = Texture(name, fileName, pathName, data, mimeType, isCube, SamplerStateDescription())
        new(name, fileName, pathName, data, mimeType) = Texture(name, fileName, pathName, data, mimeType, false)
        new(name, fileName, pathName, data, isCube) = Texture(name, fileName, pathName, data, "", isCube, SamplerStateDescription())
        new(name, fileName, pathName, isCube) = Texture(name, fileName, pathName, [||], "", isCube, SamplerStateDescription()) 
        new(name, fileName, pathName) = Texture(name, fileName, pathName, false)
        new(name, mimeType, data) = Texture(name, "", "",  data, mimeType, false)   
        new(name, data) = Texture(name, "", "",  data, "", false)   
        new(name) = Texture(name )  
        new() = Texture("", "", "", [||], "", false)  

        member this.Name = name        
        member this.Idx = idx
        member this.MimeType = mimeType
        member this.FileName = fileName
        member this.Path =
            if path <> "" then path
            else this.FileName

        member this.IsCube = isCube = true 

        member this.Data
            with get() = data
            and set(value) = data <- value 

        member this.Sampler
            with get() = sampler 
            and set(value) = sampler <- value

        member this.isEmpty = this.Name = "" 

        member this.notEmpty = this.Name <> ""

        override this.ToString() = "Texture " + (if this.isEmpty then " Empty " else this.Name)

    
    [<AllowNullLiteral>]
    type TextureBaseColour(name: string, fileName:string, pathName:string, _data:byte[], _mimeType:string, samplerDesc:SamplerStateDescription) =
        inherit Texture(name, fileName, pathName, _data, _mimeType, samplerDesc) 

    [<AllowNullLiteral>]
    type TextureMetallicRoughness(name: string, fileName:string, pathName:string, _data:byte[], _mimeType, sampler:SamplerStateDescription) =
        inherit Texture(name, fileName, pathName, _data, _mimeType, sampler) 
    
    [<AllowNullLiteral>]
    type TextureEmission(name: string, fileName:string, pathName:string, _data:byte[], _mimeType, sampler:SamplerStateDescription) =
        inherit Texture(name, fileName, pathName, _data, _mimeType, sampler) 
    
    [<AllowNullLiteral>]
    type TextureNormal(name: string, fileName:string, pathName:string, _data:byte[], _mimeType, sampler:SamplerStateDescription) =
        inherit Texture(name, fileName, pathName, _data, _mimeType, sampler) 
    
    [<AllowNullLiteral>]
    type TextureOcclusion(name: string, fileName:string, pathName:string, _data:byte[], _mimeType, sampler:SamplerStateDescription) =
        inherit Texture(name, fileName, pathName, _data, _mimeType, sampler)
    
    // ----------------------------------------------------------------------------------------------------
    //  Material - Parameter zur Beschreibung der Lichtsituation
    // ----------------------------------------------------------------------------------------------------
    let mutable materialIndex = new Dictionary<string, int>()
    [<AllowNullLiteral>]
    type Material
        (
            name: string,
            diffuseAlbedo: Vector4,
            fresnelR0: Vector3,
            roughness: float32,
            matTransform: Matrix,
            ambient: Color4,
            diffuse: Color4,
            specular: Color4,
            specularPower: float32,
            emissive: Color4,
            hasTexture:bool 
        ) =
        
        let mutable name = name
        let mutable idx = 0

        // Shader: cookbook
        let mutable ambient = ambient
        let mutable diffuse = diffuse
        let mutable specular = specular
        let mutable specularPower = specularPower
        let mutable emissive = emissive
        let mutable hasTexture = hasTexture

        // Shader: Diffuse lighting
        let mutable diffuseAlbedo = diffuseAlbedo
        let mutable fresnelR0 = fresnelR0
        let mutable roughness  = roughness
        let mutable matTransform = matTransform

        static let mutable mat_count = 0 

        do  
            if not (materialIndex.ContainsKey(name)) then 
                materialIndex.Add(name, mat_count)
                idx <- mat_count
                mat_count <- mat_count + 1
            else 
                idx <- materialIndex.Item(name)

        static member MAT_COUNT
            with get() = mat_count

        member this.IDX = idx

        // Shader: Diffuse lighting
        new(name: string, diffuseAlbedo: Vector4, fresnelR0: Vector3, roughness: float32, matTransform) =
            Material(
                name,
                diffuseAlbedo,
                fresnelR0,
                roughness,
                matTransform,
                Color4.White,
                Color4.White,
                Color4.White,
                0.0f,
                Color4.White,
                false
            )

        new() = Material("", Vector4.Zero, Vector3.Zero, 0.0f, Matrix.Identity)
        new(name: string) = Material(name, Vector4.Zero, Vector3.Zero, 0.0f, Matrix.Identity)

        // Shader: Cookbook
        new( name:string, ambient: Color4, diffuse: Color4, specular: Color4, specularPower: float32, emissive: Color4, hasTexture) =
            Material(
                name,
                Vector4.Zero,
                Vector3.Zero, 
                0.0f, 
                Matrix.Identity,
                ambient,
                diffuse,
                specular,
                specularPower,
                emissive,
                hasTexture
                )

        // Shader: Cookbook
        new( name:string, ambient: Color4, diffuse: Color4, specular: Color4, specularPower: float32, emissive: Color4) =
            Material(
                name,
                Vector4.Zero,
                Vector3.Zero, 
                0.0f, 
                Matrix.Identity,
                ambient,
                diffuse,
                specular,
                specularPower,
                emissive,
                false
                )

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

        member this.HasTexture
            with get () = hasTexture
            and set (value) = hasTexture <- value

        member this.Specular
            with get () = specular
            and set (value) = specular <- value

        member this.SpecularPower
            with get () = specularPower
            and set (value) = specularPower <- value

        member this.DiffuseAlbedo
            with get () = diffuseAlbedo
            and set (value) = diffuseAlbedo <- value

        member this.FresnelR0
            with get () = fresnelR0
            and set (value) = fresnelR0 <- value

        member this.Roughness
            with get () = roughness
            and set (value) = roughness <- value

        member this.MatTransform
            with get () = matTransform
            and set (value) = matTransform <- value

        member this.isEmpty = this.Name = ""

    type MaterialPBR() =
        inherit Material("", Vector4.Zero, Vector3.Zero, 0.0f, Matrix.Identity)
        let mutable textureBaseColour: TextureBaseColour = null
        let mutable textureMetallicRoughness: TextureMetallicRoughness = null 
        let mutable textureEmission: TextureEmission = null 
        let mutable textureNormal: TextureNormal = null 
        let mutable textureOcclusion : TextureOcclusion = null 

        member this.BaseColourTexture
            with get() = textureBaseColour 
            and set(value) = textureBaseColour <- value

        member this.MetallicRoughnessTexture
            with get() = textureMetallicRoughness 
            and set(value) = textureMetallicRoughness <- value

        member this.EmissionTexture
            with get() = textureEmission
            and set(value) = textureEmission <- value
        
        member this.NormalTexture
            with get() = textureNormal
            and set(value) = textureNormal <- value 
        
        member this.OcclusionTexture
            with get() = textureOcclusion 
            and set(value) = textureOcclusion <- value  


    [<AllowNullLiteral>]
    [<AbstractClass>]
    type Shape(name: string, origin: Vector3, vertices:List<Vertex>, indices:List<int>, color: Color, tessFactor: float32, raster: int, size: Vector3, quality:Quality) =
        let mutable transform = Matrix.Identity
        let mutable color = color
        let mutable name = name
        let mutable origin = origin
        let mutable size = size
        let mutable animated = false
        let mutable primitiveTopology : PrimitiveTopology = PrimitiveTopology.TriangleList
        let mutable primitiveTopologyType : PrimitiveTopologyType = PrimitiveTopologyType.Triangle
        let mutable rasterFactor = raster
        let mutable tessFactor = tessFactor
        
        static let mutable raster = 8 
        static let mutable tesselation = 8.0f 
        static member Raster with get() = raster and set(value) = raster <- value
        static member Tesselation with get() = tesselation and set(value) = tesselation <- value

        abstract member Vertices: List<Vertex> with get, set

        abstract member Indices: List<int> with get, set

        abstract member AddVertices: List<Vertex>-> Unit
        default this.AddVertices(vertexe:List<Vertex>) =
            vertices.AddRange(vertexe)

        abstract member AddIndices: List<int>-> Unit
        default this.AddIndices(indexe: List<int>) = 
            indices.AddRange(indexe)

        abstract member CreateVertexData : Visibility -> MeshData<Vertex> 

        abstract member Points:seq<Vector3>
        default this.Points = 
            vertices |> Seq.map (fun v -> v.Position)

        member this.Name
            with get () = name
            and set (value) = name <- value

        member this.Animated
            with get () = animated
            and set (value) = animated <- value
        
        member this.Transform  
            with get() = transform
            and set(value) = transform <- value

        abstract member Origin : Vector3 with get , set
        default this.Origin
            with get () = origin
            and set (value) = origin <- value

        member this.Size
            with get () = size
            and set (value) = size <- value 

        abstract member Center : Vector3 with get , set
        default this.Center 
            with get () = raise (System.Exception("Nicht implementiert"))
            and set(value) = raise (System.Exception("Nicht implementiert"))

        abstract member ToOrigin : Matrix with get , set
        default this.ToOrigin 
            with get () = raise (System.Exception("Nicht implementiert"))
            and set(value) = raise (System.Exception("Nicht implementiert"))

        member this.CenterOrigin =
            this.Origin - this.Center  

        member this.OriginCenter =
            -this.CenterOrigin

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

        abstract Maximum : Vector3 with get, set
        default this.Maximum 
            with get() = Vector3.UnitX
            and  set(value) = ()

        abstract Minimum : Vector3 with get, set
        default this.Minimum 
            with get() = Vector3.UnitX
            and set(value) = ()

        abstract member resize : Vector3 -> unit
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

    // ----------------------------------------------------------------------------------------------------
    //  type Geometriy: Basis für das GeometricModel
    // ----------------------------------------------------------------------------------------------------
    [<AbstractClass>]
    [<AllowNullLiteral>]
    type GeometryBased(name: string, origin: Vector3, color: Color, tessFactor: float32, raster: int, size: Vector3) =
        inherit Shape(name, origin, List<Vertex>(), List<int>(), color, Shape.Tesselation, Shape.Raster, size, Quality.Original)
        let mutable minimum = Vector3.Zero
        let mutable maximum = Vector3.Zero

        override this.AddVertices(vertexe:List<Vertex>) =
            raise (new System.Exception("Not appliccable"))

        override this.AddIndices(indexe:List<int>) =
            raise (new System.Exception("Not appliccable"))

        override this.Vertices
            with get() = raise (new System.Exception("Not appliccable"))
            and set(value) = raise (new System.Exception("Not appliccable"))

        override this.Indices
            with get() = raise (new System.Exception("Not appliccable"))
            and set(value) = raise (new System.Exception("Not appliccable"))
            
        override this.Maximum
            with get () = maximum
            and set (value) = maximum <- value

        override this.Minimum
            with get () = minimum
            and set (value) = minimum <- value
            
        override this.ToOrigin 
            with get () = Matrix.Translation(-this.Center)

    // ----------------------------------------------------------------------------------------------------
    //  FileBased: Alle Grafik-Informationen werden aus einer Datei gelesen
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    [<AbstractClass>]
    type FileBased(name: string, origin: Vector3, vertices:List<Vertex>, indices:List<int>, size: Vector3, quality:Quality) =
        inherit Shape(name, origin, vertices, indices, Color.Transparent, Shape.Tesselation, Shape.Raster, size, quality)        
        let mutable minimum = Vector3.Zero
        let mutable maximum = Vector3.Zero
        let mutable meshData = new MeshData<Vertex>()
        let mutable vertices = vertices
        let mutable indices = indices

        override this.Vertices
            with get () = vertices
            and set (value) = vertices <- value

        override this.AddVertices(vertexe:List<Vertex>) =
            vertices.AddRange(vertexe)
            
        override this.Indices
            with get () = indices
            and set (value) = indices <- value

        override this.AddIndices(indexe: List<int>) = 
            indices.AddRange(indexe)

        member this.MeshData
            with get () = meshData
            and set (value) = meshData <- value

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
            
        override this.resize(factor:Vector3) =
            this.Size <- factor

        override this.CreateVertexData(visibility: Visibility) =            
            this.MeshData <- new MeshData<Vertex>(this.Vertices |> Seq.toArray, this.Indices|> Seq.toArray)  
            this.MeshData

        override this.Center 
            with get () = 
                computeSchwerpunkt ( 
                    Seq.map (fun (v:Vertex) -> v.Position) vertices |> Seq.toList
                )

        override this.ToOrigin 
            with get () = Matrix.Identity

        override this.ToString() = "FileBased " + this.Name 
 
    [<AllowNullLiteral>]
    type Part(name: string, shape: Shape, material: Material, texture: Texture, visibility: Visibility, shaders:ShaderConfiguration) =
        let mutable name        = name
        let mutable idx         = 0
        let mutable shape       = shape
        let mutable material    = material
        let mutable texture     = texture
        let mutable visibility  = visibility
        let mutable shaders     = shaders
        let mutable transform   = Matrix.Identity

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
        new(name, shape, visibility) = Part(name, shape, new Material(), new Texture(), visibility,  ShaderConfiguration.CreateNoTesselation())
        new(name, shape) = Part(name, shape, new Material(), new Texture(), Visibility.Opaque,  ShaderConfiguration.CreateNoTesselation())
        new(material, shaders) = Part("", null, material, new Texture(), Visibility.Opaque, shaders)
        new(name, material, texture, shaders) = Part(name, null, material, texture, Visibility.Opaque, shaders)
        new() = Part("", null, new Material(), new Texture(), Visibility.Opaque,  ShaderConfiguration.CreateNoTesselation()) 

        member this.Copy() =
            new Part(name, shape, material, texture, visibility, shaders) 

        member this.Idx
            with get() = idx
            and set(value) = idx <- value 
        
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

        member this.TextureIsCube() =
            if texture = null then false
            else texture.IsCube
        
        member this.Material        
            with get() = material
            and set(value) = material <- value

        member this.Visibility
            with get() = visibility
            and set(value) = visibility <- value

        member this.Center  
            with get() = shape.Center

        member this.Transform  
            with get() = transform
            and set(value) = transform <- value

        member this.hasTexture()  = this.Texture <> null && not (this.Texture.isEmpty )
        member this.hasMaterial() = not (this.Material.isEmpty )

        member this.isEmpty() =
            this.Texture.isEmpty 
            && this.Material.isEmpty  

        member this.Transparent =
            this.Visibility = Visibility.Transparent 

        member this.Invisible =
            this.Visibility = Visibility.Invisible 

        member this.resize(generalSizeFactor)=
            shape.resize(generalSizeFactor)

        member this.OfSize(generalSizeFactor)=
            shape.resize(generalSizeFactor)
            this

        override this.ToString() = shape.ToString() + " | " + material.ToString() + " | " + this.Texture.ToString()

    [<AllowNullLiteral>]
    type Display(parts:Part list, visibility: Visibility, size:Vector3, augmentation:Augmentation) =
        let mutable size = size
        let mutable parts = parts
        let mutable visibility = visibility
        let mutable augmentation=augmentation
        do  
            if visibility <> Visibility.NotSet then
                parts |> List.iter(fun p -> p.Visibility <- visibility)

        new() = new Display([], Visibility.NotSet, Vector3.One, Augmentation.None)
        new(visibility, augmentation) = new Display([], visibility, Vector3.One, augmentation )
        new(visibility, size, augmentation) = new Display([], visibility, size, augmentation )
        new(parts) = new Display(parts, Visibility.NotSet, Vector3.One, Augmentation.None)
        new(parts, visibility) = new Display(parts, visibility, Vector3.One, Augmentation.None)
        new(parts, visibility, size) = new Display(parts, visibility, size, Augmentation.None)
        new(parts, visibility, augmentation) = new Display(parts, visibility, Vector3.One, augmentation)
        new(parts, augmentation) = new Display(parts, Visibility.NotSet, Vector3.One, augmentation)

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
            and set(value) = 
                visibility <- value
                this.Parts |> List.iter(fun p -> p.Visibility <- visibility)

        member this.Center =   
            if this.Parts.Length = 1 then
                this.Parts.Head.Center 
            else
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