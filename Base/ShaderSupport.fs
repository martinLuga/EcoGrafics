namespace Base
//
//  ShaderSupport.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX.Direct3D12

// ----------------------------------------------------------------------------------------------------
// Hilfs-klassen für Shaders  
// ----------------------------------------------------------------------------------------------------
module ShaderSupport = 

    type TopologyType   = | Triangle    | Patch         |  Line         | Undefinded
    type RasterType     = | Solid       | Wired         |  Undefinded
    type BlendType      = | Opaque      | Transparent   |  Undefinded
    type ShaderType     = | Vertex = 0  | Pixel = 1     |  Domain = 2   | Hull = 3  | Undefinded = 99

    type ShaderClass  = 
        | SimpleVSType  = 1     | TesselatedVSType = 2
        | SimplePSType  = 3     | LambertPSType   = 4  | PhongPSType =5  | BlinnPhongPSType =6 
        | TriDSType =7          | QuadDSType =8 
        | TriHSType =9          | QuadHSType =10 
        | NotSet =11 

    let forAllVertexShadersDo(nextLoop:ShaderClass->unit) =
        let allvertexShaderTypes = seq {ShaderClass.SimpleVSType; ShaderClass.TesselatedVSType}
        for vertexShadr in allvertexShaderTypes do
            nextLoop vertexShadr 

    let allPixelShadersDo (vertex:ShaderClass)  (nextLoop:ShaderClass->ShaderClass->unit ) =
        let allPixelShaderTypes = seq { ShaderClass.SimplePSType; ShaderClass.LambertPSType; ShaderClass.PhongPSType; ShaderClass.BlinnPhongPSType}
        for pixelShader in allPixelShaderTypes do
        nextLoop vertex pixelShader

    [<AllowNullLiteral>] 
    type ShaderDescription =
        val Klass:ShaderClass
        val Directory:string 
        val File:string
        val Entry:string
        val Mode:string
        new (klass, directory, file, entry, mode) = {Klass=klass; Directory=directory; File=file; Entry=entry; Mode=mode}
        new () = {Klass=ShaderClass.NotSet; Directory=""; File=""; Entry=""; Mode=""}
        override this.ToString() = this.Klass.ToString()
        member self.IsEmpty() = self.Klass=ShaderClass.NotSet
        member self.asFileInfo = (self.Directory, self.File, self.Entry, self.Mode)

    [<AllowNullLiteral>] 
    type BlendDescription=
        val Type:BlendType
        val Description:BlendStateDescription
        new (typ, description) = {Type=typ; Description=description}
        override this.ToString() = this.Type.ToString()

    [<AllowNullLiteral>] 
    type RasterizerDescription=
        val Type:RasterType
        val Description:RasterizerStateDescription
        new (typ, description) = {Type=typ; Description=description}
        override this.ToString() = this.Type.ToString()
    
    [<AllowNullLiteral>] 
    type TopologyTypeDescription=
        val Type:TopologyType
        val Description:PrimitiveTopologyType
        new (typ, description) = {Type=typ; Description=description}
        override this.ToString() = this.Type.ToString()

    // ----------------------------------------------------------------------------------------------------
    // ShaderConfiguration  
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>] 
    type ShaderConfiguration(vertexShader:ShaderDescription, pixelShader: ShaderDescription, domainShader: ShaderDescription, hullShader: ShaderDescription) =
        let mutable vertexShader=vertexShader
        let mutable pixelShader=pixelShader
        let mutable domainShader=domainShader
        let mutable hullShader=hullShader

        member this.VertexShader
            with get() = vertexShader
            and set(value) = vertexShader <- value 

        member this.PixelShader
            with get() = pixelShader
            and set(value) = pixelShader <- value 

        member this.DomainShader
            with get() = domainShader
            and set(value) = domainShader <- value 

        member this.HullShader
            with get() = hullShader
            and set(value) = hullShader <- value 

        new(vertexShader:ShaderDescription, pixelShader:ShaderDescription) = ShaderConfiguration(vertexShader, pixelShader, ShaderDescription(), ShaderDescription())        
        new() = ShaderConfiguration(ShaderDescription(), ShaderDescription(), ShaderDescription(), ShaderDescription())
        member self.IsEmpty() = self.VertexShader.IsEmpty() && self.PixelShader.IsEmpty() && self.DomainShader.IsEmpty() && self.HullShader.IsEmpty() 