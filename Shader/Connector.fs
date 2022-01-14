namespace Shader
//
//  ShaderCompile.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX.DXGI
open SharpDX.Direct3D12

open System

open DirectX.Assets

// ----------------------------------------------------------------------------------------------------
//  
// ----------------------------------------------------------------------------------------------------  

module Connector = 

    type Parameter() =
        let mutable name = "" 
        let mutable idx = 0
        let mutable typ:Type = float32.GetType()

        member this.AsInputElement() = 
            new InputElement(name, idx, Format.R32G32B32_Float, 0, 0);
 
    type Block(idx,  register) =
        let mutable idx = 0
        let mutable register = 0
        abstract member AsRootParameter:Unit->RootParameter 
        
        default this.AsRootParameter() = 
            new RootParameter()
        
        member this.Idx 
                with get() = idx 
        member this.Register
            with get() =register 

    type Constant (idx, register) =
        inherit Block(idx, register)

        override this.AsRootParameter() = 
            new RootParameter(ShaderVisibility.All, new RootDescriptor(idx, register), RootParameterType.ConstantBufferView)

    type Table ( ) =
        inherit Block(0,  0)

        override this.AsRootParameter() = 
            let table = DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 0) 
            new RootParameter(ShaderVisibility.All, table) 

    type Connector() =

        let mutable blocks:Block list = [] 

        member this.AddConstant(constant) =
            blocks <- blocks @ [constant]

        member this.GetRootSignatureDesc() =
            let slotRootParameters = blocks |> Seq.map (fun b -> b.AsRootParameter()) |> Seq.toArray
            new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout, slotRootParameters, GetStaticSamplers())  

