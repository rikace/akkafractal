module Akka.Fractal.Server.WebApp

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection

open Giraffe
open FSharp.Control.Tasks.V2.ContextInsensitive
open Akka.FSharp
open Akka.Actor
open GiraffeViewEngine
open ViewEngine

open System
open Akka.Fractal.Common.AkkaHelpers
open Akka.Routing
open Microsoft.AspNetCore.Mvc

let indexHandler () =
    htmlLayout Pages.indexView ()
    
let startFractal =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
           
            let actorProvider = ctx.GetService<Actors.SseTileActorProvider>()
            let actor = actorProvider.Invoke()
            
            let command = Akka.Fractal.Common.Messages.FractalSize(4000,4000)
            actor.Tell command
            printfn "Sent Command RenderImage"
             
            return! json "OK" next ctx            
        }
        
let webApp =
    choose [
        GET >=>
            choose [
                routeCi "/" >=> indexHandler ()           
                routeCi "/run" >=> startFractal
            ]
        setStatusCode 404 >=> text "Not Found"
    ]

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message
    
let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:5001")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore
           
let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IHostingEnvironment>()
    (match env.IsDevelopment() with
    | true  -> app.UseDeveloperExceptionPage()
    | false -> app.UseGiraffeErrorHandler errorHandler)
        .UseHttpsRedirection()
        .UseCors(configureCors)
        .UseWebSockets()
        .UseMiddleware<WebSocketMiddleware.Middleware.WebSocketMiddleware>()
        .UseStaticFiles()
        .UseCookiePolicy()
        .UseGiraffe(webApp)
 
let configureServices (services : IServiceCollection) =
    let sp  = services.BuildServiceProvider()
    let env = sp.GetService<IHostingEnvironment>()
    
    services.Configure<CookiePolicyOptions>(fun (options : CookiePolicyOptions) ->
        options.CheckConsentNeeded <- fun _ -> true
        options.MinimumSameSitePolicy <- SameSiteMode.None
    ) |> ignore
    services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1) |> ignore
    
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore
    services.AddSingleton<ActorSystem>(fun _ ->
                let config = ConfigurationLoader.load()
                System.create "fractal" config ) |> ignore
    
    services.AddSingleton<Actors.SseTileActorProvider>(fun provider ->
                let actorSystem = provider.GetService<ActorSystem>()
                
                let deploymentOptions =
                    [    SpawnOption.Router(FromConfig.Instance) ]
           
                let tileRenderActor =
                    Akka.Fractal.Common.Actors.tileRenderActor actorSystem "remoteactor" (Some deploymentOptions)
                    
                Actors.SystemActors.TileRender <- tileRenderActor
                                      
                let fractalActor = Actors.fractalActor tileRenderActor actorSystem "fractalActor"
                
                Actors.SseTileActorProvider(fun () -> fractalActor)
                )    |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "Content")
    
    printfn "contentRoot - %s" contentRoot
    printfn "webRoot - %s" webRoot
    
    let webhost = 
       WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
    
    webhost.Run()           
    0