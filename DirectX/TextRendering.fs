namespace DirectX

// Copyright (c) 2013 Justin Stenning
// Adapted from original code by Alexandre Mutel
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// Port to F# DirectX 12 - Martin Luga
 
open System

open SharpDX
open SharpDX.Direct2D1
open SharpDX.DirectWrite 

type  TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode 

module TextRendering =

    type TextRenderer(_font:string, _color:Color4, _location:Point, _size:int, _lineLength:int) =
    
        let mutable textFormat:TextFormat = null
        let mutable sceneColorBrush:Brush = null
        let mutable font:string = _font
        let mutable color:Color4 = _color
        let mutable lineLength:int = _lineLength
        let mutable size = _size
        let mutable text = ""
        let mutable location = new Point()
        let factory:Factory = null

        /// <summary>
        /// Initializes a new instance of <see cref="TextRenderer"/> class.
        /// </summary>
        static member Create (font, color, location) =
            let renderer = TextRenderer (font, color, location, 16, 500)        
            if  not (String.IsNullOrEmpty(font))  then
                renderer.Font <- font
            else
                renderer.Font <- "Calibri"

            renderer.Color <- color
            renderer.Location <- location
            renderer 

        member this.Size 
            with get() = size
            and set(value) = size <- value

        member this.Text 
            with get() = text
            and set(value) = text <- value

        member this.Font 
            with get() = font
            and set(value) = font <- value

        member this.Location 
            with get() = location
            and set(value) = location <- value

        member this.Color
            with get() = color
            and set(value) = color <- value 

        member this.LineLength 
            with get() = lineLength
            and set(value) = lineLength <- value


        /// <summary>
        /// Create any device resources
        /// </summary>
        member this.CreateDeviceDependentResources(targ:RenderTarget) =
        
            sceneColorBrush <-  new SolidColorBrush(targ , color) 
            textFormat <-  
                new TextFormat(
                    factory,
                    font,
                    float32 size
                ) 

        /// <summary>
        /// Render
        /// </summary>
        /// <param name="target">The target to render to (the same device manager must be used in both)</param>
        member this.Draw(targ:RenderTarget) =
        
            if not (String.IsNullOrEmpty(text)) then
                 ()
            else
                targ.DrawText(text, textFormat, new RectangleF(float32 location.X, float32 location.Y, float32 location.X + float32 lineLength, float32 location.Y + 16.0f), sceneColorBrush)