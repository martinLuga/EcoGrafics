namespace GraficBase
//
//  Camera.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open SharpDX.Direct3D12

open DirectX.D3DUtilities

open Base.ShaderSupport

// ----------------------------------------------------------------------------------------------------
//  Shader specifics
// ----------------------------------------------------------------------------------------------------
module ShaderPackage =

    type IShaderPackage =

        abstract member InputLayoutDesc: InputLayoutDescription

        abstract member RootSignatureDesc: RootSignatureDescription

        abstract member VertexShaderDesc: ShaderDescription

        abstract member PixelShaderDesc: ShaderDescription

        abstract member FrameLength: int

        abstract member MatLength: int

        abstract member ItemLength: int

    // ----------------------------------------------------------------------------------------------------
    //  Shader specifics ShaderRenderingCookbook
    // ----------------------------------------------------------------------------------------------------
    open ShaderRenderingCookbook
    open Structures
    open Interface
    open Pipeline
    open Shaders
    type ShaderPackageCB() =        
        let mutable inputLayoutDesc: InputLayoutDescription = inputLayoutDescription
        let mutable rootSignatureDesc: RootSignatureDescription = rootSignatureDesc
        let mutable vertexShaderDesc = vertexShaderDesc
        let mutable pixelShaderDesc = pixelShaderPhongDesc
        let mutable frameLength = D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
        let mutable matLength = D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
        let mutable itemLength = D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()
        let shaderMaterial = shaderMaterial
        let shaderFrame = shaderFrame
        let shaderObject = shaderObject

        interface IShaderPackage with

            member this.InputLayoutDesc = inputLayoutDesc

            member this.RootSignatureDesc = rootSignatureDesc

            member this.VertexShaderDesc = vertexShaderDesc

            member this.PixelShaderDesc = pixelShaderDesc

            member this.FrameLength = frameLength

            member this.MatLength = matLength

            member this.ItemLength = itemLength

    // ----------------------------------------------------------------------------------------------------
    //  Shader specifics ShaderRenderingGameProgramming
    // ----------------------------------------------------------------------------------------------------
    open ShaderGameProgramming
    open Structures
    open Interface
    open Pipeline
    open Shaders
    type ShaderPackageGP() =
        let mutable inputLayoutDesc: InputLayoutDescription = inputLayoutDescription
        let mutable rootSignatureDesc: RootSignatureDescription = rootSignatureDesc
        let mutable vertexShaderDesc = vertexShaderDesc
        let mutable pixelShaderDesc = pixelShaderPhongDesc
        let mutable frameLength = D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
        let mutable matLength = D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
        let mutable itemLength = D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()
        let shaderMaterial = shaderMaterial
        let shaderFrame = shaderFrame
        let shaderObject = shaderObject

        interface IShaderPackage with

            member this.InputLayoutDesc = inputLayoutDesc

            member this.RootSignatureDesc = rootSignatureDesc

            member this.VertexShaderDesc = vertexShaderDesc

            member this.PixelShaderDesc = pixelShaderDesc

            member this.FrameLength = frameLength

            member this.MatLength = matLength

            member this.ItemLength = itemLength