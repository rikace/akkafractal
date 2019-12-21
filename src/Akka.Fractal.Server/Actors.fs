namespace Akka.Fractal.Server

module Actors =

    open Akka.Actor
    open System
    open Akka.FSharp
    open Akka.Actor
    open System.Threading.Tasks
    open Akka.Fractal.Common
    open Akka.Fractal.Common.AkkaHelpers
    open Newtonsoft.Json
    open SixLabors.ImageSharp
    open SixLabors.ImageSharp.PixelFormats
    open WebSocketMiddleware
    

    type SystemActors private () =
        static let tileRender = ref Unchecked.defaultof<IActorRef>
        
        static member TileRender with get() = !tileRender
                                 and set v = tileRender := v
    
    
    let renderActor (width : int) (height : int)
                    (split : int) system name =
        spawnOpt system name (fun (inbox : Actor<Messages.RenderedTile>) ->
            let ys = height / split
            let xs = width / split

            let rec loop (image : Image<Rgba32>) totalMessages = actor {
                let! renderedTile = inbox.Receive()
                
                Logging.logInfof inbox "RenderedTile X : %d - Y : %d - Size : %d" renderedTile.X renderedTile.Y (renderedTile.Bytes.Length)
                    
                let sseTile = Messages.SseFormatTile(renderedTile.X, renderedTile.Y, Convert.ToBase64String(renderedTile.Bytes))
                let text = JsonConvert.SerializeObject(sseTile)
                
                
                Middleware.sendMessageToSockets text 
                //a |> Async.RunSynchronously
   
                return! loop image (totalMessages - 1)
            }
            loop (new Image<Rgba32>(width, height)) (ys + xs))
            [ SpawnOption.SupervisorStrategy (Strategy.OneForOne (fun error ->
                    match error with
                    | _ -> Directive.Resume )) ]


    type SseTileActorProvider = delegate of unit -> IActorRef

    let fractalActor (tileRenderActor : IActorRef)
                     (system : ActorSystem) name =
        
        spawn system name (fun (mailbox : Actor<Messages.FractalSize>) ->

            let rec loop () = actor {
                let! request = mailbox.Receive()

                let split = 20
                let ys = request.Height / split
                let xs = request.Width / split
                
                let renderActor =
                    if mailbox.Context.Child("renderActor").IsNobody() then
                        Logging.logInfo mailbox "Creating child actor RenderActor" 
                        renderActor request.Width request.Height split mailbox.Context "renderActor"
                    else mailbox.Context.Child("renderActor")


                for y = 0 to split - 1 do
                    let mutable yy = ys * y
                    Logging.logInfof mailbox "Sending message %d - sseTileActor" y
                    for x = 0 to split - 1 do
                        let mutable xx = xs * x                        
                        tileRenderActor.Tell(Messages.RenderTile(yy, xx, xs, ys, request.Height, request.Width), renderActor)
                
                return! loop ()
            }
            loop ())
