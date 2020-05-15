namespace DirectX
//
//  MathHelper.fs
//
//  Ported from luna directx 12
//

open System 
open SharpDX

// ----------------------------------------------------------------------------
// Convenience math functions
// Ported from luna directx 12
// ----------------------------------------------------------------------------
module MathHelper = 
    
    type MathHelper =
 
        static member _random = new Random() 
        static member Rand(minValue:int, maxValue:int) = MathHelper._random.Next(minValue, maxValue)
        static member Randf() = MathHelper._random.NextFloat(0.0f, 1.0f)
        static member Randf(minValue:float32, maxValue:float32) = MathHelper._random.NextFloat(minValue, maxValue)

        static member Sinf(a:float32) = float32 (Math.Sin(float a))
        static member Cosf(d:float32) = float32 (Math.Cos(float d))
        static member Tanf(a:float32) = float32 (Math.Tan(float a))
        static member Atanf(d:float32) =  float32 (Math.Atan(float d))
        static member Atan2f(y:float32, x:Int64) =  float32 (Math.Atan2(float y, float x))
        static member Acosf(d:float32) =  float32 (Math.Acos(float d))
        static member Expf(d:float32) =  float32 (Math.Exp(float d))
        static member Sqrtf(d:float32) =  float32 (Math.Sqrt(float d))

        static member SignBool(d:float32) = if Math.Sign(d) > 0 then true else false

        static member SphericalToCartesian(radius:float32, theta:float32, phi:float32) =
            new Vector3(
                radius * MathHelper.Sinf(phi) * MathHelper.Cosf(theta),
                radius * MathHelper.Cosf(phi),
                radius * MathHelper.Sinf(phi) * MathHelper.Sinf(theta))

        static member InverseTranspose(m:Matrix) = 
            // Inverse-transpose is just applied to normals. So zero out
            // translation row so that it doesn't get into our inverse-transpose
            // calculation--we don't want the inverse-transpose of the translation.
            let mutable mc = m
            mc.Row4 <- Vector4.UnitW
            Matrix.Transpose(Matrix.Invert(mc))
 

        /// <summary>
        /// Builds a matrix that can be used to reflect vectors about a plane.
        /// </summary>
        /// <param name="plane">The plane for which the reflection occurs. This parameter is assumed to be normalized.</param>
        /// <param name="result">When the method completes, contains the reflection matrix.</param>
        static member _Reflection(plane:Plane) = 
            let mutable result = new Matrix() 
            let num1 = plane.Normal.X
            let num2 = plane.Normal.Y
            let num3 = plane.Normal.Z
            let num4 = -2.0f * num1
            let num5 = -2.0f * num2
            let num6 = -2.0f * num3
            result.M11 <- num4 *  num1 + 1.0f
            result.M12 <- num5 * num1
            result.M13 <- num6 * num1
            result.M14 <- 0.0f
            result.M21 <- num4 * num2
            result.M22 <- num5 * num2 + 1.0f
            result.M23 <- num6 * num2
            result.M24 <- 0.0f
            result.M31 <- num4 * num3
            result.M32 <- num5 * num3
            result.M33 <- num6 * num3 + 1.0f
            result.M34 <- 0.0f
            result.M41 <- num4 * plane.D
            result.M42 <- num5 * plane.D
            result.M43 <- num6 * plane.D
            result.M44 <- 1.0f
            result 

        /// <summary>
        /// Builds a matrix that can be used to reflect vectors about a plane.
        /// </summary>
        /// <param name="plane">The plane for which the reflection occurs. This parameter is assumed to be normalized.</param>
        /// <returns>The reflection matrix.</returns>
        static member Reflection(plane:Plane) = 
            MathHelper._Reflection(plane) 

        /// <summary>
        /// Creates a matrix that flattens geometry into a shadow.
        /// </summary>
        /// <param name="light">The light direction. If the W component is 0, the light is directional light if the
        /// W component is 1, the light is a point light.</param>
        /// <param name="plane">The plane onto which to project the geometry as a shadow. This parameter is assumed to be normalized.</param>
        /// <param name="result">When the method completes, contains the shadow matrix.</param>
        static member Shadow(light:Vector4, plane:Plane) =
            let mutable result = new Matrix()  
            let num1 =  plane.Normal.X * light.X + plane.Normal.Y * light.Y + plane.Normal.Z * light.Z +  plane.D *  light.W 
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

        /// <summary>
        /// Creates a matrix that flattens geometry into a shadow.
        /// </summary>
        /// <param name="light">The light direction. If the W component is 0, the light is directional light if the
        /// W component is 1, the light is a point light.</param>
        /// <param name="plane">The plane onto which to project the geometry as a shadow. This parameter is assumed to be normalized.</param>
        /// <returns>The shadow matrix.</returns>
        static member _Shadow(light:Vector4, plane:Plane) = 
            MathHelper.Shadow(light, plane)

    // 
    // Minimum / Maximum
    // 
    
    let maxVector3 (vec1:Vector3) (vec2:Vector3) =
        Vector3.Max(vec1,vec2)
            
    let minVector3 (vec1:Vector3) (vec2:Vector3) =
        Vector3.Min(vec1,vec2)

    let computeCenter (minV:Vector3) (maxV:Vector3)  =
        Vector3(
            minV.X + (maxV.X - minV.X) / 2.0f,
            minV.Y+  (maxV.Y - minV.Y) / 2.0f,
            minV.Z+  (maxV.Z - minV.Z) / 2.0f 
        )

    let computeMinPosition (points: Vector3[]) =
        if points.Length = 0 then Vector3.Zero
        else 
            let min = points |> Seq.toList |> List.map (fun a -> a)|> List.reduce minVector3  
            min

    let computeMaxPosition (points: Vector3[]) =
        if points.Length = 0 then Vector3.Zero
        else 
            let max = points |> Seq.toList |> List.map (fun a -> a)|> List.reduce maxVector3 
            max

