namespace Geometry
//
//  VertexSphere.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX

open Base.GlobalDefs
open DirectX.VertexDefs
open Geometry.GeometryUtils

module VertexSphere  = 

    let mutable raster:int32 = 32
    let mutable delta = IIpi / float32 raster 

    let setSphereRaster (newRaster:Raster) = 
        raster <- (int newRaster)
        delta  <- IIpi / float32 raster

    let sphereVertices (color:Color) (radius:float32) isTransparent =

        let verticalSegments = raster
        let horizontalSegments = raster * 2
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
                let position = normal * radius

                // To generate a UV texture coordinate:
                let textureCoordinate = new Vector2(u, v)
                // To generate a UVW texture cube coordinate
                // let textureCoordinate = normal 

                vertices.[vertexCount] <- createVertex position normal color textureCoordinate isTransparent   
                vertexCount <- vertexCount + 1 

        vertices

    let sphereIndices (clockWiseWinding:bool) =
        // Fill the index buffer with triangles joining each pair of latitude rings.
        let verticalSegments = raster
        let horizontalSegments = raster * 2

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