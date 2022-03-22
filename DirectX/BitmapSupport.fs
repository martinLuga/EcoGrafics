namespace DirectX
//
//  BitmapSupport.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System
open System.IO
open System.Runtime.InteropServices

open SharpDX
open SharpDX.IO
open SharpDX.WIC
open SharpDX.DXGI
open SharpDX.Direct3D12
open SharpDX.Mathematics.Interop

open Base.Framework
open Base.FileSupport

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
        | "image/jpg"   -> decoder <- new JpegBitmapDecoder(factory)
        | ".png"
        | "image/png"   -> decoder <- new PngBitmapDecoder(factory)
        | _             -> decoder <- new BitmapDecoder(factory, Guid.Empty)

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
        let mutable imageArray: byte [][] = null
        let mutable bitmap:System.Drawing.Bitmap = null
        let mutable bitmapArray:System.Drawing.Bitmap[] = Array.zeroCreate 6
        let mutable converter = new FormatConverter(factory)
        let mutable frame: BitmapFrameDecode = null
        let mutable resource:Resource = null
        let mutable size: Size2 = new Size2()
        let mutable width = 0
        let mutable height = 0
        let mutable stride = width * sizeof<UInt32>
        let mutable imageSize = 0
        let mutable isCube = false
        let mutable fromArray = false
        let mutable fileType = "NotSet"

        member this.Image
            with get() = image

        member this.Resource
            with get() = resource
        
        member this.IsCube
            with get() = isCube

        member this.FromArray
            with get() = fromArray

        // ----------------------------------------------------------------------------------------------------
        // Initialize with sources: File, Directory, ByteArray
        // ----------------------------------------------------------------------------------------------------
        member this.InitFromFileSystem(path: string) =
            let attr = File.GetAttributes(path)
            if  attr = FileAttributes.Directory then
                this.InitFromArray(path)
            else 
                this.InitFromFile(path)


        //
        // 0 	GL_TEXTURE_CUBE_MAP_POSITIVE_X
        // 1 	GL_TEXTURE_CUBE_MAP_NEGATIVE_X
        // 2 	GL_TEXTURE_CUBE_MAP_POSITIVE_Y
        // 3 	GL_TEXTURE_CUBE_MAP_NEGATIVE_Y
        // 4 	GL_TEXTURE_CUBE_MAP_POSITIVE_Z
        // 5 	GL_TEXTURE_CUBE_MAP_NEGATIVE_Z
        //
        member this.InitFromArray(directoryPath) =
            isCube <- true
            fromArray <- true
            let files = filesInPath directoryPath          
            assert(files.Length=6) 
            bitmapArray <- Array.zeroCreate 6
            imageArray <- Array.zeroCreate 6
            let mutable i = 0

            let sorted = 
                [
                    files[3]  
                    files[0]
                    files[4]
                    files[1]
                    files[5]
                    files[2]                
                ]

            for file in sorted do
                stream <- new WICStream(factory, file.FullName, NativeFileAccess.Read)
                decoder <- getDecoder (file.Extension)
                this.initialize ()
                this.CreateImage()
                this.CreateBitmap()
                bitmapArray.[i] <- bitmap
                imageArray.[i] <- image
                i <- i + 1   

        member this.InitFromFile(fileName: string) =
            fromArray <- false
            if fileName.EndsWith("dds") then
                this.InitFromDDS(fileName)
            else 
                this.InitFromJPG(fileName)

        member this.InitFromJPG(fileName: string) =
            let file = new FileInfo(fileName)
            fileType <- file.Extension
            decoder <- getDecoder (file.Extension)
            stream <- new WICStream(factory, fileName, NativeFileAccess.Read)
            this.initialize ()

        member this.InitFromDDS(fileName: string) =        
            fileType <- "dds"
            image <- System.IO.File.ReadAllBytes(fileName)
            let nativeint = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(image,0)
            let ddsEncoder = new DdsDecoder(nativeint) 
            () // TODO Eventuell weiter verfolgen

        member this.InitFromByteArray(extension: string, array: byte[]) =
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

        member this.CreateImage() =
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

        member this.CreateBitmap() =
            let nativeint = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(image,0)
            let intptr = new System.IntPtr(nativeint.ToPointer())
            bitmap <- new System.Drawing.Bitmap(
                width,
                height,
                stride,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb,
                intptr) 
                
        member this.CreateTextureFromDDS() =        
            resource <- TextureUtilities.CreateTextureFromDDS(device, image, &isCube)

        // ----------------------------------------------------------------------------------------------------
        // Create Resource
        // ----------------------------------------------------------------------------------------------------
        member this.CreateTexture() =
            if isCube && fromArray then
                this.CreateTextureFromBitmapArray() // HACK only jpeg
            else                 
                if fileType.EndsWith("dds") then
                    this.CreateTextureFromDDS()
                else
                    this.CreateImage()
                    this.CreateBitmap()
                    this.CreateTextureFromJPG() 

        member this.CreateTextureFromJPG() =
            
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

            this.CreateResource(buffer, 0, bitmap, image)

            resource <- buffer

        member this.CreateTextureFromBitmapArray() =

            let textureDesc = new ResourceDescription(  
                Dimension = ResourceDimension.Texture2D,             
                MipLevels = 1s,
                Format = Format.B8G8R8A8_UNorm,
                Alignment = 0,
                Width = width,
                Height = height,
                DepthOrArraySize = 12s,
                SampleDescription = new SampleDescription(1, 0),
                Layout = TextureLayout.Unknown,
                Flags = ResourceFlags.AllowRenderTarget
               ) 

            let buffer = device.CreateCommittedResource(
                new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0),
                HeapFlags.None,
                textureDesc,
                ResourceStates.GenericRead
             )

            for i in  0..5 do
                this.CreateResource(buffer, i, bitmapArray[i], imageArray[i]) 

            resource <- buffer

        member this.CreateResource(buffer:Resource, i, bmp:System.Drawing.Bitmap, img:byte[]) =
 
            let mutable NumBytes = 0
            let mutable RowBytes = 0
            let mutable NumRows  = 0

            DX12GameProgramming.TextureUtilities.GetSurfaceInfo(bmp.Width, bmp.Height, Format.R8G8B8A8_UNorm, &NumBytes, &RowBytes, &NumRows)

            let handle = GCHandle.Alloc(img, GCHandleType.Pinned) 
            let ptr = Marshal.UnsafeAddrOfPinnedArrayElement(img, 0) 

            buffer.WriteToSubresource(
                i,
                new ResourceRegion(             
                    Back = 1,
                    Bottom = bmp.Height,
                    Right = bmp.Width
                ),
                ptr,
                4 * bmp.Width,
                4 * bmp.Width * bmp.Height
            )
            handle.Free()

