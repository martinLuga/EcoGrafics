namespace GraficBase
//
//  MyGraphicWindow.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2021 Martin Luga. All rights reserved.
// 

open System
open System.Windows.Forms

open SharpDX

open DirectX.GraficUtils

open GPUModel.MyGPU
  
// ----------------------------------------------------------------------------------------------------
// Graphic Window
// ----------------------------------------------------------------------------------------------------
module GraficWindow =

    [<AllowNullLiteral>]
    type MyWindow() =
        inherit UserControl()
        let mutable clearColor = Color.Black
        let mutable myGpu : MyGPU = null

        member this.Boundery
            with set (value: int * int * int * int) =
                let (x, y, w, h) = value
                this.SetBounds(x, y, w, h)

        member this.AspectRatio =
            (float32) this.ClientSize.Width
            / (float32) this.ClientSize.Height

        member this.Renderer
            with get () = myGpu
            and set (value) = myGpu <- value

        member this.SetBackColor(aColor) = this.BackColor <- aColor

        override this.OnSizeChanged(e: EventArgs) = base.Invalidate()

        override this.ToString() = "MyWindow-" + this.Name

        member this.Close() = ()