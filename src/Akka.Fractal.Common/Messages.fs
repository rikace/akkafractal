namespace Akka.Fractal.Common
open System

[<RequireQualifiedAccess>]
module Messages =

    [<Serializable>]
    type Completed private () =
        class
            static member instance with get() = Completed()
        end
        
    [<Serializable>]
    type RenderedTile(x : int, y : int, bytes : byte[]) =

        member __.X = x
        member __.Y = y
        member __.Bytes = bytes

    [<Serializable>]
    type RenderTile(x : int, y : int, height : int, width : int, imageHeight : int, imageWidth : int) =

        member __.X = x
        member __.Y = y
        member __.Height = height
        member __.Width = width
        member __.ImageHeight = imageHeight
        member __.ImageWidth = imageWidth

    [<Serializable>]
    type FractalSize(height : int, width : int) =
        member __.Height = height
        member __.Width = width
        
    [<Serializable>]
    type SseFormatTile(x : int,y : int, imageBase64 : string) =
        member __.X = x
        member __.Y = y
        member __.ImageBase64 = imageBase64
