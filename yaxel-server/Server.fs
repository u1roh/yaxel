module Yaxel.Server

open System
open System.IO
open System.Net
open FSharp.Data

type private ServiceApi() =

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

    let userModule = DynamicModule("Sample")

    member this.GetBreathCount modName =
        getModule modName
        |> Result.bind (fun m -> m.BreathCount)
        |> valueToJson

    member this.GetModuleList() =
        userModules.Keys
        |> Seq.toArray
        |> Ok
        |> valueToJson

    member this.BreathCount = userModule.BreathCount |> valueToJson
    member this.FunctionList = userModule.FunctionList
    member this.GetFuction funcName = userModule.GetFuction funcName
    member this.InvokeFunction(funcName, args) = userModule.InvokeFunction(funcName, args)
    member this.GetUserCode() = userModule.GetUserCode()
    member this.UpdateUserCode userCode = userModule.UpdateUserCode userCode


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
            use writer = new StreamWriter(out)
            match pathes.[1..] with
            | [| "modules" |] -> api.GetModuleList()
            | [| "breath" |] -> api.BreathCount
            | [| "function" |] -> api.FunctionList
            | [| "function"; funcName |] -> api.GetFuction funcName
            | [| "invoke"; funcName |] ->
                use reader = new StreamReader(con.Request.InputStream)
                let args = reader.ReadToEnd()
                printfn "invoke: func = %s, args = %s" funcName args
                let json = JsonValue.Parse args
                api.InvokeFunction(funcName, json)
            | [| "usercode" |] -> api.GetUserCode()
            | [| "update-usercode" |] ->
                use reader = new StreamReader(con.Request.InputStream)
                reader.ReadToEnd() |> api.UpdateUserCode
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
