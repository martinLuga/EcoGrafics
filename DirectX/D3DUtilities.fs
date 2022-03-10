namespace DirectX
//
//  D3DUtil.fs
//
//  Ported from Luna Directx 12 Game programming  to F#
//

open System
open System.Runtime.InteropServices

open System.IO
open System.Linq

open SharpDX
open SharpDX.D3DCompiler
open SharpDX.Direct3D
open SharpDX.Mathematics.Interop
open SharpDX.DXGI
open SharpDX.Direct3D12

// ----------------------------------------------------------------------------
// Portiert aus DX12GameProgramming
// ----------------------------------------------------------------------------
module D3DUtilities =

    type Device = SharpDX.Direct3D12.Device
    type Resource = SharpDX.Direct3D12.Resource
    type ShaderBytecode = SharpDX.Direct3D12.ShaderBytecode 

    [<Literal>] 
    let MAXLIGHTS = 16 

    let NUMFRAMERESOURCES = 3   // TODO Überprüfen
    let FRAMECOUNT = 2  
    let SWAPCHAINBUFFERCOUNT = 2 
    let DSVDESCRIPTORCOUNT = 1

    let viewDimension(m4xMsaaState) = 
        if m4xMsaaState = true then DepthStencilViewDimension.Texture2DMultisampled else DepthStencilViewDimension.Texture2D

    let createSwapChain(handle, aFactory:Factory4, width, height, commandQueue) =
        use factory = aFactory
        let swapChainDescription = 
            new SwapChainDescription(  
                ModeDescription = 
                    new ModeDescription(
                        Width = width,
                        Height = height,
                        Format = Format.R8G8B8A8_UNorm,
                        RefreshRate = new Rational(60, 1),                        
                        Scaling = DisplayModeScaling.Unspecified,
                        ScanlineOrdering = DisplayModeScanlineOrder.Unspecified
                    ),
                Usage = Usage.RenderTargetOutput,
                BufferCount = FRAMECOUNT,
                SwapEffect = SwapEffect.FlipDiscard,
                SampleDescription = new SampleDescription(1, 0),
                Flags = SwapChainFlags.AllowModeSwitch,
                OutputHandle = handle,
                IsWindowed = RawBool(true)
            )

        let tempSwapChain = new SwapChain(factory, commandQueue, swapChainDescription) 
        let swapChain = tempSwapChain.QueryInterface<SwapChain3>() 
        tempSwapChain.Dispose() 
        swapChain

    // Required for ShaderBytecode.CompileFromFile API in order to resolve #includes in shader files.
    // Equivalent for D3D_COMPILE_STANDARD_FILE_INCLUDE.
    type FileIncludeHandler () =

        inherit CallbackBase  ()

        interface Include with
            member this.Open(typ:IncludeType, fileName:string, parentStream:Stream) = 
                 let mutable filePath = fileName 
                 if not (Path.IsPathRooted(filePath)) then 
                     let selectedFile = Path.Combine(Environment.CurrentDirectory, fileName) 
                     if (File.Exists(selectedFile))then
                         filePath <- selectedFile  
                 new FileStream(filePath, FileMode.Open, FileAccess.Read)  :> Stream 

             member this.Close(stream:Stream) = stream.Close() 
 
    let DefaultFileIncludeHandler = new FileIncludeHandler() 

    type  D3DUtil() =
        static member DefaultShader4ComponentMapping = 5768 
        static member CreateDefaultBuffer<'T when 'T: struct and 'T: ( new : unit -> 'T ) and 'T:>ValueType>
            (device:Device,
             cmdList:GraphicsCommandList,
             initData:'T[] when 'T: struct and 'T: ( new : unit -> 'T ),
             byteSize:Int64,
             uploadBuffer:Resource outref) = 
            // Create the actual default buffer resource.
            let defaultBuffer = 
                device.CreateCommittedResource(
                    new HeapProperties(HeapType.Default),
                    HeapFlags.None,
                    ResourceDescription.Buffer(byteSize),
                    ResourceStates.Common
                ) 

            // In order to copy CPU memory data into our default buffer, we need to create
            // an intermediate upload heap.
            try
                uploadBuffer <- 
                    device.CreateCommittedResource(
                        new HeapProperties(HeapType.Upload),
                        HeapFlags.None,
                        ResourceDescription.Buffer(byteSize),
                        ResourceStates.GenericRead
                    ) 
             with :? SharpDX.SharpDXException as ex -> printfn "Exception! %s " (ex.Message)

            // Copy the data to the upload buffer.
            let ptr:IntPtr = uploadBuffer.Map(0) 
            Utilities.Write(ptr, initData, 0, initData.Length) |> ignore
            uploadBuffer.Unmap(0) 

            // Schedule to copy the data to the default buffer resource.
            cmdList.ResourceBarrierTransition(defaultBuffer, ResourceStates.Common, ResourceStates.CopyDestination) 
            cmdList.CopyResource(defaultBuffer, uploadBuffer) 
            cmdList.ResourceBarrierTransition(defaultBuffer, ResourceStates.CopyDestination, ResourceStates.GenericRead) 

            // Note: uploadBuffer has to be kept alive after the above function calls because
            // the command list has not been executed yet that performs the actual copy.
            // The caller can Release the uploadBuffer after it knows the copy has been executed.
            defaultBuffer  

        // Constant buffers must be a multiple of the minimum hardware
        // allocation size (usually 256 bytes). So round up to nearest
        // multiple of 256. We do this by adding 255 and then masking off
        // the lower 2 bytes which store all bits < 256.
        // Example: Suppose byteSize = 300.
        // (300 + 255) & ~255
        // 555 & ~255
        // 0x022B & ~0x00ff
        // 0x022B & 0xff00
        // 0x0200
        // 512
        // TODO OB DAS STIMMT ?????
        static member CalcConstantBufferByteSize<'T when 'T : struct>() = 
            (Marshal.SizeOf(typeof<'T>) + 255) &&& ~~~255

        static member CompileShader(fileName:string, entryPoint:string, profile:string, defines:ShaderMacro[]) = 
            let shaderFlags = ShaderFlags.None 
            // shaderFlags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization
            // ShaderFlags.OptimizationLevel3
            let result = 
                ShaderBytecode.CompileFromFile(
                    fileName,
                    entryPoint,
                    profile,
                    shaderFlags,
                    EffectFlags.None,
                    defines,
                    DefaultFileIncludeHandler 
                )
            new ShaderBytecode(result.Bytecode.Data)

    [<type:StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type Light =
        struct
        val  mutable  Strength:Vector3
        val  mutable  FalloffStart:float32  // Point/spot light only.
        val  mutable  Direction:Vector3     // Directional/spot light only.
        val  mutable  FalloffEnd:float32    // Point/spot light only.
        val  mutable  Position:Vector3      // Point/spot light only.
        val  mutable  SpotPower:float32     // Spot light only.
        new (strength, falloffStart, direction, falloffEnd, position, spotPower) =
            {Strength=strength; FalloffStart=falloffStart; Direction=direction; FalloffEnd=falloffEnd; Position=position; SpotPower=spotPower}
    end

    let DefaultLight = 
        new Light(
            new Vector3(0.5f),
            1.0f,
            Vector3.UnitY,
            10.0f,
            Vector3.Zero,
            64.0f
       )

    let DefaultLightArray:Light[] = Enumerable.Repeat(DefaultLight, MAXLIGHTS).ToArray() 

    [<type:StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type MaterialConstants =
        struct
        val  DiffuseAlbedo:Vector4
        val  FresnelR0:Vector3
        val  Roughness:float32
        val  MatTransform:Matrix            // Used in texture mapping.
        new (diffuseAlbedo, fresnelR0,roughness, matTransform) =
            {DiffuseAlbedo=diffuseAlbedo; FresnelR0=fresnelR0; Roughness=roughness; MatTransform=matTransform} 
    end

    let  DefaultMaterialConstants = 
        new MaterialConstants(
            Vector4.One,
            new Vector3(0.01f),
            0.25f,
            Matrix.Identity
            )

    // Simple struct to represent a material for our demos. A production 3D engine
    // would likely create a class hierarchy of Materials.
    [<AllowNullLiteral>] 
    type MaterialGPU (name:string, matCBIndex:int, diffuseSrvHeapIndex:int, diffuseAlbedo:Vector4, fresnelR0:Vector3, roughness:float32, hasTexture:bool) =
        let mutable name=name
        let mutable matCBIndex=matCBIndex
        let mutable diffuseSrvHeapIndex=diffuseSrvHeapIndex 
        let mutable normalSrvHeapIndex= -1 

        let mutable diffuseAlbedo=diffuseAlbedo 
        let mutable fresnelR0=fresnelR0
        let mutable roughness=roughness 
        let mutable numFramesDirty= NUMFRAMERESOURCES
        let mutable matTransform= Matrix.Identity 

        let mutable hasTexture=hasTexture

        new (name, constants:MaterialConstants) = MaterialGPU (name,  0, 0, constants.DiffuseAlbedo,constants.FresnelR0 ,  constants.Roughness, false)
 
        // Unique material name for lookup.
        member this.Name
            with get() = name
            and  set(value) = name <- value 

        // Index into constant buffer corresponding to this material.
        member this.MatCBIndex
            with get() = matCBIndex
            and  set(value) = matCBIndex <- value

        // Index into SRV heap for diffuse texture.
        member this.DiffuseSrvHeapIndex         
            with get() = diffuseSrvHeapIndex
            and  set(value) = diffuseSrvHeapIndex <- value

        // Index into SRV heap for normal texture.
        member this.NormalSrvHeapIndex
            with get() = normalSrvHeapIndex
            and  set(value) = normalSrvHeapIndex <- value

        // Dirty flag indicating the material has changed and we need to update the constant buffer.
        // Because we have a material constant buffer for each FrameResource, we have to apply the
        // update to each FrameResource. Thus, when we modify a material we should set
        // NumFramesDirty = NumFrameResources so that each frame resource gets the update.
        member this.NumFramesDirty
            with get() = numFramesDirty
            and  set(value) = numFramesDirty <- value

        // Material constant buffer data used for shading.
        member this.DiffuseAlbedo
            with get() = diffuseAlbedo
            and  set(value) = diffuseAlbedo <- value 

        member this.FresnelR0
            with get() = fresnelR0
            and  set(value) = fresnelR0 <- value 

        member this.Roughness
            with get() = roughness
            and  set(value) = roughness <- value 

        member this.MatTransform
            with get() = matTransform
            and  set(value) = matTransform <- value

        member this.HasTexture
            with get() = hasTexture
            and  set(value) = hasTexture <- value
            
        member this.ReduceDirty() =
            numFramesDirty <- numFramesDirty - 1

    type TextureGPU (name:string, filename:string, resource:Resource) =
        let mutable name=name
        let mutable filename=filename
        let mutable resource:Resource=resource
        let mutable uploadHeap:Resource=null

        interface IDisposable with 
            member this.Dispose() =  
                resource.Dispose() 
                uploadHeap.Dispose() 
  
        member this.Name
            with get() = name
            and set(value) = name <- value

        member this.Filename
            with get() = filename
            and set(value) = name <- value

        member this.Resource
            with get() = resource
            and set(value) = resource <- value

        member this.UploadHeap
            with get() = uploadHeap
            and set(value) = uploadHeap <- value

    type D3DExtensions()  = 
        member this.Copy(desc:GraphicsPipelineStateDescription) =
            let newDesc = 
                new GraphicsPipelineStateDescription ( 
                    BlendState = desc.BlendState,
                    CachedPSO = desc.CachedPSO,
                    DepthStencilFormat = desc.DepthStencilFormat,
                    DepthStencilState = desc.DepthStencilState,
                    SampleDescription = desc.SampleDescription,
                    DomainShader = desc.DomainShader,
                    Flags = desc.Flags,
                    GeometryShader = desc.GeometryShader,
                    HullShader = desc.HullShader,
                    IBStripCutValue = desc.IBStripCutValue,
                    InputLayout = desc.InputLayout,
                    NodeMask = desc.NodeMask,
                    PixelShader = desc.PixelShader,
                    PrimitiveTopologyType = desc.PrimitiveTopologyType,
                    RasterizerState = desc.RasterizerState,
                    RenderTargetCount = desc.RenderTargetCount,
                    SampleMask = desc.SampleMask,
                    StreamOutput = desc.StreamOutput,
                    VertexShader = desc.VertexShader,
                    RootSignature = desc.RootSignature
                )
            for i = 0 to desc.RenderTargetFormats.Length-1 do
                newDesc.RenderTargetFormats.[i] <- desc.RenderTargetFormats.[i] 
            newDesc 

    let CreateTextureFromBitmap(device:Device, bitmap:System.Drawing.Bitmap) =
        
        let width = bitmap.Width 
        let height = bitmap.Height 

        // Describe and create a Texture2D.
        let textureDesc = new ResourceDescription(             
                MipLevels = 1s,
                Format = Format.R8G8B8A8_UNorm,
                Width = width,
                Height = height,
                Flags = ResourceFlags.None,
                DepthOrArraySize = 1s,
                SampleDescription = new SampleDescription(1, 0),
                Layout = TextureLayout.Unknown,
                Dimension = ResourceDimension.Texture2D 
        ) 

        let buffer = device.CreateCommittedResource(
            new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0),
            HeapFlags.None,
            textureDesc,
            ResourceStates.GenericRead
         )

        let data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat) 

        buffer.WriteToSubresource(0, 
            new ResourceRegion(             
                Back = 1,
                Bottom = height,
                Right = width
            ),
            data.Scan0,
            4 * width,
            4 * width * height
        )

        let bufferSize = data.Height * data.Stride 
        bitmap.UnlockBits(data)  

        buffer

    let CreateSampler(_device:Device, _samplerDesc, _hdl) =
        _device.CreateSampler(_samplerDesc, _hdl) 