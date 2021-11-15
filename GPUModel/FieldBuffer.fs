namespace GPUModel
//
//  ConstantBuffers.fs
//
//  Ported from Luna Directx 12 Game programming  to F#
//

open System
open System.Runtime.InteropServices
open System.Collections.Generic

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
module FieldBuffer = 
 
    [<AllowNullLiteral>] 
    type  FieldBuffer (device:Device, elementCount:int, elementSize:int) =         

        let mutable resource:Resource = null
        let mutable resourcePointer:IntPtr = new IntPtr() 
        let mutable elementByteSize = elementSize
        let maxElements = elementCount
        do
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

        member this.ElementAdress(idx:int) =
            if idx > maxElements then
                int64 (0)
            else
                resource.GPUVirtualAddress + int64 (idx *  elementByteSize)

        member this.CopyData(elementIndex:int , structure:'T byref) = 
            let pointer = IntPtr.Add(resourcePointer, elementIndex * elementByteSize)
            Marshal.StructureToPtr(structure, pointer, true)

    [<AllowNullLiteral>] 
    type  NamedFieldBuffer (device:Device, elementSize:int) = 
        let mutable nameIndex=0
        let mutable nameIndices = new Dictionary<string, int>()
        let mutable resource:Resource = null
        let mutable resourcePointer:IntPtr = new IntPtr() 
        let mutable elementByteSize = elementSize
        let mutable elementCount = 0

        interface IDisposable with 
            member this.Dispose() =   
                resource.Unmap(0)
                resource.Dispose()

        member this.Allocate() =
            resource <- 
                device.CreateCommittedResource(
                    new HeapProperties(HeapType.Upload),
                    HeapFlags.None,
                    ResourceDescription.Buffer(int64 elementByteSize * int64 elementCount),
                    ResourceStates.GenericRead
                    ) 
            resourcePointer <- resource.Map(0)

        member this.ElementAdressAtName(name:string) =
            let idx = nameIndices.Item(name)
            this.elementAdress(idx)

        member this.elementAdress(idx:int) =
            if idx > elementCount then
                int64 (0)
            else
                resource.GPUVirtualAddress + int64 (idx * elementByteSize)

        member this.CopyData(name:string, structure:'T byref) =  
            let mutable matIdx = 0
            if nameIndices.ContainsKey(name) then
                matIdx <- nameIndices.Item(name)
            else
                elementCount <- elementCount + 1
                nameIndices.Add(name, elementCount)
            let pointer = IntPtr.Add(resourcePointer, matIdx * elementByteSize)
            Marshal.StructureToPtr(structure, pointer, true)