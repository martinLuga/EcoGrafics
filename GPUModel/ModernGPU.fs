namespace GPUModel
//
//  ModernGPU.fs
//
//  Created by Martin Luga on 08.02.22.
//  Copyright © 2022 Martin Luga. All rights reserved.
// 

open log4net

open System
open System.Threading 
open System.Windows.Forms
open System.Collections.Generic 

open SharpDX
open SharpDX.Mathematics.Interop
open SharpDX.Direct3D 
open SharpDX.Direct3D12
open SharpDX.DXGI

open Base.LoggingSupport
open Base.ShaderSupport
open Base.VertexDefs

open DirectX.GraficUtils

open DX12GameProgramming

open MyFrame
open MyPipelineSupport
open MYUtils
open MyGPUInfrastructure
open MyGPU

type MeshGeometry= MyMesh.MeshGeometry<Vertex,int>
  
// ----------------------------------------------------------------------------------------------------
// GPU Abstraction
// ----------------------------------------------------------------------------------------------------
module ModernGPU = 

    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    //  Class  MyGPU 
    //      Init
    //      Configure
    //      Draw
    //      Update
    // ----------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------
    [<AllowNullLiteral>] 
    type MyModernGPU() = 
        inherit MyGPU() 

        // Update Frame-Eigenschaften (Camera-Position, Light,...)
        // Parameter ConstantBufferView FrameCB = reg(b1) ites Element   
        member this.UpdateView(bytes) = 
            if this.FrameResources.Count  > 0 then
                this.CurrFrameResource.FrameCB.CopyData(0, bytes)

        //
        // DrawPerObject mit dem Pipelinestate 
        //
        member this.DrawPerObject(objectIdx, geometryName:string, topology:PrimitiveTopology, materialIdx, textureIdx) =
            debugDRAW("OBJECT " + objectIdx.ToString())            
            if this.FrameResources.Count > 0 then

                this.CurrFrameResource.Recorder.PipelineState <- this.PipelineProvider.GetCurrentPipelineState()         
                let commandList = this.CurrFrameResource.Recorder.CommandList 

                // Geometrie
                commandList.SetVertexBuffer(0, this.MeshCache.getVertexBuffer(geometryName))
                commandList.SetIndexBuffer(Nullable (this.MeshCache.getIndexBuffer(geometryName)))
                commandList.PrimitiveTopology <- topology

                // Objekt Eigenschaften 
                let rootObjectParmIdx = 1
                commandList.SetGraphicsRootConstantBufferView(rootObjectParmIdx, this.CurrFrameResource.ObjectCB.ElementAdress(objectIdx))

                // Material Eigenschaften
                let rootMaterialParmIdx = 3
                commandList.SetGraphicsRootConstantBufferView(rootMaterialParmIdx, this.CurrFrameResource.MaterialCB.ElementAdress(materialIdx)) 

                // Textur (wenn vorhanden)    
                if textureIdx >= 0 then  
                    let rootTexturParmIdx = 0
                    commandList.SetGraphicsRootDescriptorTable(rootTexturParmIdx, this.TextureHeapWrapper.GetGpuHandle(textureIdx)) 
            
                commandList.DrawIndexedInstanced(this.MeshCache.getIndexCount(geometryName), 1, 0, 0, 0) 

        
        override this.BuildFrameResources(itemCount:int, materialsCount:int) = 
            this.DirectFrameResource <- new FrameResource(device, this.DirectRecorder, itemCount, this.ItemLength, materialsCount, this.MatLength, this.FrameLength) 
            this.FrameResources.Clear()
            for  i = 0 to NUMFRAMERESOURCES - 1 do 
                let frameRecorder = new Recorder("Recorder frame " + i.ToString(), device, commandQueue, null)
                this.FrameResources.Add(new FrameResource(device, frameRecorder, itemCount, this.ItemLength, materialsCount, this.MatLength, this.FrameLength))
                fenceEvents.Add(new AutoResetEvent(false))  