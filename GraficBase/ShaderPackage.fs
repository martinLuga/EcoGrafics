namespace GraficBase
//
//  Camera.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open SharpDX.Direct3D12

open DirectX.D3DUtilities

open ShaderRenderingCookbook
open Structures
open Interface

// ----------------------------------------------------------------------------------------------------
//  Shader specifics
// ----------------------------------------------------------------------------------------------------
module ShaderPackage = 

    type ShaderPackage() =
        let mutable inputLayoutDesc:InputLayoutDescription = Pipeline.inputLayoutDescription
        let mutable rootSignatureDesc:RootSignatureDescription = Pipeline.rootSignatureDesc
        let mutable vertexShaderDesc = Shaders.vertexShaderDesc
        let mutable pixelShaderDesc  = Shaders.pixelShaderPhongDesc
        let mutable frameLength  = D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
        let mutable matLength = D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
        let mutable itemLength = D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()
        let shaderMaterial = shaderMaterial
        let shaderFrame = shaderFrame
        let shaderObject = shaderObject
        
        member this.InputLayoutDesc
            with get() = inputLayoutDesc

        member this.RootSignatureDesc
            with get() = rootSignatureDesc
        
        member this.VertexShaderDesc
            with get() = vertexShaderDesc
        
        member this.PixelShaderDesc
            with get() = pixelShaderDesc
        
        member this.FrameLength
         with get() = frameLength 
        
        member this.MatLength
            with get() = matLength 
        
        member this.ItemLength
            with get() = itemLength 

        member this.ShaderMaterial
            with get() = shaderMaterial 

        member this.ShaderFrame
            with get() = shaderFrame 

        member this.ShaderObject
            with get() = shaderObject 