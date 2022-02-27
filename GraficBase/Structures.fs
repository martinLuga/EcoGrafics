namespace GraficBase
//
//  Material.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System.Runtime.InteropServices

open SharpDX
open SharpDX.Mathematics.Interop

// ----------------------------------------------------------------------------------------------------
// Strukturen aus Cookbook  
// ----------------------------------------------------------------------------------------------------
module Structures = 

    [<StructLayout(LayoutKind.Sequential, Pack = 4)>]
    type ObjectConstants =
        struct             
            val mutable World: Matrix                   
            val mutable View: Matrix
            val mutable InvView: Matrix
            val mutable Proj: Matrix
            val mutable InvProj: Matrix
            val mutable ViewProj: Matrix
            val mutable InvViewProj: Matrix
            val mutable WorldViewProjection: Matrix     
            val mutable WorldInverseTranspose: Matrix   
            val mutable ViewProjection: Matrix           
            val mutable EyePosW:Vector3
            new(
                world: Matrix,
                view: Matrix,
                invView: Matrix,
                proj: Matrix,
                invProj: Matrix,
                viewProj: Matrix,
                invViewProj: Matrix,
                worldViewProjection: Matrix,
                worldInverseTranspose: Matrix,
                viewProjection: Matrix,
                eyePosW:Vector3) =
                { 
                    World = world;
                    View = view;
                    InvView = invView;
                    Proj = proj;
                    InvProj = invProj;
                    ViewProj = viewProj;
                    InvViewProj = invViewProj;
                    WorldViewProjection = worldViewProjection;
                    WorldInverseTranspose = worldInverseTranspose;
                    ViewProjection = viewProjection;
                    EyePosW = eyePosW
                } 
            static member Default =
                new ObjectConstants( 
                    World = Matrix.Identity,
                    View = Matrix.Identity,
                    InvView = Matrix.Identity,
                    Proj = Matrix.Identity,
                    InvProj = Matrix.Identity,
                    ViewProj = Matrix.Identity,
                    InvViewProj = Matrix.Identity,
                    WorldViewProjection = Matrix.Identity,
                    WorldInverseTranspose = Matrix.Identity,
                    ViewProjection = Matrix.Identity,
                    EyePosW = Vector3.Zero
                )         
            end


    // Transpose the matrices so that they are in row major order for HLSL
    let Transpose (perObject:ObjectConstants) =
        perObject.View.Transpose()
        perObject.InvView.Transpose()
        perObject.Proj.Transpose()
        perObject.InvProj.Transpose()
        perObject.ViewProj.Transpose()
        perObject.InvViewProj.Transpose()
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