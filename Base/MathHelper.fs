namespace Base
//
//  MathHelper.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open SharpDX 
open System 

// ----------------------------------------------------------------------------------------------------
// Hilfs-klassen für Shaders  
// ----------------------------------------------------------------------------------------------------

module MathHelper =

    let _random = new Random()

    type MathHelper() =

        static member Rand( minValue:int, maxValue:int) = _random.Next(minValue, maxValue)
        static member Randf() = _random.NextFloat(0.0f, 1.0f)
        static member Randf( minValue:float32, maxValue:float32) = _random.NextFloat(minValue, maxValue)

        static member Sinf  (a:float)  = float32 (Math.Sin(a))
        static member Cosf  (d:double) = float32 (Math.Cos(d))
        static member Tanf  (a:double) = float32 (Math.Tan(a))
        static member Atanf (d:double) = float32 (Math.Atan(d))
        static member Atan2f(y:double, x:double) = float32 (Math.Atan2(y, x))
        static member Acosf (d:double) = float32 (Math.Acos(d))
        static member Expf  (d:double) = float32 (Math.Exp(d))
        static member Sqrtf (d:double) = float32 (Math.Sqrt(d))

        static member SphericalToCartesian(radius:float32, theta:float, phi:float) = 
            new Vector3(
                radius * MathHelper.Sinf(phi) * MathHelper.Cosf(theta),
                radius * MathHelper.Cosf(phi),
                radius * MathHelper.Sinf(phi) * MathHelper.Sinf(theta)
            )

        static member InverseTranspose(m:Matrix) =        
            // Inverse-transpose is just applied to normals. So zero out
            // translation row so that it doesn't get into our inverse-transpose
            // calculation--we don't want the inverse-transpose of the translation.
            let mutable result = m
            result.Row4 <- Vector4.UnitW
            Matrix.Transpose(Matrix.Invert(result))        

        /// <summary>
        /// Builds a matrix that can be used to reflect vectors about a plane.
        /// </summary>
        /// <param name="plane">The plane for which the reflection occurs. This parameter is assumed to be normalized.</param>
        /// <param name="result">When the method completes, contains the reflection matrix.</param>
        static member Reflection(plane: Plane) =        
            let mutable result = new Matrix()
            let num1 = plane.Normal.X
            let num2 = plane.Normal.Y
            let num3 = plane.Normal.Z
            let num4 = -2f * num1
            let num5 = -2f * num2
            let num6 = -2f * num3
            result.M11 <- (float32)((double)num4 * (double)num1 + 1.0)
            result.M12 <- num5 * num1
            result.M13 <- num6 * num1
            result.M14 <- 0.0f
            result.M21 <- num4 * num2
            result.M22 <- (float32)((double)num5 * (double)num2 + 1.0)
            result.M23 <- num6 * num2
            result.M24 <- 0.0f
            result.M31 <- num4 * num3
            result.M32 <- num5 * num3
            result.M33 <- (float32)((double)num6 * (double)num3 + 1.0)
            result.M34 <- 0.0f
            result.M41 <- num4 * plane.D
            result.M42 <- num5 * plane.D
            result.M43 <- num6 * plane.D
            result.M44 <- 1f
            result        

        /// <summary>
        /// Creates a matrix that flattens geometry into a shadow.
        /// </summary>
        /// <param name="light">The light direction. If the W component is 0, the light is directional light if the
        /// W component is 1, the light is a point light.</param>
        /// <param name="plane">The plane onto which to project the geometry as a shadow. This parameter is assumed to be normalized.</param>
        /// <param name="result">When the method completes, contains the shadow matrix.</param>
        static member Shadow(light:byref<Vector4>, plane:byref<Plane>) =
            let mutable result = new Matrix()
            let num1 = (float32)((double)plane.Normal.X * (double)light.X + (double)plane.Normal.Y * (double)light.Y + (double)plane.Normal.Z * (double)light.Z + (double)plane.D * (double)light.W)
            let num2 = -plane.Normal.X
            let num3 = -plane.Normal.Y
            let num4 = -plane.Normal.Z
            let num5 = -plane.D
            result.M11 <- num2 * light.X + num1
            result.M21 <- num3 * light.X
            result.M31 <- num4 * light.X
            result.M41 <- num5 * light.X
            result.M12 <- num2 * light.Y
            result.M22 <- num3 * light.Y + num1
            result.M32 <- num4 * light.Y
            result.M42 <- num5 * light.Y
            result.M13 <- num2 * light.Z
            result.M23 <- num3 * light.Z
            result.M33 <- num4 * light.Z + num1
            result.M43 <- num5 * light.Z
            result.M14 <- num2 * light.W
            result.M24 <- num3 * light.W
            result.M34 <- num4 * light.W
            result.M44 <- num5 * light.W + num1
            result  