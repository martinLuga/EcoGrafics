namespace DirectX
//
//  CameraAndView.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System

open System.Runtime.InteropServices

open SharpDX
open SharpDX.Direct3D12
open SharpDX.IO
open SharpDX.WIC
open SharpDX.DXGI

open TextureSupport

type Device = SharpDX.Direct3D12.Device
type Resource = SharpDX.Direct3D12.Resource

// ----------------------------------------------------------------------------------------------------
// Material
// ----------------------------------------------------------------------------------------------------
module SurfaceAndMaterial =

    let CopyData(resource:Resource, elementIndex:int , elementByteSize:int, sourceptr:nativeint) = 
        let resourcePointer = resource.Map(0)
        let pointer = IntPtr.Add(resourcePointer, elementIndex * elementByteSize)
        Marshal.WriteIntPtr(sourceptr, pointer)

    let makeDecoder fileName factory = 
        let fileStream = new NativeFileStream(fileName, NativeFileMode.Open, NativeFileAccess.Read)
        let bitmapDecoder = new BitmapDecoder(factory, fileStream, DecodeOptions.CacheOnDemand)
        bitmapDecoder

    let makeFormatConverter (factory:ImagingFactory) (bitmapDecoder:BitmapDecoder) =
        let formatConverter = new FormatConverter(factory)
        formatConverter.Initialize(
            bitmapDecoder.GetFrame(0),
            SharpDX.WIC.PixelFormat.Format32bppPRGBA,   
            SharpDX.WIC.BitmapDitherType.None, 
            null,
            0.0, 
            SharpDX.WIC.BitmapPaletteType.Custom)
        let CanConvert = formatConverter.CanConvert(PixelFormat.Format8bppIndexed, PixelFormat.Format32bppPRGBA)
        formatConverter
    
    let LoadBitmap fileName factory = 
        let decoder = makeDecoder fileName factory
        let converter = makeFormatConverter factory decoder 
        converter   
        
    let CreateTexture2DFromBitmap (device:Device) (bitmapSource:BitmapSource ) =
        let stride = bitmapSource.Size.Width * 4;
        let stream = new SharpDX.DataStream(bitmapSource.Size.Height * stride, true, true)
        let buffer =  DataPointer(stream.DataPointer,bitmapSource.Size.Height * stride )
        bitmapSource.CopyPixels(stride, buffer)

        let textureDesc = ResourceDescription.Texture2D(Format.R8G8B8A8_UNorm, (int64) bitmapSource.Size.Width, bitmapSource.Size.Height)
        let resource = 
            device.CreateCommittedResource(
                new HeapProperties(HeapType.Default),
                HeapFlags.None,
                textureDesc,
                ResourceStates.CopyDestination
            )

        let resourcePointer = resource.Map(0)

        CopyData(resource, 0 , buffer.Size, buffer.Pointer) 

        resource

    let CreateTexture2D(device, fileName) =
       let factory = new ImagingFactory()
       let bitmap = LoadBitmap fileName factory 
       let texture = CreateTexture2DFromBitmap device bitmap
       texture