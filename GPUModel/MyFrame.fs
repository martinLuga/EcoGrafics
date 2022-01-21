namespace GPUModel
//
//  MyFrame.fs
//
//  Ported from Luna Directx 12 Game programming  to F#
//

open System
open System.Runtime.InteropServices

open SharpDX
open SharpDX.Direct3D12
open SharpDX.Mathematics.Interop

open Base.VertexDefs
open DirectX.UploadBuffer

open FieldBuffer
open MYUtils

// ----------------------------------------------------------------------------
// Die Puffer für die Übergabe an die Shader
//  Pro Objekt
//  Pro Frame
//  Pro Material
// ----------------------------------------------------------------------------
module MyFrame = 
    type Device = SharpDX.Direct3D12.Device 
    type Resource = SharpDX.Direct3D12.Resource 

    // Für einen zu rendernden Frame
    // Alle Buffer zum Übergeben
    [<AllowNullLiteralAttribute>]
    type FrameResource(device:Device, recorder:Recorder, objectCount:int, objectLength:int, materialCount:int, materialLength:int, frameLength:int) =
        let recorder = recorder 
        let mutable vertexVB:UploadBuffer<Vertex> = null
        let mutable objectCB:FieldBuffer = null
        let mutable materialCB:FieldBuffer = null
        let mutable frameCB:FieldBuffer = null 
        let mutable fenceValue:int64 = 0L
        
        do
            vertexVB        <-  new UploadBuffer<Vertex>(device, objectCount, false)
            objectCB        <-  new FieldBuffer(device, objectCount, objectLength)
            materialCB      <-  new FieldBuffer(device, materialCount, materialLength) 
            frameCB         <-  new FieldBuffer(device, 1, frameLength) 

        interface IDisposable with 
            member this.Dispose() = 
                (vertexVB:> IDisposable).Dispose()
                (objectCB:> IDisposable).Dispose()
                (materialCB:> IDisposable).Dispose()
                (frameCB:> IDisposable).Dispose()

        override this.ToString() = "MyFrameResource-" + recorder.ToString()

        member this.Recorder  
            with get() = recorder

        member this.VertexVB
            with get() = vertexVB

        member this.FrameCB
            with get() = frameCB

        member this.MaterialCB
            with get() = materialCB

        member this.ObjectCB
            with get() = objectCB

        // Fence value to mark commands up to this fence point.  This lets us
        // check if these frame resources are still in use by the GPU.
        member this.FenceValue
            with get() = fenceValue
            and set(value) = fenceValue <- value

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type ObjectConstants =
        struct             
            val mutable World: Matrix                   
            val mutable View: Matrix
            val mutable InvView: Matrix
            val mutable Proj: Matrix
            val mutable InvProj: Matrix
            val mutable ViewProj: Matrix
            val mutable InvViewProj: Matrix
            val mutable WorldViewProjection: Matrix     // WorldViewProjection matrix
            val mutable WorldInverseTranspose: Matrix   // Inverse transpose of World
            val mutable ViewProjection: Matrix          // ViewProjection matrix
            val mutable EyePosW:Vector3
        end

    // Transpose the matrices so that they are in row major order for HLSL
    let Transpose (perObject:ObjectConstants) =
        perObject.View.Transpose()
        perObject.InvView.Transpose()
        perObject.Proj.Transpose()
        perObject.InvProj.Transpose()
        perObject.ViewProj.Transpose()
        perObject.InvViewProj.Transpose()
        perObject.World.Transpose()
        perObject.WorldInverseTranspose.Transpose()  
        perObject.WorldViewProjection.Transpose()
        perObject.ViewProjection.Transpose() 
        perObject

    //  Directional light
    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type DirectionalLight =
        struct  
            val mutable Color: Color4               // 16 bytes
            val mutable Direction: Vector3          // 12 bytes
            val _padding: float32                   // 4 bytes
            new(color,direction) = {Color=color; Direction=direction; _padding = 0.0f}            
            new(color) = DirectionalLight(color,Vector3.Zero)
        end 

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type MaterialConstants =
        struct   
            val mutable Ambient:  Color4           // 16 bytes       
            val mutable Diffuse:  Color4           // 16 bytes         
            val mutable Specular: Color4           // 16 bytes  
            val mutable SpecularPower: float32     // 4 bytes
            val mutable HasTexture: RawBool        // 4 bytes          
            val mutable _padding0: Vector2         // 8 bytes
            val mutable Emissive: Color4           // 16 bytes 
            val mutable UVTransform: Matrix        // 16 bytes     
            new(ambient, diffuse, specular, specularPower, hasTexture, emissive, uVTransform) = 
                {Ambient = ambient; Diffuse = diffuse; Specular = specular; SpecularPower=specularPower; HasTexture=hasTexture; _padding0=Vector2.Zero; Emissive=emissive; UVTransform=uVTransform }
            end

    //  Per frame constant buffer (camera position) 
    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type FrameConstants =
        struct  
            val mutable Light: DirectionalLight 
            val mutable CameraPosition:Vector3     
            val mutable TessellationFactor:float32  
            new(light, lightDir, cameraPosition, tessellationFactor) = {Light=light; CameraPosition=cameraPosition; TessellationFactor=tessellationFactor}
        end
