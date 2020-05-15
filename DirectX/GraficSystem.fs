namespace DirectX
//
//  ExampleApp.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 

open log4net

open System.Windows.Forms
open System.Collections.Generic
open System.Runtime.InteropServices
open System.Diagnostics

open DirectX.D3DUtilities
open DirectX.Camera
open DirectX.MeshObjects

open SharpDX
open SharpDX.Windows
open SharpDX.Mathematics.Interop

open Base.Framework

open ApplicationBase.WindowControl
open ApplicationBase.DisplayableObject

open GPUModel.MyGPU
open GPUModel.MyGraphicWindow

open Shader.MyShaderConnector
open Shader.FrameResources.CookBook
open Shader.ShaderSupport
  
// ----------------------------------------------------------------------------------------------------
// Application using shaders from DirectX Cookbook  
//
// Singleton
//  gpu
//  window
//  renderLoop
// ----------------------------------------------------------------------------------------------------
module ExampleApp = 

    let logger = LogManager.GetLogger("ExampleApp")

    let mutable isRunning = false

    type Texture =  DirectX.D3DUtilities.Texture

    let mutable tessellationFactor = 1.0f
 
    let vertexShader = 
        ShaderDesription(
            "ExampleApp",
            "shaders",
            "VS",
            "VSMain",
            "vs_5_0"
        )
 
    let pixelShaderSimple = 
        ShaderDesription(
            "ExampleApp",
            "shaders",
            "SimplePS",
            "PSMain",
            "ps_5_0"
         )

    let tessShaderQuad = 
        ShaderDesription(
            "ExampleApp",
            "shaders",
            "TessellateQuad",
            "DS_Quads",
            "ds_5_0"
        )
 
    let pixelShaderPhong = 
         ShaderDesription(
            "ExampleApp",
            "shaders", 
            "PhongPS",
            "PSMain",
            "ps_5_0"
         )
 
    let pixelShaderLambert = 
        ShaderDesription(
            "ExampleApp",
            "shaders",
            "DiffusePS",
            "PSMain",
            "ps_5_0"
        )

    let pixelShaderBlinnPhong = 
        ShaderDesription(
            "ExampleApp",
            "shaders",
            "BlinnPhongPS",
            "PSMain",
            "ps_5_0"
        )

    // ----------------------------------------------------------------------------------------------------
    // System type 
    // ----------------------------------------------------------------------------------------------------
    type MySystem(graficWindow:MyWindow) =
        // Singleton
        static let mutable instance = MySystem()
        
        let mutable myGpu:MyGPU = MyGPU.Instance
        let mutable graficWindow=graficWindow 

        let mutable displayables = new List< Displayable>()
        let mutable materials = new Dictionary<string, Material>() 
        let mutable textures = new Dictionary<string, Texture>()
        let mutable geometries = new Dictionary<string, MeshData>()

        let mutable shaderConnector:MyShaderConnector = null

        let mutable inputLayoutName = ""
        let mutable signatureDescName = ""

        do
            inputLayoutName     <- DEFAULT_LAYOUT_NAME
            signatureDescName   <- DEFAULT_SIG_VS_NAME
            shaderConnector <- new MyShaderConnector("Default", inputLayoutName, signatureDescName, vertexShader, pixelShaderSimple, tessShaderQuad)

        new() = MySystem(MyWindow.Instance)

        static member Instance
            with get() = instance
            and set(value) = instance <- value

        static member configure(graficWindow:UserControl) =
            let win =  graficWindow:?> MyWindow
            MySystem.Instance <- new MySystem(win)
            MyGPU.Instance.InitDirect3D(win)
            MyGPU.Instance.FrameLength <- D3DUtil.CalcConstantBufferByteSize<FrameConstants>()
            MyGPU.Instance.MatLength   <- D3DUtil.CalcConstantBufferByteSize<MaterialConstants>()
            MyGPU.Instance.ItemLength  <- D3DUtil.CalcConstantBufferByteSize<ObjectConstants>()

        member this.MyGpu  
            with get() = myGpu
            and set(value) = myGpu <- value

        member this.ActiveConnector
            with get() = shaderConnector
            and set(value) = shaderConnector <- value

        // Setup grafic system
        member this.Reset() = 
            displayables <- new List< Displayable>() 
            materials <- Dictionary<string, Material>() 
            textures <- new Dictionary<string, Texture>()
            myGpu.Reset()

        member this.Stop() =
            isRunning <- false

        member this.ChangePixelShader(pixelShaderDesc:ShaderDesription) =
            myGpu.ShaderConnector.PixelShaderDesc <- pixelShaderDesc
            myGpu.Configure()

        member this.Geometry(displayable:Displayable) =
            if geometries.ContainsKey(displayable.Geometry.Name) then
                geometries.Item(displayable.Geometry.Name)
            else
                geometries.Add(displayable.Geometry.Name, displayable.Geometry.getVertexData())
                geometries.Item(displayable.Geometry.Name)

        // ----------------------------------------------------------------------------------------------------
        // Steuerung
        // ----------------------------------------------------------------------------------------------------
        member this.Start() =
            myGpu.ShaderConnector <- shaderConnector
            myGpu.Configure()
            isRunning <- true
            // Windows Render-Loop
            let loop = new RenderLoop(graficWindow)
            while loop.NextFrame() && isRunning do
                myGpu.StartUpdate()
                this.updatePerFrame() 
                myGpu.StartDraw()               
                let mutable idx = 0
                for displayable in displayables do
                    this.updatePerObject(idx, displayable)
                    this.updatePerMaterial(idx, displayable)
                    this.drawPerObject(idx, displayable)
                    idx <- idx + 1
                myGpu.EndDraw()
            Debug.Print("ExampleApp INFO: Loop END\n")

        // ----------------------------------------------------------------------------------------------------
        // Objekte initialisieren
        // ----------------------------------------------------------------------------------------------------
        member this.InitObjects() =            
            myGpu.StartDirectCommandList("InitObjectList")
            
            let materials = displayables |> List.ofSeq |> List.map (fun disp -> disp.Surface.Material) |> List.distinct

            myGpu.BuildFrameResources(displayables.Count, materials.Length)

            // Geometrie
            // Im ObjectCache werden zu jedem Objekt die Geometrie
            // und zu jeder Instanz die Parameter gehalten (GeometryWrapper)
            let mutable idx = 0
            for displayable in displayables do
                let meshData = this.Geometry(displayable)
                let objectParameter = 
                    new ObjectConstants(
                        WorldViewProjection=displayable.World * Camera.Instance.ViewProj,
                        World=displayable.World * worldMatrix,
                        WorldInverseTranspose=Matrix.Transpose(Matrix.Invert(displayable.World)),
                        ViewProjection=Camera.Instance.ViewProj
                    )
                myGpu.InstallObject(displayable.Geometry.Name, meshData)
            myGpu.FinalizeObjectCache()

            // Textures
            let files = filesInDirectory "ExampleApp" "textures"
            for file in files do 
                let textureName = (file.Name.Split('.')).[0]
                let textureFilename = file.FullName
                myGpu.InstallTextureBMP(textureName, textureFilename)

            myGpu.EndDirectCommandList()

        member this.ToggleRasterizerState()=
            myGpu.toggleRasterizerState()
            myGpu.Configure()
          
        // Add all information for display of an displayable objects  
        member this.AddObject(displayable:Displayable) = 
            displayables.Add(displayable)

        // ----------------------------------------------------------------------------------------------------
        // Update
        // ----------------------------------------------------------------------------------------------------
        member this.updatePerFrame() =
            let frameConst = 
                new FrameConstants(
                    TessellationFactor = tessellationFactor, 
                    Light = frameLight,
                    LightDir = lightDir,
                    CameraPosition  = Camera.Instance.Position    
                )
            myGpu.UpdateFrame(ref frameConst)

        member this.updatePerObject(idx:int, displayable:Displayable) = 
            let viewProjectionMatrix = Camera.Instance.ViewProj
            let perObjectWorld = displayable.World * Camera.Instance.World
            let newObject = 
                new ObjectConstants(
                    World=perObjectWorld,
                    WorldInverseTranspose=Matrix.Transpose(Matrix.Invert(perObjectWorld)),
                    WorldViewProjection=perObjectWorld * viewProjectionMatrix,
                    ViewProjection=viewProjectionMatrix
                )
            let perObject = Transpose(newObject)
            myGpu.UpdateObject(idx, displayable.Geometry.Name, ref perObject)

        // Eigentlich überflüssig, wenn sich die Materialdaten nicht ändern
        member this.updatePerMaterial(idx, displayable:Displayable) = 
            let mat = displayable.Surface.Material
            let newMaterial = 
                new MaterialConstants( 
                    Ambient = mat.Ambient,
                    Diffuse = mat.Diffuse,
                    Specular = mat.Specular,
                    SpecularPower = mat.SpecularPower,
                    Emissive = mat.Emissive,
                    HasTexture = RawBool(displayable.hasTexture()), 
                    UVTransform = Matrix.Identity
                )
            myGpu.UpdateMaterial(idx, ref newMaterial)

        member this.drawPerObject(idx, displayable) =
            let geometryName = displayable.Geometry.Name
            let materialName = displayable.Surface.Material.Name
            let textureName = if displayable.Surface.Texture = null then "" else displayable.Surface.Texture.Name
            myGpu.DrawPerObject(idx, geometryName, materialName, textureName)

