module Yaxel.Server

open System
open System.IO
open System.Net

type private ServiceApi() =
    let userModule = DynamicModule("Sample")
    member this.BreathCount = userModule.BreathCount.ToString()
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
            | [| "breath" |] -> api.BreathCount |> writer.Write
            | [| "function" |] -> api.FunctionList |> writer.Write
            | [| "function"; funcName |] -> api.GetFuction funcName |> writer.Write
            | [| "invoke"; funcName |] ->
                use reader = new StreamReader(con.Request.InputStream)
                let args = reader.ReadToEnd()
                printfn "invoke: func = %s, args = %s" funcName args
                let json = FSharp.Data.JsonValue.Parse args
                api.InvokeFunction(funcName, json) |> writer.Write
            | [| "usercode" |] -> api.GetUserCode() |> writer.Write
            | [| "update-usercode" |] ->
                use reader = new StreamReader(con.Request.InputStream)
                reader.ReadToEnd() |> api.UpdateUserCode
            | _ ->
                printfn "unknown API: pathes = %A" pathes
                writer.Write "Unknwon API"
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
