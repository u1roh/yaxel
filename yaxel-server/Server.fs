module Yaxel.Server

open System
open System.IO
open System.Net
open FSharp.Data

type private ServiceApi() =

    let watcher =
        let watcher =
            new FileSystemWatcher(Path = DynamicModule.SourceDirectory, Filter = "*.fs",
                                  NotifyFilter = (NotifyFilters.CreationTime ||| NotifyFilters.FileName),
                                  EnableRaisingEvents = true)
        watcher.Changed |> Observable.subscribe (fun _ -> printfn "ServiceApi.watcher.Changed")

    let userModules =
        Directory.GetFiles DynamicModule.SourceDirectory
        |> Array.map (Path.GetFileNameWithoutExtension >> fun name -> name, DynamicModule name)
        |> dict

    let getModule name =
        let contains, found = userModules.TryGetValue name
        if contains then
            Ok found
        else
            sprintf "module '%s' not found" name
            |> JsonValue.String
            |> Error

    member this.GetModuleBreathCount modName =
        getModule modName
        |> Result.bind (fun m -> m.BreathCount)
        |> valueToJson

    member this.GetModuleList() =
        userModules.Keys
        |> Seq.toArray
        |> Ok
        |> valueToJson

    member this.GetFunctionList modName =
        getModule modName
        |> Result.bind (fun m -> m.FunctionList)
        |> valueToJson

    member this.GetFunction(modName, funcName) =
        getModule modName
        |> Result.bind (fun m -> m.GetFuction funcName)
        |> valueToJson

    member this.InvokeFunction(modName, funcName, args) =
        getModule modName
        |> Result.bind (fun m -> m.InvokeFunction(funcName, args))
        |> valueToJson

    member this.GetUserCode modName =
        getModule modName
        |> Result.bind (fun m -> m.GetUserCode())
        |> valueToJson

    member this.UpdateUserCode(modName, userCode) =
        printfn "UpdateUserCode(%s, %s)" modName userCode
        getModule modName
        |> Result.bind (fun m -> m.UpdateUserCode userCode)
        |> valueToJson


type Server() =
    let api = ServiceApi()

    let basePath =
        let path = IO.Path.Combine(IO.Directory.GetCurrentDirectory(), "../yaxel-client/build")
        IO.Path.GetFullPath path

    member this.OnRequest(con: HttpListenerContext) =
        let path = con.Request.RawUrl.TrimStart '/'
        let out = con.Response.OutputStream

        if path.StartsWith "api/" then
            let pathes = path.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
            if pathes.[1] <> "breath" then printfn "OnRequest: path = %s" path
            use writer = new StreamWriter(out)
            match pathes.[1..] with
            | [| "modules" |] -> api.GetModuleList()
            | [| "modules"; "breath" |] ->
                JsonValue.Number(decimal 0)
                |> Ok
                |> valueToJson
            | [| "modules"; modName; "breath" |] -> api.GetModuleBreathCount modName
            | [| "modules"; modName; "functions" |] -> api.GetFunctionList modName
            | [| "modules"; modName; "functions"; funcName |] -> api.GetFunction(modName, funcName)
            | [| "modules"; modName; "functions"; funcName; "invoke" |] ->
                use reader = new StreamReader(con.Request.InputStream)
                let args = reader.ReadToEnd()
                printfn "invoke: func = %s, args = %s" funcName args
                let json = JsonValue.Parse args
                api.InvokeFunction(modName, funcName, json)
            | [| "modules"; modName; "usercode" |] -> api.GetUserCode modName
            | [| "modules"; modName; "usercode"; "update" |] ->
                use reader = new StreamReader(con.Request.InputStream)
                api.UpdateUserCode(modName, reader.ReadToEnd())
            | _ -> sprintf "unknown API: pathes = %A" pathes |> FSharp.Data.JsonValue.String
            |> writer.Write
        else
            let path =
                Path.Combine
                    (basePath,
                     (if path = "" then "index.html" else path))
            if IO.File.Exists path then
                let content = IO.File.ReadAllBytes path
                out.Write(content, 0, content.Length)
            else
                printfn "file not found: %A" path