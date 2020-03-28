module Server

open System
open System.IO
open System.Net
open FSharp.Data

let valueToJson (value: obj) =
    if isNull value then
        JsonValue.Null
    else
        match value with
        | :? int as x -> JsonValue.Number(decimal x)
        | :? float as x -> JsonValue.Number(decimal x)
        | :? string as x -> JsonValue.String x
        | _ -> JsonValue.String(value.ToString())

type private ServiceApi() =

    let basePath =
        let path = IO.Path.Combine(IO.Directory.GetCurrentDirectory(), "../yaxel-client/build")
        IO.Path.GetFullPath path

    let userPath =
        let path = IO.Path.Combine(IO.Directory.GetCurrentDirectory(), "../yaxel-user/Sample.fs")
        IO.Path.GetFullPath path

    let mutable breathCount = 0
    let mutable funcModule = DynamicCompilation.fromSourceFile userPath |> Result.map (fun asm -> asm.GetType "Sample")

    let watcher =
        let watcher = new System.IO.FileSystemWatcher()
        watcher.Path <- Path.GetDirectoryName userPath
        watcher.Filter <- "*.fs"
        watcher.NotifyFilter <-
            IO.NotifyFilters.FileName ||| IO.NotifyFilters.DirectoryName ||| IO.NotifyFilters.LastWrite
        watcher.EnableRaisingEvents <- true
        watcher.Changed
        |> Event.add (fun e ->
            printfn "flle changed: %O" e
            funcModule <- DynamicCompilation.fromSourceFile userPath |> Result.map (fun asm -> asm.GetType "Sample")
            breathCount <- breathCount + 1)
        printfn "watcher.Path = %s" watcher.Path
        printfn "watcher.Filter = %s" watcher.Filter
        watcher

    do
        printfn "basePath = %s" basePath
        printfn "userPath = %s" userPath

    member this.BreathCount = breathCount.ToString()

    member this.FunctionList =
        match funcModule with
        | Ok funcModule ->
            funcModule.GetMethods()
            |> Array.filter (fun m -> m.IsStatic)
            |> Array.map (fun m -> JsonValue.String m.Name)
            |> JsonValue.Array
        | Error e -> JsonValue.String e

    member this.GetFuction funcName =
        match funcModule with
        | Ok funcModule ->
            funcModule.GetMethod funcName
            |> Meta.ofMethod
            |> Meta.funToJsonValue
        | Error e -> JsonValue.String e

    member this.InvokeFunction(funcName, args: JsonValue) =
        printfn "InvokeFunction (%s, %A)" funcName args
        match funcModule with
        | Ok funcModule ->
            let method = funcModule.GetMethod funcName
            match args with
            | JsonValue.Array a ->
                let args =
                    (Meta.ofMethod method).FunParams
                    |> List.toArray
                    |> Array.zip a
                    |> Array.map (fun (json, param) -> Meta.deserialize param.Type json)
                method.Invoke(Unchecked.defaultof<_>, args) |> valueToJson
            | _ -> failwith "JSON is not array"
        | Error e -> JsonValue.String e

    member this.GetUserCode() = File.ReadAllText userPath

    member this.UpdateUserCode userCode = File.WriteAllText(userPath, userCode)

    member this.GetStaticFile(path, out: Stream) =
        let path =
            if path = "" then "index.html" else path

        let path = Path.Combine(basePath, path)
        if IO.File.Exists path then
            let content = IO.File.ReadAllBytes path
            out.Write(content, 0, content.Length)
        else
            printfn "file not found: %A" path

type Server() =
    let api = ServiceApi()

    member this.OnRequest(con: HttpListenerContext) =
        let path = con.Request.RawUrl.TrimStart '/'
        let out = con.Response.OutputStream

        if path.StartsWith "api/" then
            let pathes = path.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
            use writer = new StreamWriter(out)
            match pathes.[1..] with
            | [| "breath" |] -> api.BreathCount |> writer.Write
            | [| "function" |] -> api.FunctionList |> writer.Write
            | [| "function"; funcName |] -> api.GetFuction funcName |> writer.Write
            | [| "invoke"; funcName |] ->
                use reader = new StreamReader(con.Request.InputStream)
                let args = reader.ReadToEnd()
                printfn "invoke: func = %s, args = %s" funcName args
                let json = JsonValue.Parse args
                api.InvokeFunction(funcName, json) |> writer.Write
            | [| "usercode" |] -> api.GetUserCode() |> writer.Write
            | [| "update-usercode" |] ->
                use reader = new StreamReader(con.Request.InputStream)
                reader.ReadToEnd() |> api.UpdateUserCode
            | _ ->
                printfn "unknown API: pathes = %A" pathes
                writer.Write "Unknwon API"
        else
            api.GetStaticFile(path, out)
