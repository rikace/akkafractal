namespace Akka.Fractal.Common

open System
open Akka
open Akka.Actor
open Akka.FSharp

open Akka.FSharp
open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats

module Actors =

    open Akka.Fractal.Common
    open Akka.Fractal.Common.AkkaHelpers
    open Akka.Routing
    
    let tileRenderActor (system : ActorSystem) name (opts : SpawnOption list option) =
        let options = defaultArg opts []
        Spawn.spawnOpt system name (fun (inbox : Actor<Messages.RenderTile>) ->
            let rec loop count = actor {
                let! renderTile = inbox.Receive()
                let sender = inbox.Sender()
                let self = inbox.Self
                
                Logging.logInfof inbox "TileRenderActor %A rendering %d, %d" self.Path renderTile.X renderTile.Y

                let res = mandelbrotSet renderTile.X renderTile.Y renderTile.Width renderTile.Height renderTile.ImageWidth renderTile.ImageHeight 0.5 -2.5 1.5 -1.5
                let bytes = toByteArray res
                                
                let tileImage = Messages.RenderedTile(renderTile.X, renderTile.Y, bytes)
                sender <! tileImage
                
                return! loop (count + 1)//              
            }
            loop 0)
           options 
