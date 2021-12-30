namespace DirectX
//
//  TextureSupportDDS.fs
//
//  Ported from Luna Directx 12 Game programming  to F#
//
open System 
open System.Linq 
open System.Runtime.InteropServices 

open SharpDX.Direct3D12 
open SharpDX.DXGI 

// ---------------------------------------------------------------------------- 
// Custom Loader for texture files.
// Modified to support DX10 extension 
// ----------------------------------------------------------------------------
module TextureSupport = 

    type Device = SharpDX.Direct3D12.Device
    type Resource = SharpDX.Direct3D12.Resource
    type ShaderBytecode = SharpDX.Direct3D12.ShaderBytecode 

    exception ResourceNotFoundException of string
 
    let  DDS_MAGIC:int = 0x20534444 // "DDS "

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type DDS_PIXELFORMAT =
        struct 
        val size:int
        val flags:int
        val fourCC:int
        val RGBBitCount:int
        val RBitMask: UInt32 
        val GBitMask:UInt32 
        val BBitMask:UInt32 
        val ABitMask:UInt32 
    end

    let DDS_FOURCC = 0x00000004     // DDPF_FOURCC
    let DDS_RGB = 0x00000040        // DDPF_RGB
    let DDS_RGBA = 0x00000041       // DDPF_RGB | DDPF_ALPHAPIXELS
    let DDS_LUMINANCE = 0x00020000;// DDPF_LUMINANCE
    let DDS_LUMINANCEA = 0x00020001;// DDPF_LUMINANCE | DDPF_ALPHAPIXELS
    let DDS_ALPHA = 0x00000002;// DDPF_ALPHA
    let DDS_PAL8 = 0x00000020;// DDPF_PALETTEINDEXED8

    let DDS_HEADER_FLAGS_TEXTURE = 0x00001007;// DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT
    let DDS_HEADER_FLAGS_MIPMAP = 0x00020000;// DDSD_MIPMAPCOUNT
    let DDS_HEADER_FLAGS_VOLUME = 0x00800000;// DDSD_DEPTH
    let DDS_HEADER_FLAGS_PITCH = 0x00000008;// DDSD_PITCH
    let DDS_HEADER_FLAGS_LINEARSIZE = 0x00080000;// DDSD_LINEARSIZE

    let DDS_HEIGHT = 0x00000002;// DDSD_HEIGHT
    let DDS_WIDTH = 0x00000004;// DDSD_WIDTH

    let DDS_SURFACE_FLAGS_TEXTURE = 0x00001000;// DDSCAPS_TEXTURE
    let DDS_SURFACE_FLAGS_MIPMAP = 0x00400008;// DDSCAPS_COMPLEX | DDSCAPS_MIPMAP
    let DDS_SURFACE_FLAGS_CUBEMAP = 0x00000008;// DDSCAPS_COMPLEX

    let DDS_CUBEMAP_POSITIVEX = 0x00000600;// DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEX
    let DDS_CUBEMAP_NEGATIVEX = 0x00000a00;// DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEX
    let DDS_CUBEMAP_POSITIVEY = 0x00001200;// DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEY
    let DDS_CUBEMAP_NEGATIVEY = 0x00002200;// DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEY
    let DDS_CUBEMAP_POSITIVEZ = 0x00004200;// DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEZ
    let DDS_CUBEMAP_NEGATIVEZ = 0x00008200;// DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEZ

    let DDS_CUBEMAP_ALLFACES = (DDS_CUBEMAP_POSITIVEX ||| DDS_CUBEMAP_NEGATIVEX ||| DDS_CUBEMAP_POSITIVEY ||| DDS_CUBEMAP_NEGATIVEY ||| DDS_CUBEMAP_POSITIVEZ ||| DDS_CUBEMAP_NEGATIVEZ) 

    let DDS_CUBEMAP = 0x00000200;// DDSCAPS2_CUBEMAP

    let DDS_FLAGS_VOLUME = 0x00200000;// DDSCAPS2_VOLUME

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type DDS_HEADER =
        struct         
            val size:int 
            val flags:int 
            val height:int 
            val width:int 
            val pitchOrLinearSize:int 
            val depth:int  // only if DDS_HEADER_FLAGS_VOLUME is set in flags
            val mipMapCount:int 
            //===11
            [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)>]
            val reserved1:int[]
            val ddspf:DDS_PIXELFORMAT
            val caps:int 
            val caps2:int 
            val caps3:int 
            val caps4:int   
            val reserved2:int
        end

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type DDS_HEADER_DXT10 =
        struct 
            val dxgiFormat:Format 
            val resourceDimension:ResourceDimension
            val miscFlag:int // see D3D11_RESOURCE_MISC_FLAG
            val arraySize:int
            val reserved:int
        end

    let BitsPerPixel(fmt:Format) =
        match fmt with
        | Format.R32G32B32A32_Typeless
        | Format.R32G32B32A32_Float
        | Format.R32G32B32A32_UInt
        | Format.R32G32B32A32_SInt 
            -> 128              

        | Format.R32G32B32_Typeless 
        | Format.R32G32B32_Float 
        | Format.R32G32B32_UInt 
        | Format.R32G32B32_SInt  
            ->96

        | Format.R16G16B16A16_Typeless
        | Format.R16G16B16A16_Float
        | Format.R16G16B16A16_UNorm
        | Format.R16G16B16A16_UInt
        | Format.R16G16B16A16_SNorm
        | Format.R16G16B16A16_SInt
        | Format.R32G32_Typeless
        | Format.R32G32_Float
        | Format.R32G32_UInt
        | Format.R32G32_SInt
        | Format.R32G8X24_Typeless
        | Format.D32_Float_S8X24_UInt
        | Format.R32_Float_X8X24_Typeless
        | Format.X32_Typeless_G8X24_UInt
            ->64

        | Format.R10G10B10A2_Typeless
        | Format.R10G10B10A2_UNorm
        | Format.R10G10B10A2_UInt
        | Format.R11G11B10_Float
        | Format.R8G8B8A8_Typeless
        | Format.R8G8B8A8_UNorm
        | Format.R8G8B8A8_UNorm_SRgb
        | Format.R8G8B8A8_UInt
        | Format.R8G8B8A8_SNorm
        | Format.R8G8B8A8_SInt
        | Format.R16G16_Typeless
        | Format.R16G16_Float
        | Format.R16G16_UNorm
        | Format.R16G16_UInt
        | Format.R16G16_SNorm
        | Format.R16G16_SInt
        | Format.R32_Typeless
        | Format.D32_Float
        | Format.R32_Float
        | Format.R32_UInt
        | Format.R32_SInt
        | Format.R24G8_Typeless
        | Format.D24_UNorm_S8_UInt
        | Format.R24_UNorm_X8_Typeless
        | Format.X24_Typeless_G8_UInt
        | Format.R9G9B9E5_Sharedexp
        | Format.R8G8_B8G8_UNorm
        | Format.G8R8_G8B8_UNorm
        | Format.B8G8R8A8_UNorm
        | Format.B8G8R8X8_UNorm
        | Format.R10G10B10_Xr_Bias_A2_UNorm
        | Format.B8G8R8A8_Typeless
        | Format.B8G8R8A8_UNorm_SRgb
        | Format.B8G8R8X8_Typeless
        | Format.B8G8R8X8_UNorm_SRgb
            ->32 

        | Format.R8G8_Typeless
        | Format.R8G8_UNorm
        | Format.R8G8_UInt
        | Format.R8G8_SNorm
        | Format.R8G8_SInt
        | Format.R16_Typeless
        | Format.R16_Float
        | Format.D16_UNorm
        | Format.R16_UNorm
        | Format.R16_UInt
        | Format.R16_SNorm
        | Format.R16_SInt
        | Format.B5G6R5_UNorm
        | Format.B5G5R5A1_UNorm
        | Format.B4G4R4A4_UNorm
            ->16 

        | Format.R8_Typeless
        | Format.R8_UNorm
        | Format.R8_UInt
        | Format.R8_SNorm
        | Format.R8_SInt
        | Format.A8_UNorm
            ->8 

        | Format.R1_UNorm
            ->1 

        | Format.BC1_Typeless
        | Format.BC1_UNorm
        | Format.BC1_UNorm_SRgb
        | Format.BC4_Typeless
        | Format.BC4_UNorm
        | Format.BC4_SNorm
            ->4 

        | Format.BC2_Typeless
        | Format.BC2_UNorm
        | Format.BC2_UNorm_SRgb
        | Format.BC3_Typeless
        | Format.BC3_UNorm
        | Format.BC3_UNorm_SRgb
        | Format.BC5_Typeless
        | Format.BC5_UNorm
        | Format.BC5_SNorm
        | Format.BC6H_Typeless
        | Format.BC6H_Uf16
        | Format.BC6H_Sf16
        | Format.BC7_Typeless
        | Format.BC7_UNorm
        | Format.BC7_UNorm_SRgb
            ->8

        | _  ->0

    //--------------------------------------------------------------------------------------
    // Get surface information for a particular format
    //--------------------------------------------------------------------------------------
    let GetSurfaceInfo(width:int, height:int, fmt:Format) =

        let mutable outNumBytes = 0
        let mutable outRowBytes = 0
        let mutable outNumRows = 0
 
        let mutable numBytes = 0 
        let mutable rowBytes = 0 
        let mutable numRows = 0 

        let mutable bc = false 
        let mutable packed = false 
        let mutable bcnumBytesPerBlock = 0 

        match fmt with 
        | Format.BC1_Typeless
        | Format.BC1_UNorm
        | Format.BC1_UNorm_SRgb
        | Format.BC4_Typeless
        | Format.BC4_UNorm
        | Format.BC4_SNorm ->
            bc <- true 
            bcnumBytesPerBlock <- 8 

        | Format.BC2_Typeless
        | Format.BC2_UNorm
        | Format.BC2_UNorm_SRgb
        | Format.BC3_Typeless
        | Format.BC3_UNorm
        | Format.BC3_UNorm_SRgb
        | Format.BC5_Typeless
        | Format.BC5_UNorm
        | Format.BC5_SNorm
        | Format.BC6H_Typeless
        | Format.BC6H_Uf16
        | Format.BC6H_Sf16
        | Format.BC7_Typeless
        | Format.BC7_UNorm
        | Format.BC7_UNorm_SRgb ->
            bc <- true 
            bcnumBytesPerBlock <- 16  

        | Format.R8G8_B8G8_UNorm
        | Format.G8R8_G8B8_UNorm ->
            packed <- true 

        // TODO
        | _ ->
            packed <- false
            bc <- false 
            bcnumBytesPerBlock <- 0 

        if (bc) then 
            let mutable numBlocksWide = 0 
            if (width > 0) then 
                numBlocksWide <- Math.Max(1, (width + 3) / 4);
 
            let mutable numBlocksHigh = 0 
            if (height > 0)then
                    numBlocksHigh <- Math.Max(1, (height + 3) / 4);

            rowBytes <- numBlocksWide * bcnumBytesPerBlock;
            numRows  <- numBlocksHigh;
 
        else if (packed) then
            rowBytes <- ((width + 1) >>> 1) * 4 
            numRows <- height 
        else
            let mutable bpp = BitsPerPixel(fmt) 
            rowBytes <- (width * bpp + 7) / 8  // round up to nearest byte
            numRows <- height 

        numBytes <- rowBytes * numRows;

        outNumBytes <- numBytes 
        outRowBytes  <-  rowBytes 
        outNumRows <-  numRows 

        (outNumBytes, outRowBytes, outNumRows)


    let ISBITMASK(ddpf:DDS_PIXELFORMAT, r:UInt32, g:UInt32, b:UInt32, a:UInt32) = 
        (ddpf.RBitMask = r && ddpf.GBitMask = g && ddpf.BBitMask = b && ddpf.ABitMask = a)  

    let MAKEFOURCC(ch0:char, ch1:char, ch2:char, ch3:char) = 
        let result = (int ch0 ||| (int ch1 <<< 8 )||| (int ch2  <<< 16) ||| (int ch3 <<< 24)) 
        result 

    let GetDXGIFormat(ddpf:DDS_PIXELFORMAT) =
        if ((ddpf.flags &&& DDS_RGB) > 0) then
            // Note that sRGB formats are written using the "DX10" extended header
            match ddpf.RGBBitCount with
                
            | 32 ->
                if (ISBITMASK(ddpf, uint32 0x000000ff, uint32 0x0000ff00, uint32 0x00ff0000, uint32 0xff000000)) then 
                    Format.R8G8B8A8_UNorm  

                else if (ISBITMASK(ddpf, uint32 0x00ff0000, uint32 0x0000ff00, uint32 0x000000ff, uint32 0xff000000)) then 
                    Format.B8G8R8A8_UNorm 

                else if (ISBITMASK(ddpf, uint32 0x00ff0000, uint32 0x0000ff00, uint32 0x000000ff, uint32 0x00000000)) then 
                    Format.B8G8R8X8_UNorm;

                // No DXGI format maps to ISBITMASK(0x000000ff, 0x0000ff00, 0x00ff0000, 0x00000000) aka D3DFMT_X8B8G8R8

                // Note that many common DDS reader/writers (including D3DX) swap the
                // the RED/BLUE masks for 10:10:10:2 formats. We assumme
                // below that the 'backwards' header mask is being used since it is most
                // likely written by D3DX. The more robust solution is to use the 'DX10'
                // header extension and specify the DXGI_FORMAT_R10G10B10A2_UNORM format directly

                // For 'correct' writers, this should be 0x000003ff, 0x000ffc00, 0x3ff00000 for RGB data
                else if (ISBITMASK(ddpf, uint32 0x3ff00000, uint32 0x000ffc00, uint32 0x000003ff, uint32 0xc0000000)) then  
                        Format.R10G10B10A2_UNorm  

                // No DXGI format maps to ISBITMASK(0x000003ff, 0x000ffc00, 0x3ff00000, 0xc0000000) aka D3DFMT_A2R10G10B10

                else if (ISBITMASK(ddpf, uint32 0x0000ffff, uint32 0xffff0000, uint32 0x00000000, uint32 0x00000000)) then 
                    Format.R16G16_UNorm

                else if (ISBITMASK(ddpf, uint32 0xffffffff,uint32 0x00000000, uint32 0x00000000, uint32  0x00000000)) then  
                    // Only 32-bit color channel format in D3D9 was R32F
                    Format.R32_Float; // D3DX writes this out as a FourCC of 114
                    
                else Format.Unknown


            | 24 ->
                    // No 24bpp DXGI formats aka D3DFMT_R8G8B8
                    Format.Unknown

            | 16 ->
                    if (ISBITMASK(ddpf, uint32 0x7c00, uint32 0x03e0, uint32 0x001f, uint32 0x8000)) then 
                        Format.B5G5R5A1_UNorm

                    else if (ISBITMASK(ddpf, uint32 0xf800, uint32 0x07e0, uint32 0x001f, uint32 0x0000)) then  
                        Format.B5G6R5_UNorm

                    // No DXGI format maps to ISBITMASK(0x7c00, 0x03e0, 0x001f, 0x0000) aka D3DFMT_X1R5G5B5
                    else if (ISBITMASK(ddpf, uint32 0x0f00, uint32 0x00f0, uint32 0x000f, uint32 0xf000)) then  
                        Format.B4G4R4A4_UNorm  

                    // No DXGI format maps to ISBITMASK(0x0f00, 0x00f0, 0x000f, 0x0000) aka D3DFMT_X4R4G4B4

                    // No 3:3:2, 3:3:2:8, or paletted DXGI formats aka D3DFMT_A8R3G3B2, D3DFMT_R3G3B2, D3DFMT_P8, D3DFMT_A8P8, etc.
                    else Format.Unknown

            | _ ->  Format.Unknown
 
        else if ((ddpf.flags &&& DDS_LUMINANCE) > 0) then 
            if (8 = ddpf.RGBBitCount) then                
                if (ISBITMASK(ddpf, uint32 0x000000ff, uint32 0x00000000, uint32 0x00000000, uint32 0x00000000)) then                    
                    Format.R8_UNorm; // D3DX10/11 writes this out as DX10 extension
                else 
                    Format.Unknown   
                    // No DXGI format maps to ISBITMASK(0x0f, 0x00, 0x00, 0xf0) aka D3DFMT_A4L4                

            else if (16 = ddpf.RGBBitCount) then                
                if (ISBITMASK(ddpf, uint32 0x0000ffff, uint32 0x00000000, uint32 0x00000000, uint32 0x00000000)) then                  
                    Format.R16_UNorm // D3DX10/11 writes this out as DX10 extension
                    
                else if (ISBITMASK(ddpf, uint32 0x000000ff, uint32 0x00000000, uint32 0x00000000, uint32 0x0000ff00)) then                    
                    Format.R8G8_UNorm; // D3DX10/11 writes this out as DX10 extension
                else 
                    Format.Unknown  
            else 
                Format.Unknown  
            
        else if ((ddpf.flags &&& DDS_ALPHA) > 0) then           
            if (8 = ddpf.RGBBitCount) then               
                Format.A8_UNorm
            else 
                Format.Unknown                  
            
        else if ((ddpf.flags &&& DDS_FOURCC) > 0) then
            if (MAKEFOURCC('D', 'X', 'T', '1') = ddpf.fourCC) then
                Format.BC1_UNorm                
            else if (MAKEFOURCC('D', 'X', 'T', '3') = ddpf.fourCC) then
                Format.BC2_UNorm                
            else if (MAKEFOURCC('D', 'X', 'T', '5') = ddpf.fourCC) then               
                Format.BC3_UNorm
            // While pre-mulitplied alpha isn't directly supported by the DXGI formats,
            // they are basically the same as these BC formats so they can be mapped
            else if (MAKEFOURCC('D', 'X', 'T', '2') = ddpf.fourCC) then               
                Format.BC2_UNorm                
            else if (MAKEFOURCC('D', 'X', 'T', '4') = ddpf.fourCC) then               
                Format.BC3_UNorm               

            else if (MAKEFOURCC('A', 'T', 'I', '1') = ddpf.fourCC) then               
                Format.BC4_UNorm                
            else if (MAKEFOURCC('B', 'C', '4', 'U') = ddpf.fourCC) then               
                Format.BC4_UNorm                
            else if (MAKEFOURCC('B', 'C', '4', 'S') = ddpf.fourCC) then               
                Format.BC4_SNorm 
            else if (MAKEFOURCC('A', 'T', 'I', '2') = ddpf.fourCC) then               
                Format.BC5_UNorm
            else if (MAKEFOURCC('B', 'C', '5', 'U') = ddpf.fourCC) then               
                Format.BC5_UNorm
            else if (MAKEFOURCC('B', 'C', '5', 'S') = ddpf.fourCC) then               
                Format.BC5_SNorm
            // BC6H and BC7 are written using the "DX10" extended header
            else if (MAKEFOURCC('R', 'G', 'B', 'G') = ddpf.fourCC) then               
                Format.R8G8_B8G8_UNorm                
            else if (MAKEFOURCC('G', 'R', 'G', 'B') = ddpf.fourCC) then               
                Format.G8R8_G8B8_UNorm
            else 
                // Check for D3DFORMAT enums being set here
                match ddpf.fourCC with               
                | 36  -> // D3DFMT_A16B16G16R16
                    Format.R16G16B16A16_UNorm;

                | 110  -> // D3DFMT_Q16W16V16U16
                    Format.R16G16B16A16_SNorm;

                | 111 -> // D3DFMT_R16F
                    Format.R16_Float 

                | 112  -> // D3DFMT_G16R16F
                    Format.R16G16_Float 

                | 113 -> // D3DFMT_A16B16G16R16F
                    Format.R16G16B16A16_Float;

                | 114 -> // D3DFMT_R32F
                    Format.R32_Float 

                | 115 ->  // D3DFMT_G32R32F
                    Format.R32G32_Float 

                | 116 ->  // D3DFMT_A32B32G32R32F
                    Format.R32G32B32A32_Float 
                
                | _ ->
                    Format.Unknown 
        else 
            Format.Unknown           
    //--------------------------------------------------------------------------------------
    // Bytes auf eine Struktur kopieren
    //--------------------------------------------------------------------------------------
    let ByteArrayToStructure<'T> (bytes:byte[], start:int, count:int ): 'T = 
        let temp = bytes.Skip(start).Take(count).ToArray() 
        let handle = GCHandle.Alloc(temp, GCHandleType.Pinned) 
        let stuff:'T =  Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof<'T>) :?> 'T
        handle.Free()
        stuff

    //--------------------------------------------------------------------------------------
    //  
    //--------------------------------------------------------------------------------------
    let FillInitData(texture:Resource, width:int, height:int, depth:int, mipCount:int, arraySize:int, format:Format, maxsize:int, bitSize:int, bitData:byte[] , offset:int) =
        let NumBytes = 0 
        let RowBytes = 0 
        let NumRows = 0 
        let pSrcBits = bitData 
        let pEndBits = bitData    // + bitSize 

        let mutable index = 0  
        let mutable k = offset 

        for j = 0 to arraySize-1 do           
            let mutable w = width 
            let mutable h = height 
            let mutable d = depth 
            for i = 0 to mipCount-1 do               
                let result = GetSurfaceInfo(w, h, format)
                let (NumBytes,   RowBytes,   NumRows) = result
                let handle = GCHandle.Alloc(bitData, GCHandleType.Pinned) 
                let ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bitData, k) 
                texture.WriteToSubresource(index, Nullable<ResourceRegion>(), ptr, RowBytes, NumBytes) 
                handle.Free() 
                index <- index + 1
                k <- k + NumBytes * d

                w <- w >>> 1
                h <- h >>> 1
                d <- d >>> 1

                if (w = 0) then
                    w <- 1
                    
                if (h = 0) then
                    h <- 1 
                    
                if (d = 0) then
                    d <- 1

    //--------------------------------------------------------------------------------------
    // Erzeuge Textur aus DDS
    //--------------------------------------------------------------------------------------
    let CreateTextureFromDDS(d3dDevice:Device, header:DDS_HEADER, header10:DDS_HEADER_DXT10 Nullable , bitData:byte[] , offset:int, maxsize:int) =      
        let mutable isCubeMap = false
        let mutable width = header.width 
        let mutable height = header.height 
        let mutable depth = header.depth 

        let mutable resDim = ResourceDimension.Unknown 
        let mutable arraySize = 1 
        let mutable format = Format.Unknown;
        isCubeMap <- false 

        let mutable mipCount = header.mipMapCount 
        if (0 =  mipCount) then           
            mipCount <- 1            

        if (((header.ddspf.flags &&& DDS_FOURCC) > 0) && (MAKEFOURCC('D', 'X', '1', '0') = header.ddspf.fourCC)) then  
            let d3d10ext:DDS_HEADER_DXT10 = header10.GetValueOrDefault() 
            arraySize <- d3d10ext.arraySize
            if arraySize = 0 then               
                raise (System.ArgumentException("Arraysize cannot be 0!"))              

            if (BitsPerPixel(d3d10ext.dxgiFormat) = 0) then                 
                raise (System.ArgumentException("DXGI Format cannot be 0!"))                 

            format <- d3d10ext.dxgiFormat 

            match d3d10ext.resourceDimension with
               
            | ResourceDimension.Texture1D ->
                // D3DX writes 1D textures with a fixed Height of 1
                if ((header.flags &&& DDS_HEIGHT) > 0 && height <> 1) then                       
                    raise (System.ArgumentException("Height must be 1!"))                         
                height <- 1 
                depth  <- 1 

            | ResourceDimension.Texture2D ->
                //D3D11_RESOURCE_MISC_TEXTURECUBE
                if ((d3d10ext.miscFlag &&& 0x4) > 0) then                       
                    arraySize <- arraySize * 6
                    isCubeMap <- true                         
                depth <- 1 

            | ResourceDimension.Texture3D ->
                if not ((header.flags &&& DDS_HEADER_FLAGS_VOLUME) > 0)  then                       
                    raise (System.ArgumentException("Tex 3D volume must be > 0!"))                           

                if (arraySize > 1) then                       
                    raise (System.ArgumentException("Arraysize cannot be > 1!")) 

            | _ ->
                    raise (System.ArgumentException("Arraysize cannot be > 1!"))               

            resDim <- d3d10ext.resourceDimension 
            
        else           
            format <- GetDXGIFormat(header.ddspf) 
            if (format = Format.Unknown) then               
                raise (System.ArgumentException("Format unknown!"))              

            if ((header.flags &&& DDS_HEADER_FLAGS_VOLUME) > 0) then                
                resDim <- ResourceDimension.Texture3D 
                
            else               
                if ((header.caps2 &&& DDS_CUBEMAP) > 0) then                    
                    // We require all six faces to be defined
                    if ((header.caps2 &&& DDS_CUBEMAP_ALLFACES) <> DDS_CUBEMAP_ALLFACES) then                        
                        raise (System.ArgumentException("All six faces must be defined!"))                      

                    arraySize <- 6 
                    isCubeMap <- true                     

                depth <- 1 
                resDim <- ResourceDimension.Texture2D                
            
        let resource = 
            d3dDevice.CreateCommittedResource(
                new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0),
                HeapFlags.None,
                new ResourceDescription( 
                    //Alignment = -1,
                    Dimension = resDim,
                    DepthOrArraySize = int16 arraySize,
                    Flags = ResourceFlags.None,
                    Format = format,
                    Height = height,
                    Layout = TextureLayout.Unknown,
                    MipLevels = int16 mipCount,
                    SampleDescription = new SampleDescription(1, 0),
                    Width = int64 width
                ),
                ResourceStates.GenericRead) 

        FillInitData(resource, width, height, depth, mipCount, arraySize, format, 0, 0, bitData, offset) 

        (resource , isCubeMap)
        
    //--------------------------------------------------------------------------------------
    // Load Texture from memory
    //--------------------------------------------------------------------------------------
    // <param name="device">Device manager</param>
    // <param name="data">Data</param>
    // <param name="isCubeMap">Is cubemap</param>
    // <returns>Resource</returns>
    //--------------------------------------------------------------------------------------
    let CreateTextureFromDDS_1(device:Device, data:byte[]) =

        let mutable isCubeMap = false
       
        // Validate DDS file in memory
        let mutable header = new DDS_HEADER() 

        let ddsHeaderSize = Marshal.SizeOf(header);
        let ddspfSize = Marshal.SizeOf(new DDS_PIXELFORMAT());
        let ddsHeader10Size = Marshal.SizeOf(new DDS_HEADER_DXT10()) 

        if (data.Length < (sizeof<UInt16> + ddsHeaderSize)) then           
            raise (System.ArgumentException("Incorrect length!"))

        //first is magic number
        let dwMagicNumber = BitConverter.ToInt32(data, 0) 
        if (dwMagicNumber <> DDS_MAGIC) then         
            raise (System.ArgumentException("Incorrect length!"))            

        header <- ByteArrayToStructure<DDS_HEADER>(data, 4, ddsHeaderSize) 

        // Verify header to validate DDS file
        if (header.size <> ddsHeaderSize ||  header.ddspf.size <> ddspfSize) then          
            raise (System.ArgumentException("Incorrect header length!"))            

        // Check for DX10 extension
        let mutable dx10Header:DDS_HEADER_DXT10 Nullable = Nullable(DDS_HEADER_DXT10())
        if (((header.ddspf.flags &&& DDS_FOURCC) > 0) && (MAKEFOURCC('D', 'X', '1', '0') = header.ddspf.fourCC)) then           
            // Must be long enough for both headers and magic value
            if (data.Length < (ddsHeaderSize + 4 + ddsHeader10Size)) then               
                raise (System.ArgumentException("Incorrect header length!"))                

            dx10Header <- Nullable(ByteArrayToStructure<DDS_HEADER_DXT10>(data, 4 + ddsHeaderSize, ddsHeader10Size))
            

        let offset = 4 + ddsHeaderSize + (if dx10Header.HasValue then ddsHeader10Size else 0) 

        CreateTextureFromDDS(device, header, dx10Header, data, offset, 0 ) 

    //--------------------------------------------------------------------------------------
    // Load texture from DDS file
    //--------------------------------------------------------------------------------------
    // <param name="device">Device</param>
    // <param name="filename">Filename</param>
    // <returns></returns>
    //--------------------------------------------------------------------------------------
    let CreateTextureFromDDS_2(device:Device, filename:string) =
        let isCube = false 
        let texture = CreateTextureFromDDS_1(device, System.IO.File.ReadAllBytes(filename))
        (fst texture, isCube)        

    //--------------------------------------------------------------------------------------
    //  Create texture from bmp
    //--------------------------------------------------------------------------------------
    //  <param name="device">Device</param>
    //  <param name="filename">Filename</param>
    //  <returns></returns>
    //--------------------------------------------------------------------------------------
    let CreateTextureFromBitmap(device:Device, filename:string) = 
    
        let mutable bitmap:System.Drawing.Bitmap = null 

        try 
            bitmap <- new System.Drawing.Bitmap(filename) 
        with :? System.ArgumentException  ->  raise (ResourceNotFoundException ("Texture " + filename))

        let width = bitmap.Width 
        let height = bitmap.Height
        
        let data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb) 

        let resource = 
            device.CreateCommittedResource(
                new HeapProperties(CpuPageProperty.WriteBack, MemoryPool.L0),
                HeapFlags.None,
                new ResourceDescription( 
                    //Alignment = -1,
                    Dimension = ResourceDimension.Texture2D,
                    DepthOrArraySize = int16 6,
                    Flags = ResourceFlags.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Height = height,
                    Layout = TextureLayout.Unknown,
                    MipLevels = int16 1,
                    SampleDescription = new SampleDescription(1, 0),
                    Width = int64 width
                ),
                ResourceStates.GenericRead) 

        resource.WriteToSubresource(
            0,
            Nullable( 
                new ResourceRegion(
                    Back = 1,
                    Bottom = height,
                    Right = width
                )
            ),
            data.Scan0,
            4 * width, 4 * width * height
        )  
        let bufferSize = data.Height * data.Stride 
        bitmap.UnlockBits(data) 

        resource