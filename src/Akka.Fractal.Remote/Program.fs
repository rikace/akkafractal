open Akka
open Akka.FSharp
open Akka.Fractal.Common.AkkaHelpers
open System
open System
open Akka.Bootstrap.Docker
open Akka.Cluster

[<EntryPoint>]
let main argv =
    
    Console.Title <- sprintf "Akka Fractal Remote System - PID %d" (System.Diagnostics.Process.GetCurrentProcess().Id)

    let config = ConfigurationLoader.load().BootstrapFromDocker()
    use system = System.create "fractal" config
    
    let _ = 
        spawn system "listener"
        <| fun mailbox ->
            let cluster = Cluster.Get (mailbox.Context.System)
            cluster.Subscribe (mailbox.Self, [| typeof<ClusterEvent.IMemberEvent> |])
            mailbox.Defer <| fun () -> cluster.Unsubscribe (mailbox.Self)
            printfn "Created an actor on node [%A] with roles [%s]" cluster.SelfAddress (String.Join(",", cluster.SelfRoles))
            let rec seed () = 
                actor {
                    let! (msg: obj) = mailbox.Receive ()
                    match msg with
                    | :? ClusterEvent.IMemberEvent -> printfn "Cluster event %A" msg
                    | _ -> printfn "Received: %A" msg
                    return! seed () }
            seed ()    

    Console.ForegroundColor <- ConsoleColor.Green
    printfn "Remote Worker %s listening..." system.Name
    printfn "Press [ENTER] to exit."
    
    system.WhenTerminated.Wait()
    0
