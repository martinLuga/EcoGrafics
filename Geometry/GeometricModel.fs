namespace Geometry
//
//  GeometricModel.fs
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

// ----------------------------------------------------------------------------------------------------
// Geometrische Objekte
// Kugel
// Quader
// ----------------------------------------------------------------------------------------------------
module GeometricModel = 

    let logger = LogManager.GetLogger("Geometric.GeometricModel")
    let logDebug = Debug(logger)
    let logInfo  = Info(logger)
    let logWarn  = Warn(logger)
    let logError = Error(logger)

    exception GeometryException of string

    let approximatelyEqual (a : float32, b: float32) =
        abs(b - a) < 0.9f

    let mutable instanceCount = 1

    let mutable DEFAULT_RASTER = 8
    let DEFAULT_TESSELATION = 1.0f

    let setCylinderRaster (newRaster:Raster) = 
        DEFAULT_RASTER  <- (int newRaster)
        tessellation <- (int newRaster)

    let nextInstanceCountString =
        let next = instanceCount + 1
        instanceCount <- next
        next.ToString()

    let QuaderDefaultName = "Quader" + nextInstanceCountString

    let COUNTERCLOCKWISE = false
    let CLOCKWISE = true

    type PlanePosition = 
        FRONT | BACK | LEFT| RIGHT | BOTTOM | TOP | NONE

    type TesselationMode = 
        TRI | QUAD | BEZIER | NONE

    [<AbstractClass>]
    type Geometric(name: string, ursprung:Vector3, color:Color, topology, tessFactor, raster) =
        let mutable (world:Matrix) = Matrix.Identity
        let mutable topology=topology       
        let mutable color = color
        let mutable name = name
        let mutable ursprung = ursprung
        let mutable minimum = Vector3.Zero 
        let mutable maximum = Vector3.Zero 
        let mutable tessFactor = tessFactor
        let mutable raster = raster
        member this.World
            with get () = world
            and set (value) = world <- value
        member this.Topology
            with get () = topology
            and set (value) = topology <- value 
        member this.Name  
            with get () = name
            and set (value) = name <- value
        member this.Ursprung  
            with get () = ursprung
            and set (value) = ursprung <- value
        member this.Color  
            with get () = color
            and set (value) = color <- value
        member this.Maximum 
            with get () = maximum
            and set (value) = maximum <- value
        member this.Minimum 
            with get () = minimum
            and set (value) = minimum <- value
        member this.TessFactor
            with get () = tessFactor
            and  set (value) = tessFactor <- value
        member this.Raster
            with get () = raster
            and  set (value) = raster <- value

        abstract member Center: Vector3 

        abstract member resize:float32 -> unit 

        abstract member Boundaries: Vector3 -> Vector3 * Vector3

        abstract member getVertexData: bool -> MeshData

        abstract member getNormalAt: Vector3 * Vector3 -> Vector3
        default this.getNormalAt(hitPoint: Vector3, position: Vector3) = Vector3.UnitX
        
        abstract member getClosestAt: Vector3-> Vector3
        default this.getClosestAt(position: Vector3) = Vector3.Zero

        abstract member hitPoint: Vector3 * Vector3 -> Vector3
        default this.hitPoint(myPosition, anotherPosition) =
            let mutable bb = this.BoundingBox(myPosition)
            let mutable p = anotherPosition
            Collision.ClosestPointBoxPoint(&bb,&p)

        abstract member tesselationMode: unit-> TesselationMode
        default this.tesselationMode( ) = TesselationMode.NONE

        member this.canTesselate( ) = this.tesselationMode( )<>TesselationMode.NONE

        member this.BoundingBox(objectPosition) = 
            let mutable box = BoundingBox()
            box.Minimum <- fst (this.Boundaries(objectPosition))
            box.Maximum <- snd (this.Boundaries(objectPosition))
            box

        member this.CenterAtPosition(objectPosition) = 
            this.Center + objectPosition


    // ----------------------------------------------------------------------------------------------------
    // Kugel
    // ----------------------------------------------------------------------------------------------------
    type Kugel(name: string, radius :float32, color:Color) =
        inherit Geometric(name, Vector3.Zero, color, PrimitiveTopology.TriangleList, DEFAULT_TESSELATION, DEFAULT_RASTER)
        let mutable radius = radius

        member this.Radius  
            with get () = radius
            and set (value) = radius <- value        
        
        new() = Kugel("Sphere", 1.0f, Color.Gray)
        new(radius, farbe) = Kugel("Sphere", radius, farbe)

        override this.Center = Vector3(base.Ursprung.X + this.Radius, base.Ursprung.Y + this.Radius, base.Ursprung.Z + this.Radius) 

        override this.resize  newSize  = 
            this.Radius <-this.Radius * newSize 

        override this.ToString()  = "Kugel:" + this.Name + " r= " + this.Radius.ToString()

        override this.Boundaries(objectPosition) = 
            this.Minimum <- objectPosition
            this.Maximum <- Vector3(objectPosition.X + this.Radius * 2.0f, objectPosition.Y + this.Radius * 2.0f , objectPosition.Z + this.Radius* 2.0f) 
            (this.Minimum, this.Maximum)

        //override this.hitPoint(myPosition, anotherPosition) =
        //    let mutable bs = new BoundingSphere(myPosition, this.Radius)
        //    let mutable p = anotherPosition
        //    let point = Collision.ClosestPointSpherePoint(&bs,&p)
        //    point
            
        override this.getVertexData(isTransparent) =
            let vertices = sphereVertices this.Color this.Radius isTransparent 
            let indices = sphereIndices CLOCKWISE
            new MeshData(vertices, indices, this.Topology)   

        // Kugel : Die Normale ist Die Senkrechte auf dem hitPoint
        // TODO Implementierung OK ?
        override this.getNormalAt(hitPosition, position) = 
            let result = (hitPosition - position)
            result.Normalize()
            result

    let makeCenter(ursprung:Vector3) seitenLaenge =
        Vector3(ursprung.X + seitenLaenge / 2.0f, ursprung.Y+ seitenLaenge / 2.0f, ursprung.Z+ seitenLaenge / 2.0f)

    // ----------------------------------------------------------------------------------------------------
    // Würfel
    // ----------------------------------------------------------------------------------------------------
    type Würfel(name:string, ursprung:Vector3, seitenlaenge:float32, colorFront:Color, colorRight:Color, colorBack:Color, colorLeft:Color, colorTop:Color, colorBottom:Color) =
        inherit Geometric(name, ursprung, Color.White, PrimitiveTopology.TriangleList, DEFAULT_TESSELATION, DEFAULT_RASTER)
        let mutable seitenlaenge=seitenlaenge

        member this.SeitenLaenge
            with get () = seitenlaenge
            and set (value) = seitenlaenge <- value      
        
        member this.ColorFront=colorFront                                  
        member this.ColorRight=colorRight                                  
        member this.ColorBack=colorBack                                  
        member this.ColorLeft=colorLeft                                  
        member this.ColorTop=colorTop                                  
        member this.ColorBottom=colorBottom

        new(name, seitenlaenge, colorFront, colorRight, colorBack, colorLeft, colorTop, colorBottom) = 
            Würfel(name, Vector3.Zero, seitenlaenge, colorFront, colorRight, colorBack, colorLeft, colorTop, colorBottom)

        new (name, seitenLaenge, color) = Würfel(name, seitenLaenge, color, color, color, color, color, color)

        new() = Würfel("Cube",  0.0f, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black)

        override this.Center = makeCenter base.Ursprung this.SeitenLaenge
  
        override this.resize newSize  = 
            this.SeitenLaenge <- this.SeitenLaenge * newSize 

        override this.ToString() = "Quader l= " + this.SeitenLaenge.ToString()
        
        override this.Boundaries(objectPosition) = 
            this.Minimum <-  objectPosition  
            this.Maximum <- Vector3(objectPosition.X + this.SeitenLaenge, objectPosition.Y + this.SeitenLaenge, objectPosition.Z + this.SeitenLaenge) 
            (this.Minimum, this.Maximum)
        
        override this.getVertexData(isTransparent) =
            cubeVertices this.Ursprung this.SeitenLaenge this.SeitenLaenge this.SeitenLaenge this.ColorFront this.ColorRight this.ColorBack this.ColorLeft this.ColorTop this.ColorBottom this.Topology isTransparent

        member this.planePositionAt(position: Vector3) =
 
            if approximatelyEqual(position.Z, this.Minimum.Z) then 
                PlanePosition.FRONT
            else if approximatelyEqual(position.Z, this.Maximum.Z) then 
                PlanePosition.BACK
            else if approximatelyEqual(position.X, this.Minimum.X) then 
                PlanePosition.LEFT  
            else if approximatelyEqual(position.X, this.Maximum.X) then 
                PlanePosition.RIGHT  
            else if approximatelyEqual(position.Y, this.Minimum.Y) then 
                PlanePosition.TOP  
            else if approximatelyEqual(position.Y, this.Maximum.Y) then 
                PlanePosition.BOTTOM  
            else    
                raise (GeometryException("Plane-Position nicht ermittelt"))          // Error

        // QUADER : Die Normale je nach Seite
        override this.getNormalAt(hitPosition, position: Vector3) = 
            let planePosition = this.planePositionAt(hitPosition)
            //logger.Debug(this.Name + " PLANEPOS  " + planePosition.ToString())
            match planePosition with
            | PlanePosition.FRONT  -> 
                Vector3.UnitZ * -1.0f
            | PlanePosition.BACK   -> 
                Vector3.UnitZ
            | PlanePosition.LEFT   -> 
                Vector3.UnitX * -1.0f
            | PlanePosition.RIGHT  -> 
                Vector3.UnitX
            | PlanePosition.BOTTOM -> 
                Vector3.UnitY * -1.0f
            | PlanePosition.TOP    -> 
                Vector3.UnitY
            | _ -> raise (GeometryException("Plane-Position für Normale nicht ermittelt"))           

    // ----------------------------------------------------------------------------------------------------
    // Quader
    // ----------------------------------------------------------------------------------------------------
    type Quader(name: string, laenge:float32, hoehe:float32, breite:float32, colorFront:Color, colorRight:Color, colorBack:Color, colorLeft:Color, colorTop:Color, colorBottom:Color) =
        inherit Geometric(name, Vector3.Zero, colorFront, PrimitiveTopology.TriangleList, DEFAULT_TESSELATION, DEFAULT_RASTER)
        let mutable laenge=laenge
        let mutable hoehe=hoehe  
        let mutable breite=breite  

        member this.Laenge 
            with get () = laenge
            and set (value) = laenge <- value    

        member this.Hoehe
            with get () = hoehe
            and set (value) = hoehe <- value 
        
        member this.Breite
            with get () = breite
            and set (value) = breite <- value         
        
        member this.ColorFront=colorFront                                  
        member this.ColorRight=colorRight                                  
        member this.ColorBack=colorBack                                  
        member this.ColorLeft=colorLeft                                  
        member this.ColorTop=colorTop                                  
        member this.ColorBottom=colorBottom

        new() = Quader(QuaderDefaultName, 1.0f, 2.0f, 3.0f, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black)
        new (name, laenge, hoehe, breite, color) = Quader(name, laenge, hoehe, breite, color, color, color, color, color, color)
        new (laenge, hoehe, breite, color) = Quader(QuaderDefaultName, laenge, hoehe, breite, color, color, color, color, color, color)
        new (laenge, hoehe, breite, farbe, colorRight, colorBack, colorLeft, colorTop, colorBottom) = Quader(QuaderDefaultName, laenge, hoehe, breite, farbe, colorRight, colorBack, colorLeft, colorTop, colorBottom)
        new (min:Vector3, max:Vector3, color) = Quader ((max.X-min.X), (max.Y-min.Y), (max.Z-min.Z), color) 
         
        override this.ToString() = "Quader L: "  + this.Laenge.ToString() + " B: " + this.Breite.ToString() + " H: " + this.Hoehe.ToString()
        
        override this.Center = Vector3(base.Ursprung.X + this.Laenge / 2.0f, base.Ursprung.Y+ this.Hoehe / 2.0f, base.Ursprung.Z + this.Breite / 2.0f)

        override this.resize newSize  = 
            this.Laenge <- this.Laenge * newSize 
            this.Breite <- this.Breite * newSize
            this.Hoehe <- this.Hoehe * newSize
        
        // Displayable ruft diese Methode mit seiner Position
        // d.h. der Ursprung ist um diesen Vektor verschoben
        override this.Boundaries(objectPosition) = 
            this.Minimum <- objectPosition  
            this.Maximum <- Vector3(objectPosition.X + laenge, objectPosition.Y + hoehe, objectPosition.Z + breite) 
            (this.Minimum, this.Maximum)

        override this.getVertexData(isTransparent) =
            cubeVertices this.Ursprung this.Laenge this.Hoehe this.Breite this.ColorFront  this.ColorRight this.ColorBack  this.ColorLeft this.ColorTop  this.ColorBottom  this.Topology isTransparent
            //cubeVerticesUni this.Ursprung this.Laenge this.Hoehe this.Breite this.Color this.Topology
            
        // Die Seite ermitteln
        member this.planePositionAt(position: Vector3) =
 
            if approximatelyEqual(position.Z, this.Minimum.Z) then 
                PlanePosition.FRONT
            else if approximatelyEqual(position.Z, this.Maximum.Z) then 
                PlanePosition.BACK
            else if approximatelyEqual(position.X, this.Minimum.X) then 
                PlanePosition.LEFT  
            else if approximatelyEqual(position.X, this.Maximum.X) then 
                PlanePosition.RIGHT  
            else if approximatelyEqual(position.Y, this.Minimum.Y) then 
                PlanePosition.TOP  
            else if approximatelyEqual(position.Y, this.Maximum.Y) then 
                PlanePosition.BOTTOM  
            else 
                logError("Seite nicht ermittelt für " + this.ToString())
                logError("Getroffen in Punkt: " + position.ToString())
                raise (GeometryException("Plane-Position für Normale nicht ermittelt")) 

        // Quader : Die Normale je nach Seite
        override this.getNormalAt(hitPosition, position: Vector3) = 
            let planePosition = this.planePositionAt(hitPosition)
            logger.Debug(this.Name + " PLANEPOS  " + planePosition.ToString())
            match planePosition with
            | PlanePosition.FRONT  -> 
                Vector3.UnitZ * -1.0f
            | PlanePosition.BACK   -> 
                Vector3.UnitZ
            | PlanePosition.LEFT   -> 
                Vector3.UnitX * -1.0f
            | PlanePosition.RIGHT  -> 
                Vector3.UnitX
            | PlanePosition.BOTTOM -> 
                Vector3.UnitY * -1.0f
            | PlanePosition.TOP    -> 
                Vector3.UnitY
            | _ -> raise (GeometryException("Plane-Position für Normale nicht ermittelt")) 

    // ----------------------------------------------------------------------------------------------------
    // Cylinder
    // ----------------------------------------------------------------------------------------------------
    type Cylinder(name: string, radius :float32, hoehe :float32, color:Color, color2:Color, withCap:bool) =
        inherit Geometric(name, Vector3.Zero, color, PrimitiveTopology.TriangleList, DEFAULT_TESSELATION, DEFAULT_RASTER)
        let mutable radius =radius   
        let mutable hoehe =hoehe 
        let mutable colorCone = color 
        let mutable colorCap = color2
        let mutable withCap = withCap

        member this.Radius 
            with get () = radius
            and set (value) = radius <- value  
            
        member this.Hoehe 
            with get () = hoehe
            and set (value) = hoehe <- value  
        
        member this.Farbe1=color                                           
        member this.Farbe2=color2

        new() = Cylinder("Cylinder", 0.0f, 0.0f, Color.Transparent, Color.Transparent, true)
        new(radius , hoehe, farbe) = Cylinder("Cylinder", radius , hoehe, farbe, farbe, true)
        new(radius , hoehe, farbe1, farbe2) = Cylinder("Cylinder", radius , hoehe, farbe1, farbe2, true)
        new(name, radius , hoehe, farbe1, farbe2) = Cylinder(name, radius , hoehe, farbe1, farbe2, true)

        override this.ToString() = "Cylinder r= " + this.Radius.ToString() + " h= " + this.Hoehe.ToString()

        override this.Center = Vector3(base.Ursprung.X , this.Ursprung.Y + this.Hoehe / 2.0f, this.Ursprung.Z)

        override this.resize newSize  = 
            this.Radius <- this.Radius * newSize 
            this.Hoehe <- this.Hoehe * newSize 
        
        override this.Boundaries(objectPosition) = 
            this.Minimum <- Vector3(objectPosition.X - radius, objectPosition.Y , objectPosition.Z - radius)  
            this.Maximum <- Vector3(objectPosition.X + radius, objectPosition.Y + hoehe, objectPosition.Z + radius)  
            (this.Minimum, this.Maximum)
        
        override this.getVertexData(isTransparent) =
            cylinderVertices colorCone colorCap this.Hoehe this.Radius this.Topology this.Raster withCap isTransparent 

    // ----------------------------------------------------------------------------------------------------
    // Pyramid
    // ----------------------------------------------------------------------------------------------------
    type Pyramide(name: string, seitenLaenge :float32, hoehe :float32, colorFront:Color, colorRight:Color, colorBack:Color, colorLeft:Color, colorBasis:Color) =
        inherit Geometric(name, Vector3.Zero, Color.White, PrimitiveTopology.TriangleList, DEFAULT_TESSELATION, DEFAULT_RASTER)

        let mutable seitenLaenge=seitenLaenge  
        let mutable hoehe =hoehe 
        
        member this.SeitenLaenge
            with get () = seitenLaenge
            and set (value) = seitenLaenge <- value  
        
        member this.Hoehe 
            with get () = hoehe
            and set (value) = hoehe <- value 
        
        member this.ColorBasis=colorBasis                                        
        member this.ColorFront=colorFront                                      
        member this.ColorRight=colorRight                                       
        member this.ColorBack=colorBack                                       
        member this.ColorLeft=colorLeft
        member this.World
            with get () = Matrix.Translation(this.Ursprung)

        new(name, seitenLaenge, hoehe, color)= Pyramide(name, seitenLaenge , hoehe, color, color, color, color, color)

        override this.Center = Vector3(this.Ursprung.X + this.SeitenLaenge/ 2.0f, this.Ursprung.Y + this.Hoehe / 2.0f, this.Ursprung.Z + this.SeitenLaenge/ 2.0f)

        override this.resize newSize  = 
            this.SeitenLaenge <- this.SeitenLaenge * newSize 
            this.Hoehe <- this.Hoehe * newSize 

        override this.ToString() = "Pyramid l=  " + this.SeitenLaenge.ToString() + " h= " + this.Hoehe.ToString()
        
        // Umschließender Quader
        override this.Boundaries(objectPosition) = 
            this.Minimum <- objectPosition    
            this.Maximum <- Vector3(objectPosition.X + this.SeitenLaenge , objectPosition.Y + this.Hoehe, objectPosition.Z + this.SeitenLaenge)  
            (this.Minimum, this.Maximum)
        
        override this.getVertexData(isTransparent) =
            pyramidVertices this.Ursprung this.SeitenLaenge this.Hoehe this.ColorFront this.ColorRight this.ColorBack this.ColorLeft this.ColorBasis this.Topology isTransparent
        
        member this.planePositionAt(position: Vector3) =
 
            if approximatelyEqual(position.Z, this.Minimum.Z) then 
                PlanePosition.FRONT
            else if approximatelyEqual(position.Z, this.Maximum.Z) then 
                PlanePosition.BACK
            else if approximatelyEqual(position.X, this.Minimum.X) then 
                PlanePosition.LEFT  
            else if approximatelyEqual(position.X, this.Maximum.X) then 
                PlanePosition.RIGHT  
            else if approximatelyEqual(position.Y, this.Minimum.Y) then 
                PlanePosition.TOP  
            else if approximatelyEqual(position.Y, this.Maximum.Y) then 
                PlanePosition.BOTTOM  
            else    
                raise (GeometryException("Plane-Position für Normale nicht ermittelt"))

        // PYRAMIDE : Die Normale je nach Seite
        override this.getNormalAt(hitPosition, position: Vector3) = 
            let planePosition = this.planePositionAt(hitPosition)
            //logger.Debug(this.Name + " PLANEPOS  " + planePosition.ToString())
            match planePosition with
            | PlanePosition.FRONT  -> 
                Vector3.UnitZ * -1.0f
            | PlanePosition.BACK   -> 
                Vector3.UnitZ
            | PlanePosition.LEFT   -> 
                Vector3.UnitX * -1.0f
            | PlanePosition.RIGHT  -> 
                Vector3.UnitX
            | PlanePosition.BOTTOM -> 
                Vector3.UnitY * -1.0f
            | PlanePosition.TOP    -> 
                Vector3.UnitY
            | _ -> raise (GeometryException("Plane-Position für Normale nicht ermittelt"))

    // ----------------------------------------------------------------------------------------------------
    // Material
    // ----------------------------------------------------------------------------------------------------
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

    // ----------------------------------------------------------------------------------------------------
    // Quadrat
    // ----------------------------------------------------------------------------------------------------
    type QuadPatch(name: string, p1: Vector3, p2: Vector3, p3: Vector3, p4: Vector3, color:Color, tessFactor:float32) =
        inherit Geometric(name, Vector3.Zero, Color.White, PrimitiveTopology.PatchListWith4ControlPoints, tessFactor, DEFAULT_RASTER)
        let mutable seitenLaenge=0.0f

        do  
            seitenLaenge <- seiteVonQuadrat( p1, p2, p3, p4)

        member this.P1=p1
        member this.P2=p2
        member this.P3=p3
        member this.P4=p4                                   
        member this.ColorFront=color   
        
        new() = 
            QuadPatch("QuadPatch", Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Color.Black, 1.0f)

        new(name, seitenLaenge, color, tessFactor) = 
            QuadPatch(
             name,
             base.Ursprung,
             Vector3(base.Ursprung.X + seitenLaenge, base.Ursprung.Y, base.Ursprung.Z),
             Vector3(base.Ursprung.X + seitenLaenge, base.Ursprung.Y, base.Ursprung.Z + seitenLaenge),
             Vector3(base.Ursprung.X,                base.Ursprung.Y, base.Ursprung.Z + seitenLaenge),
             color,
             tessFactor
             ) 
             
        override this.Center = Vector3(this.Ursprung.X + this.SeitenLaenge/ 2.0f, this.Ursprung.Y , this.Ursprung.Z + this.SeitenLaenge/ 2.0f)

        override this.ToString() = "QuadPatch " + name + " Tess=" + this.TessFactor.ToString() 

        member this.SeitenLaenge
            with get() = seitenLaenge
            and  set(value) = seitenLaenge <- value

        override this.resize newSize  = 
            this.SeitenLaenge <- this.SeitenLaenge * newSize 
        
        override this.Boundaries(objectPosition) = 
            this.Minimum <- Vector3(objectPosition.X - this.SeitenLaenge  / 2.0f , -999.0f , objectPosition.Z- this.SeitenLaenge  / 2.0f )  
            this.Maximum <- Vector3(objectPosition.X + this.SeitenLaenge / 2.0f , objectPosition.Y / 2.0f , objectPosition.Z + this.SeitenLaenge / 2.0f ) 
            (this.Minimum, this.Maximum)

        override this.tesselationMode( ) = TesselationMode.QUAD

        override this.getVertexData(isTransparent) =
            quadContext this.P1 this.P2 this.P3 this.P4 this.ColorFront isTransparent

        override this.getNormalAt(hitPosition, position) = Vector3.UnitY

    // ----------------------------------------------------------------------------------------------------
    // Triangle
    // ----------------------------------------------------------------------------------------------------
    type TriPatch(name: string, p1: Vector3, p2: Vector3, p3: Vector3, color:Color, tessFactor:float32) =
        inherit Geometric(name, Vector3.Zero, Color.White, PrimitiveTopology.PatchListWith3ControlPoints, tessFactor, DEFAULT_RASTER)
        member this.P1=p1
        member this.P2=p2
        member this.P3=p3                                  
        member this.Color=color 

        new() = TriPatch("TriPatch", Vector3.Zero, Vector3.Zero, Vector3.Zero, Color.Black, 1.0f)
        new (p1, p2, p3, color) = TriPatch("TriPatch", p1, p2, p3, color, 1.0f)

        // TODO : HACK
        override this.Center = Vector3(this.Ursprung.X , this.Ursprung.Y, this.Ursprung.Z )

        override this.ToString() = this.P1.ToString() + " / "

        // TODO : HACK
        override this.Boundaries(objectPosition) = 
            this.Minimum <-  Vector3(min this.P1.X this.P2.X   ,min this.P1.Y  this.P2.Y , min this.P1.Z  this.P2.Z  )  
            this.Maximum <-  Vector3(max this.P1.X this.P2.X   , max this.P1.Y  this.P2.Y , max this.P1.Z  this.P2.Z  )
            (this.Minimum, this.Maximum)

        override this.resize newSize  = 
            ()  // HACK 

        override this.tesselationMode( ) = TesselationMode.TRI

        override this.getVertexData(isTransparent) =
            triContext this.P1 this.P2 this.P3 this.Color isTransparent

        override this.getNormalAt(hitPosition, position) = Vector3.UnitY

    // ----------------------------------------------------------------------------------------------------
    // Fläche aus Quadraten
    // ----------------------------------------------------------------------------------------------------
    type QuadPlane(name: string, seitenLaenge:float32, patchLaenge:float32, color:Color, tessFactor:float32) =
        inherit Geometric(name, Vector3.Zero, Color.White, PrimitiveTopology.PatchListWith4ControlPoints, tessFactor, DEFAULT_RASTER)
        member this.SeitenLaenge=seitenLaenge
        member this.PatchLaenge=patchLaenge

        new (name, seitenLaenge, patchLaenge, color) = QuadPlane (name, seitenLaenge, patchLaenge, color, 1.0f) 

        override this.ToString() = this.Name + " L " + this.SeitenLaenge.ToString() + " P " + this.PatchLaenge.ToString()
        
        // Maximum ist die Begrenzung der Fläche auf ihrer Höhe
        // Minimum ist die gleiche Fläche in der Tiefe 999 weil Displayable.MIN hier nicht erreichbar ist
        override this.Boundaries(objectPosition) = 
            this.Minimum <- Vector3(objectPosition.X, -999.0f , objectPosition.Z) 
            this.Maximum <- Vector3(objectPosition.X + seitenLaenge, objectPosition.Y, objectPosition.Z + seitenLaenge) 
            (this.Minimum, this.Maximum) 

        override this.Center = Vector3(base.Ursprung.X + this.SeitenLaenge / 2.0f, base.Ursprung.Y+ this.SeitenLaenge / 2.0f, base.Ursprung.Z+ this.SeitenLaenge / 2.0f)

        override this.resize newSize  = 
            ()  // HACK 
        
        override this.tesselationMode( ) = TesselationMode.NONE

        override this.getVertexData(isTransparent) =
            quadPlaneContext this.SeitenLaenge this.PatchLaenge this.Color  isTransparent

        override this.getNormalAt(hitPosition, position) = Vector3.UnitY

    // ----------------------------------------------------------------------------------------------------
    // Polyeder
    // ----------------------------------------------------------------------------------------------------
    type Polyeder(name: string, center: Vector3, radius:float32, color:Color, tessFactor:float32) =
        inherit Geometric(name, Vector3.Zero, color, PrimitiveTopology.PatchListWith3ControlPoints, tessFactor, DEFAULT_RASTER)
        let mutable center=center
        let mutable radius=radius

        member this.Radius
            with get() = radius
            and set(value) = radius <- value

        member this.TessFactor = tessFactor

        override this.ToString() = this.Name 
        override this.tesselationMode( ) = TesselationMode.TRI
        override this.Center = center

        override this.resize newSize  = 
            this.Radius <- this.Radius * newSize

        override this.Boundaries(objectPosition) = 
            this.Minimum <- Vector3(objectPosition.X - this.Radius, objectPosition.Y - this.Radius, objectPosition.Z - this.Radius) 
            this.Maximum <- Vector3(objectPosition.X + this.Radius, objectPosition.Y + this.Radius, objectPosition.Z + this.Radius) 
            (this.Minimum, this.Maximum)

        override this.getVertexData(isTransparent) =
            icosahedronContext this.Radius this.Color this.Topology isTransparent

        override this.getNormalAt(hitPosition, position) = Vector3.UnitY

    // ----------------------------------------------------------------------------------------------------
    // Corpus
    // TODO: center ist zu berechnen als Schwerpunkt des Polygons
    // ----------------------------------------------------------------------------------------------------
    type Corpus(name: string, center: Vector3, contour: Vector3[], height:float32, colorBottom:Color, colorTop:Color, colorSide:Color) =
        inherit Geometric(name, Vector3.Zero, colorTop, PrimitiveTopology.PatchListWith3ControlPoints, DEFAULT_TESSELATION, DEFAULT_RASTER)
        let mutable center=center
        member this.Contour=contour
        member this.Height=height
        member this.ColorBottom=colorBottom
        member this.ColorTop=colorTop
        member this.ColorSide=colorSide

        new (name, center, contour, height, color) = Corpus (name, center, contour, height, color, color, color)  

        override this.ToString() = this.Name 
        override this.tesselationMode( ) = TesselationMode.TRI

        override this.Center = center
        
        override this.resize newSize  = 
            () // HACK

        // Umschließender Quader
        // TODO: objectPosition berücksichtigen
        override this.Boundaries(objectPosition) = 
            this.Minimum <- computeMinPosition(this.Contour) + objectPosition
            this.Maximum <- computeMaxPosition(this.upperContour()) + objectPosition
            (this.Minimum, this.Maximum)

        member this.upperContour() =
            this.Contour |> Array.map (fun point -> Vector3(point.X, point.Y + this.Height, point.Z))

        override this.getVertexData(isTransparent) =
            corpusContext this.Center this.Contour this.Height this.ColorBottom this.ColorTop this.ColorSide this.Topology isTransparent
            
        // Die Seite ermitteln
        member this.planePositionAt(position: Vector3) =
 
            if approximatelyEqual(position.Z, this.Minimum.Z) then 
                PlanePosition.FRONT
            else if approximatelyEqual(position.Z, this.Maximum.Z) then 
                PlanePosition.BACK
            else if approximatelyEqual(position.X, this.Minimum.X) then 
                PlanePosition.LEFT  
            else if approximatelyEqual(position.X, this.Maximum.X) then 
                PlanePosition.RIGHT  
            else if approximatelyEqual(position.Y, this.Minimum.Y) then 
                PlanePosition.TOP  
            else if approximatelyEqual(position.Y, this.Maximum.Y) then 
                PlanePosition.BOTTOM  
            else    
                raise (GeometryException("Plane-Position  nicht ermittelt"))

        // Quader : Die Normale je nach Seite
        override this.getNormalAt(hitPosition, position: Vector3) = 
            let planePosition = this.planePositionAt(hitPosition)
            //logger.Debug(this.Name + " PLANEPOS  " + planePosition.ToString())
            match planePosition with
            | PlanePosition.FRONT  -> 
                Vector3.UnitZ * -1.0f
            | PlanePosition.BACK   -> 
                Vector3.UnitZ
            | PlanePosition.LEFT   -> 
                Vector3.UnitX * -1.0f
            | PlanePosition.RIGHT  -> 
                Vector3.UnitX
            | PlanePosition.BOTTOM -> 
                Vector3.UnitY * -1.0f
            | PlanePosition.TOP    -> 
                Vector3.UnitY
            | _ -> raise (GeometryException("Plane-Position für Normale nicht ermittelt"))

    // ----------------------------------------------------------------------------------------------------
    // DreiD
    // ----------------------------------------------------------------------------------------------------
    type DreiD(name: string, fileName: string, color:Color, tessFactor:float32) =
        inherit Geometric(name, Vector3.Zero, color, PrimitiveTopology.TriangleList, tessFactor , DEFAULT_RASTER)        
        member this.FileName=fileName
        member this.Points:Vector3[]=[||]
        override this.ToString() = this.Name 
        override this.tesselationMode( ) = TesselationMode.NONE
        override this.Center = Vector3.Zero        
        override this.resize newSize  = 
            () // HACK
        override this.Boundaries(objectPosition) = 
            this.Minimum <- computeMinPosition(this.Points)
            this.Maximum <- computeMaxPosition(this.Points)
            (this.Minimum, this.Maximum) 

        override this.getVertexData(isTransparent) =
            dreiDcontext this.FileName color isTransparent

        override this.getNormalAt(hitPosition, position) = Vector3.UnitY