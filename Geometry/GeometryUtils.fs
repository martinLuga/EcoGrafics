namespace Geometry
//
//  Framework.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System

open SharpDX

open Base.GlobalDefs

open DirectX.VertexDefs
open DirectX.GraficUtils

open GeometryTypes

// ----------------------------------------------------------------------------------------------------
// Utility 
// Bitmap und Texture
// STATUS: IN_ARBEIT
// ----------------------------------------------------------------------------------------------------
module GeometryUtils = 

    let ToRadians(v:Vector3) =
        let x  = v.X * v.X
        let y  = v.Y * v.Y
        let z  = v.Z * v.Z
        let r = sqrt (x + y + z) 
        let phi = atan (v.Y / v.X)
        let theta = acos (v.Z / r)
        (r, phi, theta)

    let ToCartesian(phi:float32, theta:float32, r:float32) =
        let x = r * cos phi * sin theta
        let y = r * sin phi * sin theta
        let z = r * cos theta
        new Vector3(x,y,z)

    // Jeweils Position, Normale, Farbe und UV
    let createVertex pos normal (color:Color) uv isTransparent = 
       let mutable colr4 = if isTransparent then ToTransparentColor(color.ToColor4()) else color.ToColor4()
       new Vertex(pos, normal, colr4, uv)

    // Die Normale als Kreuzprodukt von 2 Vektoren 
    // Bei 3 Punkten
    // Negativ, weil kreuzprodukt RH ist
    // wir hier aber immer LH arbeiten
    let createNormal p1 p2 p3 =   
        let u = p3 - p1
        let v = p3 - p2
        let norm = Vector3.Cross(u,v)
        norm.Normalize()
        -norm

    // ----------------------------------------------------------------------------------------------------
    // Die 4 Ecken des Quadrats, müssen in Reihenfolge im Uhrzeiger kommen
    // Indices zeigen auf Vertexe für 2 Dreiecke jeweils im Uhrzeigersinn
    // ----------------------------------------------------------------------------------------------------
    let square p1 p2 p3 p4 normal color idx isTransparent = 
        let v1 = createVertex p1 normal color  (new Vector2(0.0f, 0.0f)) isTransparent
        let v2 = createVertex p2 normal color  (new Vector2(1.0f, 0.0f)) isTransparent
        let v3 = createVertex p3 normal color  (new Vector2(1.0f, 1.0f)) isTransparent
        let v4 = createVertex p4 normal color  (new Vector2(0.0f, 1.0f)) isTransparent
        let vert = {SV1 = v1; SV2 = v2; SV3 = v3; SV4 = v4}
        let ind =  {IV1 = idx + 0; IV2 = idx + 1; IV3 = idx + 2; IV4 = idx + 3}
        (vert, ind) 

    // ----------------------------------------------------------------------------------------------------
    // Das Dreieck zu den 3 Punkten, Reihenfolge im Uhrzeiger 
    // ----------------------------------------------------------------------------------------------------
    let triangle p1 p2 p3 normal color idx  isTransparent = 
        let v1 = createVertex p1 normal color (new Vector2(0.0f, 0.0f)) isTransparent
        let v2 = createVertex p2 normal color (new Vector2(1.0f, 0.0f)) isTransparent
        let v3 = createVertex p3 normal color (new Vector2(1.0f, 1.0f)) isTransparent
        let vert = {TV1 = v1; TV2 = v2; TV3 = v3}
        let ind =  {ITV1 = idx + 0; ITV2 = idx + 1; ITV3 = idx + 2 }
        (vert, ind)

    // ----------------------------------------------------------------------------------------------------
    //  Richtungsänderungen bei zufälliger Bewegung
    // ----------------------------------------------------------------------------------------------------
    let updateDirectionRandom(random:Random) (dir:Vector3) =
        let x = (random.Next(-10,10) |> float32 )  / 100.0f
        let y = (random.Next(-10,10) |> float32  ) / 100.0f
        let z = (random.Next(-10,10) |> float32  ) / 100.0f  
        let dir = new Vector3(x, y, z)
        dir.Normalize()
        dir          

    let updateDirectionRandom2(random:Random) (dir:Vector3) =
        let deviation = 5  
        let x = dir.X + (random.Next(-deviation,deviation) |> float32 )  / 10.0f
        let y = dir.Y + (random.Next(-deviation,deviation) |> float32  ) / 10.0f
        let z = dir.Z + (random.Next(-deviation,deviation) |> float32  ) / 10.0f  
        let result = new Vector3(x, y, z)
        result.Normalize()
        result 

    // ----------------------------------------------------------------------------------------------------
    //  Rotationen Yaw pitch roll 
    // ----------------------------------------------------------------------------------------------------
    // Yaw pitch roll -> Quaternion 
    let rotation (pitch:float32, yaw:float32, roll:float32) = 
        Quaternion.RotationYawPitchRoll (yaw, pitch, roll)

    //  Quaternion -> Yaw pitch roll 
    let toPitchYawRoll(q:Quaternion) =
        let mutable pitch = 0.0f
        let mutable pitchB = 0.0f
        let mutable yaw = 0.0f

        // roll (x-axis rotation)
        let sinr_cosp = 2.0f * (q.W  * q.X + q.Y * q.Z) 
        let cosr_cosp = 1.0f - 2.0f * (q.X * q.X + q.Y * q.Y)
        let roll = atan2 sinr_cosp cosr_cosp  
        
        // pitch (y-axis rotation)
        let sinp = +2.0f * (q.W * q.Y - q.Z * q.X) 
        if  abs(sinp) >= 1.0f then
            let s = sign(sinp) 
            pitch <- pihalbe * (float32) s       // use 90 degrees if out of range
        else
            pitch <- asin(sinp) 
        
        // yaw (z-axis rotation)
        let siny_cosp = 2.0f * (q.W * q.Z + q.X * q.Y) 
        let cosy_cosp = 1.0f - 2.0f * (q.Y * q.Y + q.Z * q.Z)   
        yaw <- atan2 siny_cosp cosy_cosp  
        (pitch, yaw, roll)

    //-----------------------------------------------------------------------------------------------------
    // Rotationen um die Achsen
    // TODO: Target noch nicht berücksichtigt
    //-----------------------------------------------------------------------------------------------------  
    let rotationMatrixYPR(rotHorizontal, rotVertical) = 
        let oRotationQuat = Quaternion.RotationYawPitchRoll(rotHorizontal, rotVertical, 0.0f) 
        Matrix.RotationQuaternion(oRotationQuat) 

    // Generate a rotation matrix for rotation around Y-axis
    let rotationMatrixHor(rotHorizontal) = 
        let oRotationQuat = Quaternion.RotationAxis(Vector3.UnitY, rotHorizontal)
        Matrix.RotationQuaternion(oRotationQuat) 

    // Generate a rotation matrix for rotation around X-axis
    let rotationMatrixVert(rotVertical) = 
        let oRotationQuat = Quaternion.RotationAxis(Vector3.UnitX, rotVertical)
        Matrix.RotationQuaternion(oRotationQuat) 

    //-----------------------------------------------------------------------------------------------------
    // Drehung eines Objekts, so dass es zwischen Punkt p1 und p2 liegt
    //-----------------------------------------------------------------------------------------------------     
    let rotateBetween(p1:Vector3, p2:Vector3) =   
        p1.Normalize()
        p2.Normalize()
        let mutable v = Vector3.Cross(p1,p2)            // Vektor, senkrecht zu beiden
        let angle = acos(Vector3.Dot(p1, p2))           // Winkel zwischen v1, v2 
        Quaternion.RotationAxis(v, angle)               // Drehung um die Achse v mit Winkel w  

    let euler(v1: Vector3, v2: Vector3) =            
        let q = rotateBetween(v1, v2)                      // Winkel zwischen v1, v2  
        toPitchYawRoll(q)     

    //-----------------------------------------------------------------------------------------------------
    // Die Euler-Winkel bestimmen die Lage des Objekts im Raum 
    //
    // Pitch (the x component) is the rotation about the node’s x-axis.
    // Yaw (the y component) is the rotation about the node’s y-axis.
    // Roll (the z component) is the rotation about the node’s z-axis
    // Implementierung 2
    //-----------------------------------------------------------------------------------------------------
    let eulerAngle( p1:Vector3, p2:Vector3) =            
        let w = p2 - p1        
        let l = w.Length
        let lxz = sqrt( w.X * w.X +  w.Z * w.Z)
            
        // PITCH
        let mutable pitch = 0.0f
        let mutable pitchB = 0.0f
        if w.Y < 0.0f then
            pitchB <- pi - asin(lxz) 
        else  
            pitchB <- asin(lxz)
             
        if w.Z = 0.0f then
            pitch <- pitchB
        else 
            pitch <- pitchB * (float32) (sign(w.Z))  
            
        // YAW
        let mutable yaw = 0.0f
        if w.X = 0.0f && w.Z = 0.0f then
            yaw <- 0.0f
        else 
            let mutable inner =  w.X  / sin (pitch) 
            if inner > 1.0f then
                yaw <- pihalbe
            else 
                if inner < -1.0f then
                    yaw <- pihalbe
                else  
                    yaw <- asin(inner)
        (yaw, pitch, 0.0f)

    //-----------------------------------------------------------------------------------------------------
    // Begrenzungen, um die Singulatitäten bei Drehungen im Kreis zu vermeiden
    //-----------------------------------------------------------------------------------------------------  
    let adjustIIPi(angle) =        
        if angle > IIpi then
            angle - IIpi
        else if angle < -IIpi then
            angle + IIpi
        else angle

    let limitPIHalbe(angle) =        
        if angle >= pihalbe then
            pihalbe  - 0.01f
        else if angle <= -pihalbe then
            -pihalbe + 0.01f
        else angle