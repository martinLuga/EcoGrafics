namespace GltfBase
//
//  MyFrame.fs
//
//  Ported from Luna Directx 12 Game programming  to F#
//

open System

open GPUModel.MYUtils
open GPUModel.FieldBuffer

// ----------------------------------------------------------------------------
// Die Puffer für die Übergabe an die Shader
//  Pro Objekt
//  Pro Frame
//  Pro Material
// ----------------------------------------------------------------------------
module FrameResource = 
    type Device = SharpDX.Direct3D12.Device 
    type Resource = SharpDX.Direct3D12.Resource 

    [<AllowNullLiteralAttribute>]
    type FrameResource(device:Device, recorder:Recorder, objectCount:int, objectLength:int, materialCount:int, materialLength:int, frameLength:int) =
        let recorder = recorder 
        let mutable viewCB:FieldBuffer = null
        let mutable frameCB:FieldBuffer = null 
        let mutable materialCB:FieldBuffer = null

        let mutable fenceValue:int64 = 0L
    
        do
            viewCB          <-  new FieldBuffer(device, objectCount, objectLength)
            frameCB         <-  new FieldBuffer(device, 1, frameLength) 
            materialCB      <-  new FieldBuffer(device, materialCount, materialLength) 

        interface IDisposable with 
            member this.Dispose() = 
                (viewCB:> IDisposable).Dispose()
                (materialCB:> IDisposable).Dispose()
                (frameCB:> IDisposable).Dispose()

        override this.ToString() = "MyFrameResource-" + recorder.ToString()

        member this.Recorder  
            with get() = recorder

        member this.FrameCB
            with get() = frameCB

        member this.MaterialCB
            with get() = materialCB

        member this.ViewCB
            with get() = viewCB

        // Fence value to mark commands up to this fence point.  This lets us
        // check if these frame resources are still in use by the GPU.
        member this.FenceValue
            with get() = fenceValue
            and set(value) = fenceValue <- value