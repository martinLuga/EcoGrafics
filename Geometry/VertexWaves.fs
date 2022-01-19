namespace Geometry
//
//  VertexWaves.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open Base.MeshObjects 
open Base.ModelSupport
open Base.VertexDefs

open SharpDX

open System.Diagnostics
open System.Threading.Tasks

module VertexWaves  = 

    [<AllowNullLiteral>]
    type Waves(m:int, n:int, dx:float32 , dt:float32, speed:float32, damping:float32) =
 
        // Simulation constants we can precompute.
        let mutable _k1:float32 = 0.0f
        let mutable _k2:float32 = 0.0f
        let mutable _k3:float32 = 0.0f

        let mutable _t:float32 = 0.0f
        let mutable _timeStep:float32 = dt
        let mutable _spatialStep:float32 = dx

        let mutable _prevSolution:Vector3[] = null
        let mutable _currSolution:Vector3[] = null
        let mutable _normals:Vector3[] = null
        let mutable _tangentX:Vector3[] = null
        let mutable _rowCount = m
        let mutable _columnCount = n
        let mutable _vertexCount = m * n
        let mutable _triangleCount = (m - 1) * (n - 1) * 2
        let mutable _vertices:Vertex[] = [||]
        let mutable _indices:int[] = [||]
        let mutable _meshdata:MeshData<Vertex> = new MeshData<Vertex>()

        do    

            let d = damping * dt + 2.0f
            let e = (speed * speed) * (dt * dt) / (dx * dx)
            _k1 <- (damping * dt - 2.0f) / d
            _k2 <-  (4.0f - 8.0f  * e) / d
            _k3 <-  (2.0f * e) / d

            _prevSolution   <- Array.create _vertexCount Vector3.Zero  
            _currSolution   <- Array.create _vertexCount Vector3.Zero
            _normals        <- Array.create _vertexCount Vector3.Zero
            _tangentX       <- Array.create _vertexCount Vector3.Zero
            _vertices       <- Array.create _vertexCount (new Vertex())
            _meshdata       <- new MeshData<Vertex>()

            // Generate grid vertices in system memory.

            let halfWidth = (float32 n - 1.0f) * dx * 0.5f 
            let halfDepth = (float32 m - 1.0f) * dx * 0.5f 
            
            for  i = 0 to m - 1  do        
                let z = halfDepth - float32 i * dx
                for  j = 0 to n - 1 do            
                    let x = -halfWidth + float32 j * dx
                    _prevSolution[i * n + j] <- new Vector3(x, 0.0f, z)
                    _currSolution[i * n + j] <-  new Vector3(x, 0.0f, z)
                    _normals[i * n + j]  <-  Vector3.UnitY
                    _tangentX[i * n + j] <-  Vector3.UnitX 

        member this.Vertices
            with get () = _vertices 

        member this.RowCount
            with get () = _rowCount 

        member this.ColumnCount
            with get () = _columnCount 

        member this.VertexCount
            with get () = _vertexCount 

        member this.TriangleCount
            with get () = _triangleCount

        member this.Width = 
            float32 this.ColumnCount * _spatialStep

        member this.Depth = 
            float32 this.RowCount * _spatialStep

        // Returns the solution at the ith grid point.
        member this.Position(i) =
            _currSolution[i]

        // Returns the solution normal at the ith grid point.
        member this.Normal(i) =
            _normals[i]

        // Returns the unit tangent vector at the ith grid point in the local x-axis direction.
        member this.TangentX(i) =
            _tangentX[i]

        member this.Update(dt:float32) =
    
            // Accumulate time.
            _t <- _t + dt

            // Only update the simulation at the specified time step.
            if (_t >= _timeStep) then
        
                // Only update interior points we use zero boundary conditions.
                Parallel.For(1, this.RowCount - 1, fun i -> 
            
                    for  j = 1 to this.ColumnCount - 2  do
                
                        // After this update we will be discarding the old previous
                        // buffer, so overwrite that buffer with the new update.
                        // Note how we can do this inplace (read/write to same element)
                        // because we won't need prev_ij again and the assignment happens last.

                        // Note j indexes x and i indexes z: h(x_j, z_i, t_k)
                        // Moreover, our +z axis goes "down" this is just to
                        // keep consistent with our row indices going down.

                        _prevSolution[i * this.ColumnCount + j].Y <-
                            _k1 * _prevSolution[i * this.ColumnCount + j].Y +
                            _k2 * _currSolution[i * this.ColumnCount + j].Y +
                            _k3 * (_currSolution[(i + 1) * this.ColumnCount + j].Y +
                            _currSolution[(i - 1) * this.ColumnCount + j].Y +
                            _currSolution[i * this.ColumnCount + j + 1].Y +
                            _currSolution[i * this.ColumnCount + j - 1].Y)
                
                ) |> ignore

                // We just overwrote the previous buffer with the new data, so
                // this data needs to become the current solution and the old
                // current solution becomes the new previous solution.
                let temp = _prevSolution
                _prevSolution <- _currSolution
                _currSolution <- temp

                // Reset time.
                _t <- 0.0f

                //
                // Compute normals using finite difference scheme.
                //
                Parallel.For(1, this.RowCount - 1, fun i -> 
            
                    for  j = 1 to this.ColumnCount - 2 do
                
                        let l = _currSolution[i * this.ColumnCount + j - 1].Y
                        let r = _currSolution[i * this.ColumnCount + j + 1].Y
                        let t = _currSolution[(i - 1) * this.ColumnCount + j].Y
                        let b = _currSolution[(i + 1) * this.ColumnCount + j].Y

                        _normals[i * this.ColumnCount + j] <- Vector3.Normalize(new Vector3(-r + l, 2.0f * _spatialStep, b - t))
                        _tangentX[i * this.ColumnCount + j] <- Vector3.Normalize(new Vector3(2.0f * _spatialStep, r - l, 0.0f))
                
                ) |> ignore       
    

        member this.Disturb(i, j, magnitude:float32) =
    
            // Don't disturb boundaries.
            Debug.Assert(i > 1 && i < this.RowCount - 2)
            Debug.Assert(j > 1 && j < this.ColumnCount - 2)

            let halfMag = 0.5f * magnitude

            // Disturb the ijth vertex height and its neighbors.

            _currSolution[i * this.ColumnCount + j].Y <-
                _currSolution[i * this.ColumnCount + j].Y + magnitude
            
            _currSolution[i * this.ColumnCount + j + 1].Y <-
                _currSolution[i * this.ColumnCount + j + 1].Y + halfMag 
            
            _currSolution[i * this.ColumnCount + j - 1].Y  <- 
                _currSolution[i * this.ColumnCount + j - 1].Y   + halfMag
            
            _currSolution[(i + 1) * this.ColumnCount + j].Y   <-
                _currSolution[(i + 1) * this.ColumnCount + j].Y + halfMag
            
            _currSolution[(i - 1) * this.ColumnCount + j].Y <-
                _currSolution[(i - 1) * this.ColumnCount + j].Y + halfMag

        member this.MeshData 
            with get() =
                if _meshdata.Indices.Count = 0 then 
                    _meshdata.AddIndices(this.Indices)  
                _meshdata

        member this.Indices  
            with get() =
                if _indices.Length = 0 then 
                    _indices <- Array.create (3 * this.TriangleCount) 0   // 3 indices per face.
                    Debug.Assert(this.VertexCount < System.Int32.MaxValue)

                    // Iterate over each quad.
                    let m = this.RowCount 
                    let n = this.ColumnCount 
                    let mutable k = 0 
                    for i = 0 to m - 2 do               
                        for  j = 0 to n - 2 do                   
                            _indices[k + 0] <-  (i * n + j)
                            _indices[k + 1] <-  (i * n + j + 1)
                            _indices[k + 2] <-  ((i + 1) * n + j)

                            _indices[k + 3] <-  ((i + 1) * n + j)
                            _indices[k + 4] <-  (i * n + j + 1)
                            _indices[k + 5] <-  ((i + 1) * n + j + 1)
                            k <- k + 6 // Next quad.
                _indices

        member this.UpdateVertices(color:Color) = 

            for i = 0 to this.VertexCount - 1 do 
                _vertices.[i].Position <- this.Position(i)
                _vertices.[i].Normal <- this.Normal(i)
                _vertices.[i].Color <- color.ToColor4()
        
                // Derive tex-coords from position by
                // mapping [-w/2,w/2] --> [0,1]

                _vertices.[i].Texture <-
                    new Vector2(
                        0.5f + this.Position(i).X / this.Width,
                        0.5f - this.Position(i).Z / this.Depth
                    )

        member this.CreateMeshData(color:Color, visibility) = 
            let isTransparent = TransparenceFromVisibility(visibility)
            this.UpdateVertices(color)
            this.MeshData.Vertices <- _vertices |> ResizeArray<Vertex> 
            this.MeshData

    let CreateMeshData(waves:Waves, color, visibility) =
        let isTransparent = TransparenceFromVisibility(visibility)
        waves.UpdateVertices(color)
        new MeshData<Vertex>(waves.Vertices, waves.Indices)