namespace GPUModel
//
//  MyGraphicWindow.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
// 

open System
open System.Windows.Forms
open System.Drawing

open MyGPU
  
// ----------------------------------------------------------------------------------------------------
// Graphic Window
// ----------------------------------------------------------------------------------------------------
module MyGraphicWindow =

    type MyWindow () =
        inherit UserControl() 
        let mutable clearColor = Color.Black

        static let mutable instance = new MyWindow()
        static member Instance
            with get() = 
                if instance.Equals(null) then
                    instance <- new MyWindow()
                instance
            and set(value) = instance <- value

        member this.Boundery
            with set(value:int*int*int*int) = 
                let (x,y,w,h) = value
                this.SetBounds(x,y,w,h)

        member this.Renderer 
            with get () = MyGPU.Instance

        override this.OnPaint(e:PaintEventArgs ) = 
           //  this.Renderer.Render() 
           ()

        override this.OnSizeChanged(e:EventArgs) = 
            base.Invalidate() 

        member this.Close() = 
            let rend = this.Renderer :> IDisposable
            rend.Dispose()