namespace DirectX
//
//  D3DUtil.fs
//
//  Ported from Luna Directx 12 Game programming  to F#
//

open System
open System.Runtime.InteropServices
open SharpDX.Direct3D12

open System.IO
open System.Linq

open SharpDX
open SharpDX.D3DCompiler
open SharpDX.Direct3D
open SharpDX.Mathematics.Interop
open SharpDX.DXGI

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

    // We define the enums below to provide the same values for mouse and keyboard input
    // as System.Windows.Forms does. This is done in order to prevent direct dependencies
    // from samples to System.Windows.Forms and System.Drawing.

    // Ref: System.Windows.Forms.MouseButtons
    [<Flags>]
    type MouseButtons =
        | Left = 1048576
        | None = 0
        | Right = 2097152
        | Middle = 4194304
        | XButton1 = 8388608
        | XButton2 = 16777216 

    // Ref: System.Windows.Forms.Keys
    type Keys =  
        | KeyCode = 65535
        | Modifiers = -65536
        | None = 0
        | LButton = 1
        | RButton = 2
        | Cancel = 3
        | MButton = 4
        | XButton1 = 5
        | XButton2 = 6
        | Back = 8
        | Tab = 9
        | LineFeed = 10
        | Clear = 12
        | Return = 13
        | Enter = 13
        | ShiftKey = 16
        | ControlKey = 17
        | Menu = 18
        | Pause = 19
        | Capital = 20
        | CapsLock = 20
        | KanaMode = 21
        | HanguelMode = 21
        | HangulMode = 21
        | JunjaMode = 23
        | FinalMode = 24
        | HanjaMode = 25
        | KanjiMode = 25
        | Escape = 27
        | IMEConvert = 28
        | IMENonconvert = 29
        | IMEAccept = 30
        | IMEAceept = 30
        | IMEModeChange = 31
        | Space = 32
        | Prior = 33
        | PageUp = 33
        | Next = 34
        | PageDown = 34
        | End = 35
        | Home = 36
        | Left = 37
        | Up = 38
        | Right = 39
        | Down = 40
        | Select = 41
        | Print = 42
        | Execute = 43
        | Snapshot = 44
        | PrintScreen = 44
        | Insert = 45
        | Delete = 46
        | Help = 47
        | D0 = 48
        | D1 = 49
        | D2 = 50
        | D3 = 51
        | D4 = 52
        | D5 = 53
        | D6 = 54
        | D7 = 55
        | D8 = 56
        | D9 = 57
        | A = 65
        | B = 66
        | C = 67
        | D = 68
        | E = 69
        | F = 70
        | G = 71
        | H = 72
        | I = 73
        | J = 74
        | K = 75
        | L = 76
        | M = 77
        | N = 78
        | O = 79
        | P = 80
        | Q = 81
        | R = 82
        | S = 83
        | T = 84
        | U = 85
        | V = 86
        | W = 87
        | X = 88
        | Y = 89
        | Z = 90
        | LWin = 91
        | RWin = 92
        | Apps = 93
        | Sleep = 95
        | NumPad0 = 96
        | NumPad1 = 97
        | NumPad2 = 98
        | NumPad3 = 99
        | NumPad4 = 100
        | NumPad5 = 101
        | NumPad6 = 102
        | NumPad7 = 103
        | NumPad8 = 104
        | NumPad9 = 105
        | Multiply = 106
        | Add = 107
        | Separator = 108
        | Subtract = 109
        | Decimal = 110
        | Divide = 111
        | F1 = 112
        | F2 = 113
        | F3 = 114
        | F4 = 115
        | F5 = 116
        | F6 = 117
        | F7 = 118
        | F8 = 119
        | F9 = 120
        | F10 = 121
        | F11 = 122
        | F12 = 123
        | F13 = 124
        | F14 = 125
        | F15 = 126
        | F16 = 127
        | F17 = 128
        | F18 = 129
        | F19 = 130
        | F20 = 131
        | F21 = 132
        | F22 = 133
        | F23 = 134
        | F24 = 135
        | NumLock = 144
        | Scroll = 145
        | LShiftKey = 160
        | RShiftKey = 161
        | LControlKey = 162
        | RControlKey = 163
        | LMenu = 164
        | RMenu = 165
        | BrowserBack = 166
        | BrowserForward = 167
        | BrowserRefresh = 168
        | BrowserStop = 169
        | BrowserSearch = 170
        | BrowserFavorites = 171
        | BrowserHome = 172
        | VolumeMute = 173
        | VolumeDown = 174
        | VolumeUp = 175
        | MediaNextTrack = 176
        | MediaPreviousTrack = 177
        | MediaStop = 178
        | MediaPlayPause = 179
        | LaunchMail = 180
        | SelectMedia = 181
        | LaunchApplication1 = 182
        | LaunchApplication2 = 183
        | OemSemicolon = 186
        | Oem1 = 186
        | Oemplus = 187
        | Oemcomma = 188
        | OemMinus = 189
        | OemPeriod = 190
        | OemQuestion = 191
        | Oem2 = 191
        | Oemtilde = 192
        | Oem3 = 192
        | OemOpenBrackets = 219
        | Oem4 = 219
        | OemPipe = 220
        | Oem5 = 220
        | OemCloseBrackets = 221
        | Oem6 = 221
        | OemQuotes = 222
        | Oem7 = 222
        | Oem8 = 223
        | OemBackslash = 226
        | Oem102 = 226
        | ProcessKey = 229
        | Packet = 231
        | Attn = 246
        | Crsel = 247
        | Exsel = 248
        | EraseEof = 249
        | Play = 250
        | Zoom = 251
        | NoName = 252
        | Pa1 = 253
        | OemClear = 254
        | Shift = 65536
        | Control = 131072
        | Alt = 262144 