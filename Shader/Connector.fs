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
open System.Reflection

open DirectX.Assets

// ----------------------------------------------------------------------------------------------------
//  
// ----------------------------------------------------------------------------------------------------  

module Connector = 

    //float3 PosL    : POSITION;
    //float3 NormalL : NORMAL;
    //float2 TexC    : TEXCOORD;

    let toHlslType (typ) =
        match typ with
        |  "Vector3" -> "float3"
        |  "Vector4" -> "float4"
        |  "Vector2" -> "float2"
        |  _ -> raise (new Exception("Parameter typ not recognized"))
        

    type Parameter(typ, name, semantic) =
        let mutable typ = typ
        let mutable name = name 
        let mutable semantic = semantic 
        let mutable idx = 0

        member this.AsHlslElement() = 
            toHlslType(typ) + "     " + name + ";"

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

    type Constant (name:string, struktur: Object) =
        inherit Block(0, 0)        
        let mutable struktur = struktur
        let typ = struktur.GetType()
        let fields:FieldInfo[] = struktur.GetType().GetFields()

        let asShaderString(fields:FieldInfo[]) =
            let mutable result = ""
            fields |> Array.iter (fun field -> 
                result <- result + "\n      " + (sprintf "%s  %s;"  field.FieldType.Name field.Name)
            )
            result

        member this.AsShaderStruct() =
            "
                struct " + name + "
                {
            "
            
            + asShaderString(fields)
            
            +
                "
                };                
                "

        member this.AsInputLayout() = 
             new RootParameter(ShaderVisibility.All, new RootDescriptor(0, 0), RootParameterType.ConstantBufferView)

        override this.AsRootParameter() = 
            new RootParameter(ShaderVisibility.All, new RootDescriptor(0, 0), RootParameterType.ConstantBufferView)

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

