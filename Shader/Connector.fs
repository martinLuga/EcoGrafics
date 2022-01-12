namespace Shader
//
//  ShaderCompile.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open log4net

open SharpDX
open SharpDX.D3DCompiler
open SharpDX.DXGI
open SharpDX.Direct3D12

open System
open System.IO

open Base.FileSupport
open Base.ShaderSupport

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
 
    type Block() =
         abstract member AsRootParameter:Unit->RootParameter 
         default this.AsRootParameter() = 
            new RootParameter()

    type Constant () =
        inherit Block()
        override this.AsRootParameter() = 
            new RootParameter(ShaderVisibility.All, new RootDescriptor(0, 0), RootParameterType.ConstantBufferView)

    type Connector() =

        let mutable blocks:Block list = [] 

