namespace Geometry
//
//  VertexSphere.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX

open Base.GlobalDefs 
open Base.VertexDefs
open Base.MeshObjects 
open Base.ModelSupport

module VertexSphere  = 

    // ----------------------------------------------------------------------------------------------------
    // Vertices 
    // Berechnung erfolgt nur für den Radius, d.h. Center = Nullpunkt
    // In der Anwendung orientiert sich ein Objekt immer an der linken unteren Ecke als Ursprung
    // Deshalb wird immer noch der Vektor Center=(r, r, r) aufaddiert
    // ----------------------------------------------------------------------------------------------------
    let sphereVertices (ursprung:Vector3, color:Color, radius:float32, tessellation:int32, isTransparent) =
    
        let mutable delta = IIpi / float32 tessellation 
        let mutable color4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()

        let center = ursprung + Vector3(radius, radius, radius)
        let verticalSegments = tessellation
        let horizontalSegments = tessellation * 2
        let vertices = Array.create ((verticalSegments + 1) * (horizontalSegments + 1)) (new Vertex())
        let mutable vertexCount = 0

        // Create rings of vertices at progressively higher latitudes.
        for i = 0 to verticalSegments do 
            
            let v = 1.0f - ((float32)i/(float32)verticalSegments)
            let latitude =  ((float32)i * pi / (float32)verticalSegments) - pihalbe
            let dy =  sin(latitude)
            let dxz =  cos(latitude)

            // Create a single ring of vertices at this latitude.
            for j = 0 to horizontalSegments do
                let u = (float32)j / (float32)horizontalSegments

                let longitude = ((float32)j * IIpi / (float32)horizontalSegments) 
                let mutable dx =  sin(longitude)  
                let mutable dz =  cos(longitude)  

                dx <- dx * dxz
                dz <- dz * dxz

                let normal = new Vector3(dx, dy, dz)
                let position = normal * radius + center

                // To generate a UV texture coordinate:
                let textureCoordinate = new Vector2(u, v)
                // To generate a UVW texture cube coordinate
                // let textureCoordinate = normal 

                vertices.[vertexCount] <- createVertex position normal color4 textureCoordinate     
                vertexCount <- vertexCount + 1 

        vertices

    let sphereIndices (clockWiseWinding:bool, tessellation:int32) =
        // Fill the index buffer with triangles joining each pair of latitude rings.
        let verticalSegments = tessellation
        let horizontalSegments = tessellation * 2

        let indices = Array.create ((verticalSegments) * (horizontalSegments + 1) * 6) (int32 0)
        let stride = horizontalSegments + 1
        let mutable indexCount = 0

        for i = 0 to verticalSegments - 1 do 
            for j = 0 to horizontalSegments do
                let nextI = i + 1
                let nextJ = (j + 1) % stride

                indices.[indexCount] <- (i * stride + j) 
                indexCount <- indexCount + 1

                // Implement correct winding of vertices
                if clockWiseWinding then
                    indices.[indexCount]  <- (i * stride + nextJ)
                    indexCount <- indexCount + 1
                    indices.[indexCount]  <- (nextI * stride + j)
                    indexCount <- indexCount + 1
                else
                    indices.[indexCount]  <- (nextI * stride + j)
                    indexCount <- indexCount + 1
                    indices.[indexCount]  <- (i * stride + nextJ)
                    indexCount <- indexCount + 1

                indices.[indexCount] <- (i * stride + nextJ)
                indexCount <- indexCount + 1

                // Implement correct winding of vertices
                if clockWiseWinding then
                    indices.[indexCount] <- (nextI * stride + nextJ)
                    indexCount <- indexCount + 1
                    indices.[indexCount]  <- (nextI * stride + j)
                    indexCount <- indexCount + 1
                else
                    indices.[indexCount]  <-  (nextI * stride + j)
                    indexCount <- indexCount + 1
                    indices.[indexCount]  <-  (nextI * stride + nextJ)
                    indexCount <- indexCount + 1
        indices

    let CreateMeshData(ursprung:Vector3, color:Color, radius:float32, tesselation:int, visibility:Visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        let vertices = sphereVertices(ursprung, color, radius, tesselation, isTransparent)
        let indices = sphereIndices(CLOCKWISE, tesselation)
        new MeshData<Vertex>(vertices, indices)