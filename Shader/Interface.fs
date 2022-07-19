namespace Shader
//
//  Interface.fs
//
//  Created by Martin Luga on 08.07.22.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX 

open Base.ModelSupport

// ----------------------------------------------------------------------------------------------------
//  Interfaces
// ----------------------------------------------------------------------------------------------------  
module Interface = 

    type IShader =
       // abstract method
       abstract member SHDRMaterial:Material-> bool 

       // abstract immutable property
       abstract member SHDRObject: Matrix -> Matrix -> Matrix ->  Vector3 

