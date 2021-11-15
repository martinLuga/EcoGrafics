namespace GPUModel
//
//  ConstantBuffers.fs
//
//  Ported from Luna Directx 12 Game programming  to F#
//

open System
open System.Runtime.InteropServices
open SharpDX.Direct3D12

open DirectX.D3DUtilities

// ----------------------------------------------------------------------------
// Wrapper für GPU Buffer
//
//  struct T
//  resource  (lazy)
//  size
//  pointer
//
//  Usage: let objectCB = UploadBuffer<ObjectConstants>
//  In Frameresource
// ----------------------------------------------------------------------------
module UploadBuffer = 
 
    [<AllowNullLiteral>] 
    type  UploadBuffer<'T when 'T: struct> (device:Device, elementCount:int, isConstantBuffer:bool ) =         

        let mutable resource:Resource = null
        let mutable elementByteSize = 0
        let mutable resourcePointer:IntPtr = new IntPtr() 
        do
            elementByteSize <- 
                if isConstantBuffer then
                    D3DUtil.CalcConstantBufferByteSize<'T>()
                else
                    Marshal.SizeOf(typeof<'T>) 

            resource <- 
                device.CreateCommittedResource(
                    new HeapProperties(HeapType.Upload),
                    HeapFlags.None,
                    ResourceDescription.Buffer(int64 elementByteSize * int64 elementCount),
                    ResourceStates.GenericRead
                    ) 
            resourcePointer <- resource.Map(0)

        interface IDisposable with 
            member this.Dispose() =   
                resource.Unmap(0)
                resource.Dispose()
 
        // ----------------------------------------------------------------------------
        // Constant buffer elements need to be multiples of 256 bytes.
        // This is because the hardware can only view constant data
        // at m*256 byte offsets and of n*256 byte lengths.
        // typedef struct D3D12_CONSTANT_BUFFER_VIEW_DESC {
        // UINT64 OffsetInBytes; // multiple of 256
        // UINT   SizeInBytes;   // multiple of 256
        // } D3D12_CONSTANT_BUFFER_VIEW_DESC;
        // ----------------------------------------------------------------------------

        member this.Resource
            with get() = resource

        // We do not need to unmap until we are done with the resource. However, we must not write to
        // the resource while it is in use by the GPU (so we must use synchronization techniques).

        member this.CopyData(elementIndex:int , structure:'T byref) = 
            let pointer = IntPtr.Add(resourcePointer, elementIndex * elementByteSize)
            Marshal.StructureToPtr(structure, pointer, true)