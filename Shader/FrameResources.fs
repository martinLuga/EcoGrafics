namespace Shader
//
//  FrameResources.fs
//
//  Ported from Luna Directx 12 Game programming  to F#
//

open System
open System.Runtime.InteropServices
 
open SharpDX
open SharpDX.Direct3D12
open SharpDX.Mathematics.Interop

open DirectX.D3DUtilities
open DirectX.UploadBuffer

// ----------------------------------------------------------------------------
// Die Puffer für die Übergabe an die Shader
//  Pro Objekt
//  Pro Frame
//  Pro Material
// ----------------------------------------------------------------------------
module FrameResources = 

    type Device = SharpDX.Direct3D12.Device 
    type Resource = SharpDX.Direct3D12.Resource 

    // Für einen zu rendernden Frame
    // Alle Buffer zum Übergeben
    [<AllowNullLiteralAttribute>]
    type FrameResource<'O, 'M, 'P when 'O:struct and 'M: struct and 'P: struct> (device:Device, objectCount:int, materialCount:int) =
        let mutable cmdListAlloc:CommandAllocator = null 
        let mutable objectCB:UploadBuffer<'O> = null
        let mutable materialCB:UploadBuffer<'M> = null
        let mutable passCB:UploadBuffer<'P> = null 
        let mutable fence:int64 = 0L
        
        do
            cmdListAlloc    <-  device.CreateCommandAllocator(CommandListType.Direct)  
            objectCB        <-  new UploadBuffer<'O>(device, objectCount, true)
            materialCB      <-  new UploadBuffer<'M>(device, materialCount, true) 
            passCB          <-  new UploadBuffer<'P>(device, 1, true) 

        interface IDisposable with 
            member this.Dispose() = 
                (objectCB:> IDisposable).Dispose()
                (materialCB:> IDisposable).Dispose()
                (passCB:> IDisposable).Dispose()
                cmdListAlloc.Dispose()

        // We cannot reset the allocator until the GPU is done processing the commands.
        // So each frame needs their own allocator.
        member this.CmdListAlloc  
            with get() = cmdListAlloc

            // We cannot update a cbuffer until the GPU is done processing the commands
            // that reference it. So each frame needs their own cbuffers.
            member this.PassCB
            with get() = passCB
            member this.MaterialCB
            with get() = materialCB
            member this.ObjectCB
            with get() = objectCB

            // Fence value to mark commands up to this fence point.  This lets us
            // check if these frame resources are still in use by the GPU.
            member this.Fence
            with get() = fence
            and set(value) = fence <- value

    // ----------------------------------------------------------------------------
    //  Übergabestrukturen an die shader
    //  Version luna
    // ----------------------------------------------------------------------------
    module LunaBook = 
        [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
        type ObjectConstants = 
            struct 
                val mutable World:Matrix
                val mutable WorldViewProjection: Matrix     // WorldViewProjection matrix
                val mutable TexTransform:Matrix
                new(worldView, worldViewProjection, texTransform) = {World = worldView; WorldViewProjection=worldViewProjection;TexTransform=texTransform}
            end

        // Transpose the matrices so that they are in row major order for HLSL
        let Transpose (perObject:ObjectConstants) =
            perObject.World.Transpose()  
            perObject.WorldViewProjection.Transpose()
            perObject

        [<type:StructLayout(LayoutKind.Sequential, Pack = 4)>]
        type MaterialConstants =
            struct
            val  mutable DiffuseAlbedo:Vector4
            val  mutable FresnelR0:Vector3
            val  mutable Roughness:float32
            val  mutable UVTransform:Matrix            
            val  mutable HasTexture:bool
            new (diffuseAlbedo, fresnelR0,roughness, matTransform, hasTexture) =
                {DiffuseAlbedo=diffuseAlbedo; FresnelR0=fresnelR0; Roughness=roughness; UVTransform=matTransform; HasTexture=hasTexture} 
        end

        let  DefaultMaterialConstants = 
            new MaterialConstants(
                Vector4.One,
                new Vector3(0.01f),
                0.25f,
                Matrix.Identity,
                false
                )

        [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
        [<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
        type FrameConstants= 
            struct 
                val mutable View:Matrix
                val mutable InvView:Matrix
                val mutable Proj:Matrix
                val mutable InvProj:Matrix
                val mutable ViewProj:Matrix
                val mutable InvViewProj:Matrix
                val mutable EyePosW:Vector3
                val mutable PerObjectPad1:single
                val mutable RenderTargetSize:Vector2
                val mutable InvRenderTargetSize:Vector2
                val mutable NearZ:float
                val mutable FarZ:float
                val mutable AmbientLight:Vector4
                // Indices [0, NUM_DIR_LIGHTS) are directional lights:
                // indices [NUM_DIR_LIGHTS, NUM_DIR_LIGHTS+NUM_POINT_LIGHTS) are point lights:
                // indices [NUM_DIR_LIGHTS+NUM_POINT_LIGHTS, NUM_DIR_LIGHTS+NUM_POINT_LIGHT+NUM_SPOT_LIGHTS)
                // are spot lights for a maximum of MaxLights per object.
                [<MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXLIGHTS)>]
                val mutable Lights:Light[] 

                new(view, invView, proj, invProj, viewProj, invViewProj, eyePosW, perObjectPad1, renderTargetSize, invRenderTargetSize, nearZ, farZ, ambientLight, lights) = 
                    {View=view; InvView=invView; Proj=proj; InvProj=invProj; ViewProj=viewProj; InvViewProj=invViewProj; EyePosW=eyePosW; PerObjectPad1= perObjectPad1;
                    RenderTargetSize=renderTargetSize; InvRenderTargetSize=invRenderTargetSize; NearZ=nearZ; FarZ=farZ; AmbientLight=ambientLight; Lights=lights}

                new(view, invView, proj, invProj, viewProj, invViewProj, ambientLight, lights) = 
                    {View=view; InvView=invView; Proj=proj; InvProj=invProj; ViewProj=viewProj; InvViewProj=invViewProj; EyePosW=Vector3.Zero; PerObjectPad1= 0.0f;
                    RenderTargetSize=Vector2.Zero;InvRenderTargetSize=Vector2.Zero;NearZ=0.0; FarZ=0.0; AmbientLight=ambientLight; Lights=lights}
            end

        let DefaultFrameConstants = 
            new FrameConstants(  
                Matrix.Identity,
                Matrix.Identity,
                Matrix.Identity,
                Matrix.Identity,
                Matrix.Identity,
                Matrix.Identity,
                Vector4.UnitW,
                DefaultLightArray
            )

        //  Directional light
        [<StructLayout(LayoutKind.Sequential, Pack = 1)>]
        type DirectionalLight =
            struct  
                val mutable Color: Color4               // 16 bytes
                val mutable Direction: Vector3          // 12 bytes
                val _padding: float32                   // 4 bytes
                new(color,direction) = {Color=color; Direction=direction; _padding = 0.0f}            
                new(color) = DirectionalLight(color,Vector3.Zero)
            end 

        let newFrameResource(device, itemCount, matCount) = 
            new FrameResource<ObjectConstants, MaterialConstants, FrameConstants>(device, itemCount, matCount)

    // ----------------------------------------------------------------------------
    //  Satz von Übergabestrukturen an die shader
    //  Version alt 
    // ----------------------------------------------------------------------------
    module CookBook = 

        [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
        type ObjectConstants =
            struct 
                val mutable WorldViewProjection: Matrix     // WorldViewProjection matrix
                val mutable World : Matrix                  // We need the world matrix so that we can calculate the lighting in world space
                val mutable WorldInverseTranspose: Matrix   // Inverse transpose of World
                val mutable ViewProjection: Matrix          // ViewProjection matrix
                new(worldView, world, worldInverseTranspose, viewProjection) = 
                    {WorldViewProjection = worldView; World = world; WorldInverseTranspose = worldInverseTranspose;ViewProjection=viewProjection}
            end

        // Transpose the matrices so that they are in row major order for HLSL
        let Transpose (perObject:ObjectConstants) =
            perObject.World.Transpose()
            perObject.WorldInverseTranspose.Transpose()  
            perObject.WorldViewProjection.Transpose()
            perObject.ViewProjection.Transpose() 
            perObject

        //  Directional light
        [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
        type DirectionalLight =
            struct  
                val mutable Color: Color4               // 16 bytes
                val mutable Direction: Vector3          // 12 bytes
                val _padding: float32                   // 4 bytes
                new(color,direction) = {Color=color; Direction=direction; _padding = 0.0f}            
                new(color) = DirectionalLight(color,Vector3.Zero)
            end 

        [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
        type MaterialConstants =
            struct   
                val mutable Ambient:  Color4           // 16 bytes       
                val mutable Diffuse:  Color4           // 16 bytes         
                val mutable Specular: Color4           // 16 bytes  
                val mutable SpecularPower: float32     // 4 bytes
                val mutable HasTexture: RawBool        // 4 bytes          
                val mutable _padding0: Vector2         // 8 bytes
                val mutable Emissive: Color4           // 16 bytes 
                val mutable UVTransform: Matrix        // 16 bytes     
                new(ambient, diffuse, specular, specularPower, hasTexture, emissive, uVTransform) = 
                    {Ambient = ambient; Diffuse = diffuse; Specular = specular; SpecularPower=specularPower; HasTexture=hasTexture; _padding0=Vector2.Zero; Emissive=emissive; UVTransform=uVTransform }
             end

        //  Per frame constant buffer (camera position) 
        [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
        type FrameConstants =
            struct  
                val mutable Light: DirectionalLight 
                val mutable CameraPosition:Vector3     
                val mutable TessellationFactor:float32  
                new(light, lightDir, cameraPosition, tessellationFactor) = {Light=light; CameraPosition=cameraPosition; TessellationFactor=tessellationFactor}
            end

        let oldFrameResource(device, itemCount, matCount) = 
            new FrameResource<ObjectConstants, MaterialConstants, FrameConstants>(device, itemCount, matCount)
