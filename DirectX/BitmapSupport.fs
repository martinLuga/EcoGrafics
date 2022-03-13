namespace DirectX
//
//  BitmapSupport.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System
open System.IO

open SharpDX
open SharpDX.IO
open SharpDX.WIC
open SharpDX.DXGI
open SharpDX.Direct3D12
open SharpDX.Mathematics.Interop

open Base.Framework

open DX12GameProgramming

// ----------------------------------------------------------------------------------------------------
// Read and install bitmap-files of multiple formats
// ----------------------------------------------------------------------------------------------------

module BitmapSupport =

    type Device = SharpDX.Direct3D12.Device
    type Resource = SharpDX.Direct3D12.Resource
    type ShaderBytecode = SharpDX.Direct3D12.ShaderBytecode 

    let factory = new ImagingFactory()

    let getDecoder (extension) =
        let mutable decoder: BitmapDecoder = null

        match extension with
        | ".jpg"
        | "image/jpg" -> decoder <- new JpegBitmapDecoder(factory)
        | "image/png" -> decoder <- new PngBitmapDecoder(factory)
        | _ -> ()

        decoder

    // ----------------------------------------------------------------------------------------------------
    // Manager
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>]
    type BitmapManager(_device:Device) =
        let mutable device = _device
        let mutable stream: WICStream = null
        let mutable decoder: BitmapDecoder = null
        let mutable pixelFormat = PixelFormat.FormatDontCare
        let mutable image: byte [] = null
        let mutable bitmap:System.Drawing.Bitmap = null
        let mutable converter = new FormatConverter(factory)
        let mutable frame: BitmapFrameDecode = null
        let mutable size: Size2 = new Size2()
        let mutable width = 0
        let mutable height = 0
        let mutable stride = width * sizeof<UInt32>
        let mutable imageSize = 0

        member this.InitFromArray(mimeType, data) =
            this.InitFromArray(mimeType, data)

        member this.Image
            with get() = image

        member this.InitFromFile(fileName: string) =
            let file = new FileInfo(fileName)
            decoder <- getDecoder (file.Extension)
            stream <- new WICStream(factory, fileName, NativeFileAccess.Read)
            this.initialize ()

        member this.InitFromArray(extension: string, array: byte[]) =
            let dataStream = ByteArrayToStream(array, 0, array.Length)
            this.InitFromStream(extension , dataStream)

        member this.InitFromStream(extension: string, dataStream: Stream) =
            decoder <- getDecoder (extension)
            stream <- new WICStream(factory, dataStream)
            this.initialize ()

        member this.initialize() =
            decoder.Initialize(stream, DecodeOptions.CacheOnDemand)
            frame <- decoder.GetFrame(0)
            pixelFormat <- frame.PixelFormat
            size <- frame.Size
            width <- size.Width
            height <- size.Height
            stride <- width * sizeof<UInt32>
            imageSize <- stride * height
            converter <- new FormatConverter(factory)

        member this.Copy() =
            image <- Array.zeroCreate imageSize
            if pixelFormat = PixelFormat.Format32bppRGBA then
                frame.CopyPixels(image)
            else if (converter.CanConvert(pixelFormat, PixelFormat.Format32bppRGBA)) = RawBool(true) then
                converter.Initialize(
                    frame,
                    PixelFormat.Format32bppRGBA,
                    BitmapDitherType.ErrorDiffusion,
                    null,
                    0,
                    BitmapPaletteType.MedianCut
                )
                converter.CopyPixels(image, stride)
            else
                raise (SystemException("Format convert"))

        member this.GetBitmap() =
            let nativeint = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(image,0)
            let intptr = new System.IntPtr(nativeint.ToPointer())
            bitmap <- new System.Drawing.Bitmap(
                width,
                height,
                stride,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb,
                intptr) 
                
        member this.CreateTextureFromDDS(textureFilename) =        
            TextureUtilities.CreateTextureFromDDS(device, textureFilename)

        member this.CreateTextureFromBitmap() =

            this.Copy()
            this.GetBitmap()
            
            let width = bitmap.Width 
            let height = bitmap.Height 

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