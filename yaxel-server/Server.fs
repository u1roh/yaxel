module Yaxel.Server

open System
open System.IO
open System.Net
open FSharp.Data

type private ServiceApi() =

    static let restoreSampleModules() =
        printfn "restoreSampleModules()"
        IO.Directory.GetFiles "../yaxel-sample"
        |> Array.map (fun path ->
            printfn " - restore %s" path
            let dst = IO.Path.Combine(DynamicModule.SourceDirectory, IO.Path.GetFileName path)
            IO.File.Copy(path, dst, true)
            IO.Path.GetFileNameWithoutExtension path)

    do restoreSampleModules() |> ignore

    let watcher =
        let watcher =
            new FileSystemWatcher(Path = DynamicModule.SourceDirectory, Filter = "*.fs",
                                  NotifyFilter = (NotifyFilters.CreationTime ||| NotifyFilters.FileName),
                                  EnableRaisingEvents = true)
        watcher.Changed |> Observable.subscribe (fun _ -> printfn "ServiceApi.watcher.Changed")

    let userModules =
        let dic = Collections.Generic.Dictionary()
        Directory.GetFiles DynamicModule.SourceDirectory
        |> Array.map (Path.GetFileNameWithoutExtension >> fun name -> name, DynamicModule name)
        |> Array.iter (fun (name, item) -> dic.Add(name, item))
        dic

    let getModule name =
        let contains, found = userModules.TryGetValue name
        if contains then
            Ok found
        else
            sprintf "module '%s' not found" name
            |> JsonValue.String
            |> Error

    member this.RestoreSampleModules() =
        try
            for name in restoreSampleModules() do
                if not (userModules.ContainsKey name) then userModules.Add(name, DynamicModule name)
            Ok()
        with e -> e.ToString() |> Error
        |> valueToJson

    member this.GetModuleBreathCount modName =
        getModule modName
        |> Result.bind (fun m -> m.BreathCount)
        |> valueToJson

    member this.GetModuleList() =
        userModules.Keys
        |> Seq.sort
        |> Seq.toArray
        |> Ok
        |> valueToJson

    member this.AddNewModule(name: string) =
        let path = Path.Combine(DynamicModule.SourceDirectory, name + ".fs")
        if File.Exists path || userModules.ContainsKey name then
            Error "the name already exists"
        else
            File.WriteAllText
                (path,
                 sprintf "module %s\n" name)
            userModules.Add(name, DynamicModule name)
            Ok()
        |> valueToJson

    member this.DeleteModule(name: string) =
        let path = Path.Combine(DynamicModule.SourceDirectory, name + ".fs")
        if File.Exists path then
            File.Delete path
            userModules.Remove name |> ignore
            Ok()
        else
            Error "the module doesn't exist"
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
            if pathes.[pathes.Length - 1] <> "breath" then printfn "OnRequest: path = %s" path
            use writer = new StreamWriter(out)
            try
                match pathes.[1..] with
                | [| "modules"; "restore-sample" |] when con.Request.HttpMethod = "POST" -> api.RestoreSampleModules()
                | [| "modules" |] -> api.GetModuleList()
                | [| "modules"; "breath" |] ->
                    JsonValue.Number(decimal 0)
                    |> Ok
                    |> valueToJson
                | [| "modules"; "new" |] ->
                    use reader = new StreamReader(con.Request.InputStream)
                    let name = reader.ReadToEnd()
                    printfn "modules/new: name = %s" name
                    api.AddNewModule name
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
                | [| "modules"; modName |] ->
                    if con.Request.HttpMethod = "DELETE" then
                        api.DeleteModule modName
                    else
                        Error "unknown method" |> valueToJson
                | _ -> sprintf "unknown API: pathes = %A" pathes |> FSharp.Data.JsonValue.String
            with e ->
                e.ToString()
                |> Error
                |> valueToJson
            |> writer.Write
        else
            let path =
                Path.Combine
                    (basePath,
                     (if path = "" then "index.html" else path))
            if IO.File.Exists path then
                match Path.GetExtension path with
                | ".svg" -> Some "image/svg+xml"
                | ".css" -> Some "text/css"
                | ".html" -> Some "text/html"
                | ".js" -> Some "text/javascript"
                | _ -> None
                |> Option.iter (fun ct -> con.Response.ContentType <- ct)
                let content = IO.File.ReadAllBytes path
                out.Write(content, 0, content.Length)
            else
                printfn "file not found: %A" path
