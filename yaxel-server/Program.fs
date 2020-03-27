open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.FSharp.Reflection
open FSharp.Data
open Microsoft.FSharp.Reflection
open FSharp.Compiler.SourceCodeServices

let valueToJson (value: obj) =
    if isNull value then JsonValue.Null else
    match value with
    | :? int as x -> JsonValue.Number (decimal x)
    | :? float as x -> JsonValue.Number (decimal x)
    | :? string as x -> JsonValue.String x
    | _ -> JsonValue.String (value.ToString())

module Compilation =
    let private scs = FSharpChecker.Create()

    let fromSourceFile srcPath =
        let dllPath = Path.ChangeExtension (srcPath, ".dll")
        printfn "srcPath = %s, dllPath = %s" srcPath dllPath
        let errors, exitCode, asm =
            let runtimePath =
                let runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
                fun dll -> Path.Combine(runtimeDir, dll)
            scs.CompileToDynamicAssembly(
                [|
                    "fsc.exe"
                    "--noframework"
                    "-r"; (typeof<unit>).Assembly.Location
                    "-r"; runtimePath "mscorlib.dll"
                    "-r"; runtimePath "netstandard.dll"
                    "-r"; runtimePath "System.Runtime.dll"
                    "-r"; runtimePath "System.Runtime.Numerics.dll"
                    "-r"; runtimePath "System.Private.CoreLib.dll"
                    "-r"; runtimePath "System.Collections.dll"
                    "-r"; runtimePath "System.Net.Requests.dll"
                    "-r"; runtimePath "System.Net.WebClient.dll"
                    "-o"; dllPath
                    "-a"; srcPath
                |],
                execute = None)
            |> Async.RunSynchronously
        printfn "exitCode = %A" exitCode
        printfn "errors = %A" errors
        let errorJson =
            errors
            |> Array.map (sprintf "\"%O\"")
            |> String.concat ","
            |> sprintf "[%s]"
        asm |> Option.iter (fun asm ->
            asm.GetTypes()
            |> Array.filter FSharpType.IsModule
            |> Array.collect (fun t -> t.GetMethods())
            |> Array.filter (fun m -> m.IsStatic)
            |> Array.iter (printfn "%O"))
        match asm with
        | Some asm -> Ok asm
        | None -> Error errorJson


[<EntryPoint>]
let main args =
    let basePath = IO.Path.Combine(IO.Directory.GetCurrentDirectory(), "../yaxel-client/build")
    let basePath = IO.Path.GetFullPath basePath
    printfn "basePath = %s" basePath

    let userPath = IO.Path.Combine(IO.Directory.GetCurrentDirectory(), "../yaxel-user/Sample.fs")
    let userPath = IO.Path.GetFullPath userPath
    printfn "userPath = %s" userPath

    let mutable breathCount = 0
    let mutable funcModule = Compilation.fromSourceFile userPath |> Result.map (fun asm -> asm.GetType "Sample")

    let watcher =
        let watcher = new System.IO.FileSystemWatcher()
        watcher.Path <- Path.GetDirectoryName userPath
        watcher.Filter <- "*.fs"
        watcher.NotifyFilter <- IO.NotifyFilters.FileName ||| IO.NotifyFilters.DirectoryName ||| IO.NotifyFilters.LastWrite
        watcher.EnableRaisingEvents <- true
        watcher.Changed |> Event.add (fun e ->
            printfn "flle changed: %O" e
            funcModule <- Compilation.fromSourceFile userPath |> Result.map (fun asm -> asm.GetType "Sample")
            breathCount <- breathCount + 1)
        printfn "watcher.Path = %s" watcher.Path
        printfn "watcher.Filter = %s" watcher.Filter
        watcher

    let listener = new System.Net.HttpListener()
    listener.Prefixes.Add "http://*/"
    listener.Start()
    while true do
        let con = listener.GetContext()
        try
            let path = con.Request.RawUrl.TrimStart '/'
            let out = con.Response.OutputStream

            if path.StartsWith "api/" then
                let pathes = path.Split([|'/'|], StringSplitOptions.RemoveEmptyEntries)
                use writer = new StreamWriter (out)
                match pathes.[1..] with
                | [| "breath" |] -> breathCount.ToString() |> writer.Write 
                | [| "function" |] ->
                    match funcModule with
                    | Ok funcModule ->
                        funcModule.GetMethods()
                        |> Array.filter (fun m -> m.IsStatic)
                        |> Array.map (fun m -> sprintf "\"%s\"" m.Name)
                        |> String.concat ","
                        |> sprintf "[%s]"
                        |> writer.Write
                    | Error e -> writer.Write e
                | [| "function"; funcName |] ->
                    match funcModule with
                    | Ok funcModule ->
                        funcModule.GetMethod funcName
                        |> Meta.ofMethod
                        |> Meta.funToJsonValue
                        |> writer.Write
                    | Error e -> writer.Write e
                | [| "invoke"; funcName |] ->
                    use reader = new StreamReader(con.Request.InputStream)
                    let args = reader.ReadToEnd()
                    printfn "invoke: func = %s, args = %s" funcName args
                    let json = JsonValue.Parse args
                    printfn "json = %A" json
                    match funcModule with
                    | Ok funcModule ->
                        let method = funcModule.GetMethod funcName
                        match json with
                        | JsonValue.Array a ->
                            let args =
                                (Meta.ofMethod method).FunParams
                                |> List.toArray
                                |> Array.zip a
                                |> Array.map (fun (json, param) -> Meta.deserialize param.Type json)
                            printfn "args = %A" args
                            let ret = method.Invoke (Unchecked.defaultof<_>, args) |> valueToJson
                            printfn "%O" ret
                            use writer = new StreamWriter (out)
                            ret.ToString() |> writer.Write
                        | _ ->
                            failwith "JSON is not array"
                    | Error e -> writer.Write e
                | [| "usercode" |] ->
                    use reader = new StreamReader(userPath)
                    reader.ReadToEnd() |> writer.Write
                | [| "update-usercode" |] ->
                    use reader = new StreamReader(con.Request.InputStream)
                    let userCode = reader.ReadToEnd()
                    IO.File.WriteAllText (userPath, userCode)
                | _ ->
                    printfn "unknown API: pathes = %A" pathes
                    writer.Write "Unknwon API"
            else
                let path =
                    if path = "" then "index.html" else path
                let path = Path.Combine (basePath, path)
                if IO.File.Exists path then
                    let content = IO.File.ReadAllBytes path
                    out.Write(content, 0, content.Length)
                else
                    printfn "file not found: %A" path
        with e ->
            printfn "error: %A" e
        
        con.Response.Close()

    0
