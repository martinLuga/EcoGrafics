namespace Base
//
//  Framework.fs
//
//  Created by Martin Luga on 08.02.18.
//  Copyright © 2018 Martin Luga. All rights reserved.
//

open System
open System.Linq
open System.Text
open System.Xml
open System.Xml.Linq

open System.IO
open System.Runtime.Serialization

// ----------------------------------------------------------------------------
// Immer benötigte Basis Funktionen
// String
// ----------------------------------------------------------------------------
module StringConvert =

    let ToInt(inputField:string) =
        if inputField.Trim() = "" then 0 else (Convert.ToInt32(inputField.Trim())) 

// ----------------------------------------------------------------------------
// Immer benötigte Basis Funktionen
// String
// ----------------------------------------------------------------------------
module StringSupport =


    let iterateLines aLine aLineFunc = Seq.iter aLineFunc aLine 

    let getString(currentLine: string, start, len) =
        currentLine.Substring(start, len).Trim(' ')

    let getInt(currentLine:string, start, len) =
         currentLine.Substring(start, len) |> int

    let getFloat(currentLine:string, start, len)=
         getInt(currentLine, start, len) |> float32

    let getElementsSeparatedBy(currentLine:string, separator:char) =
        currentLine.Split(separator) |> Array.filter (fun substr -> substr.Length > 0) |> Array.map (fun substr -> substr.Trim())

    let getElements(currentLine:string) =
        getElementsSeparatedBy(currentLine, ' ')

    let getElementCount(currentLine:string) =
        getElements(currentLine).Length 

    let  onlyDigits(currentLine:string) = 
         new String(currentLine.Where(Char.IsDigit).ToArray())

    let  onlyLetters(currentLine:string) = 
         new String(currentLine.Where(Char.IsLetter).ToArray())

    let toSystemString(aString:string) =
        new String(aString |> Seq.toArray)

    let toString = System.Text.Encoding.ASCII.GetString
    let toBytes (x : string) = System.Text.Encoding.ASCII.GetBytes x

    //  XML
    let serializeXml<'a> (x : 'a) =
        let xmlSerializer = new DataContractSerializer(typedefof<'a>)
        use stream = new MemoryStream()
        xmlSerializer.WriteObject(stream, x)
        toString <| stream.ToArray()

    let deserializeXml<'a> (xml : string) =
        let xmlSerializer = new DataContractSerializer(typedefof<'a>)
        use stream = new MemoryStream(toBytes xml)
        xmlSerializer.ReadObject(stream) :?> 'a

    let PrettyXml(xml:string) =
        let stringBuilder = new StringBuilder()
        let element = XElement.Parse(xml)
        let settings = new XmlWriterSettings()
        settings.OmitXmlDeclaration <- true
        settings.Indent <- true
        settings.NewLineOnAttributes <- true
        using (XmlWriter.Create(stringBuilder, settings)) (fun xmlWriter -> 
            element.Save(xmlWriter))
        stringBuilder.ToString()