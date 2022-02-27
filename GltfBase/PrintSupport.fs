namespace GltfBase
//
//  NodeAdapter.fs
//
//  Created by Martin Luga on 10.09.18.
//  Copyright © 2022 Martin Luga. All rights reserved.
//

open System.Collections.Generic

open System
open System.Collections.Generic 

open log4net

open SharpDX
open SharpDX.Direct3D
open SharpDX.Direct3D12
open SharpDX.DXGI
open SharpDX.Windows

open Base.GameTimer
open Base.LoggingSupport
open Base.ShaderSupport


open Base.GeometryUtils
open Base.ShaderSupport

open SharpDX

open VGltf.Types

open NodeAdapter
 
module PrintSupport = 

    let logger = LogManager.GetLogger("Print.Adapter")
    let logDebug = Debug(logger)
    let logInfo = Info(logger)
    let logError = Error(logger)
    let logWarn = Warn(logger)

    // ----------------------------------------------------------------------------------------------------
    // NodeAdapter
    // ----------------------------------------------------------------------------------------------------
    let printAdapter(adapter:NodeAdapter) = 
        adapter.printAll()
    
 

