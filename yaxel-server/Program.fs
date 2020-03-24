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
    let fromSourceFile srcPath =
        let dllPath = Path.ChangeExtension (srcPath, ".dll")
        printfn "srcPath = %s, dllPath = %s" srcPath dllPath
        let scs = FSharpChecker.Create()
        let errors, exitCode, asm =
            scs.CompileToDynamicAssembly(
                [|
                    "fsc.exe"
                    "--noframework"
                    "-r"; @"/usr/share/dotnet/sdk/3.0.103/FSharp/FSharp.Core.dll"
                    "-r"; (Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "mscorlib.dll"))
                    "-r"; (Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "netstandard.dll"))
                    "-r"; (Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "System.Runtime.dll"))
                    "-r"; (Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "System.Runtime.Numerics.dll"))
                    "-r"; (Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "System.Private.CoreLib.dll"))
                    "-r"; (Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "System.Collections.dll"))
                    "-r"; (Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "System.Net.Requests.dll"))
                    "-r"; (Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "System.Net.WebClient.dll"))
                    "-o"; dllPath
                    "-a"; srcPath
                |],
                execute = None)
            |> Async.RunSynchronously
        printfn "exitCode = %A" exitCode
        printfn "errors = %A" errors
        asm |> Option.iter (fun asm ->
            asm.GetTypes()
            |> Array.filter FSharpType.IsModule
            |> Array.collect (fun t -> t.GetMethods())
            |> Array.filter (fun m -> m.IsStatic)
            |> Array.iter (printfn "%O"))
        match asm with
        | Some asm -> Ok asm
        | None -> Error errors


[<EntryPoint>]
let main args =
    let basePath = IO.Path.Combine(IO.Directory.GetCurrentDirectory(), "../yaxel-client/build")
    let basePath = IO.Path.GetFullPath basePath
    printfn "basePath = %s" basePath

    let userPath = IO.Path.Combine(IO.Directory.GetCurrentDirectory(), "../yaxel-user/Sample.fs")
    let userPath = IO.Path.GetFullPath userPath
    printfn "userPath = %s" userPath

    let mutable funcModule = Compilation.fromSourceFile userPath |> Result.map (fun asm -> asm.GetType "Sample")
    match funcModule with
    | Error errors -> printfn "Error: %A" errors
    | _ -> ()

    let listener = new System.Net.HttpListener()
    listener.Prefixes.Add "http://*/"
    listener.Start()
    while true do
        let con = listener.GetContext()
        try
            let path = con.Request.RawUrl.TrimStart '/'
            let out = con.Response.OutputStream

            let pathes = path.Split([|'/'|], StringSplitOptions.RemoveEmptyEntries)
            if pathes.Length > 0 && pathes.[0] = "function" then
                use writer = new StreamWriter (out)
                match funcModule with
                | Ok funcModule ->
                    if pathes.Length = 1 then
                        funcModule.GetMethods()
                        |> Array.filter (fun m -> m.IsStatic)
                        |> Array.map (fun m -> sprintf "\"%s\"" m.Name)
                        |> String.concat ","
                        |> sprintf "[%s]"
                        |> writer.Write
                    else
                        funcModule.GetMethod pathes.[1]
                        |> Meta.ofMethod
                        |> Meta.funToJsonValue
                        |> writer.Write
                | Error errors ->
                    errors
                    |> Array.map (sprintf "\"%O\"")
                    |> String.concat ","
                    |> sprintf "[%s]"
                    |> writer.Write
            elif pathes.Length >= 2 && pathes.[0] = "invoke" then
                use reader = new StreamReader(con.Request.InputStream)
                let args = reader.ReadToEnd()
                printfn "invoke: func = %s, args = %s" pathes.[1] args
                let json = JsonValue.Parse args
                printfn "json = %A" json
                match funcModule with
                | Ok funcModule ->
                    let method = funcModule.GetMethod pathes.[1]
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
                | Error errors ->
                    use writer = new StreamWriter (out)
                    errors
                    |> Array.map (sprintf "\"%O\"")
                    |> String.concat ","
                    |> sprintf "[%s]"
                    |> writer.Write
                
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
