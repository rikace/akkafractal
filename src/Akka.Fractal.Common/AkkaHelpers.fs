namespace Akka.Fractal.Common

module AkkaHelpers =
        
    open System
    open System.IO
    open Microsoft.IO
    open Akka.Actor
  
    open SixLabors.ImageSharp
    open SixLabors.ImageSharp.Formats.Png
    open SixLabors.ImageSharp.PixelFormats
    open SixLabors.ImageSharp.Processing

    module ConfigurationLoader =

        open System.IO
        open Akka.Configuration
        
        let loadConfig (configFile : string) = 
            if File.Exists(configFile) |> not then 
                raise (new FileNotFoundException(sprintf "Cannot find akka config file %s" configFile))

            let config = File.ReadAllText(configFile)
            ConfigurationFactory.ParseString(config)
        
        let load () = loadConfig("akka.conf")



    type StreamManager private () =
        static  let streamManager = lazy (
            new RecyclableMemoryStreamManager()
        )
        static member Instance with get () = streamManager.Value

    let toByteArray (imageIn : Image<Rgba32>) : byte [] =        
        use ms = StreamManager.Instance.GetStream("BitmapConverter")
        let options = PngEncoder(Quantizer = KnownQuantizers.WebSafe)
        imageIn.SaveAsPng(ms, options)
        ms.Flush()
        ms.ToArray()

    let toBitmap (byteArrayIn : byte[]) : Image<Rgba32> =
        use ms = StreamManager.Instance.GetStream("BitmapConverter", byteArrayIn, 0, byteArrayIn.Length)
        let returnImage = Image.Load(byteArrayIn)
        returnImage
    
    
    let mandelbrotSet (xp : int) (yp : int) (w : int) (h :int) (width : int) (height : int)
                      (maxr : float) (minr : float) (maxi : float) (mini : float) : Image<Rgba32> =

        let img = new Image<Rgba32>(w, h)
        let mutable zx = 0.
        let mutable zy = 0.
        let mutable cx = 0.
        let mutable cy = 0.
        let mutable xjump = ((maxr - minr) / ( float width))

        let yjump = ((maxi - mini) / (float height))

        let mutable tempzx = 0.
        let loopmax = 1000
        let mutable loopgo = 0

        for x = xp to (xp + w) - 1 do
            cx <- (xjump * float x) - abs(minr)

            for y = yp to (yp + h) - 1 do
                zx <- 0.
                zy <- 0.
                cy <- (yjump * float y) - abs(mini)
                loopgo <- 0

                while (zx * zx + zy * zy <= 4. && loopgo < loopmax) do
                    loopgo <- loopgo + 1
                    tempzx <- zx
                    zx <- (zx * zx) - (zy * zy) + cx
                    zy <- (2. * tempzx * zy) + cy

                if loopgo <> loopmax then
                    img.[x - xp, y - yp] <- Rgba32(byte(loopgo % 32 * 7), byte(loopgo % 128 * 2), byte(loopgo % 16 * 14))
                else
                    img.[x - xp, y - yp] <- Rgba32.Black
        img
        
    module Remoting =
        open Akka.FSharp
        open Akka.Actor
     
        let parseAddress address = Deploy(RemoteScope (Address.Parse address))
        let deployRemotely remoteSystemAddress = SpawnOption.Deploy (parseAddress remoteSystemAddress)
