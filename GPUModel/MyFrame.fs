namespace GPUModel
//
//  MyFrame.fs
//
//  Ported from Luna Directx 12 Game programming  to F#
//

open System

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