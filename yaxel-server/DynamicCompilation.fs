module Yaxel.DynamicCompilation

open System.IO
open Microsoft.FSharp.Reflection
open FSharp.Compiler.SourceCodeServices

let private scs = FSharpChecker.Create()

let fromSourceFile srcPath =
    let dllPath = Path.ChangeExtension(srcPath, ".dll")
    printfn "srcPath = %s, dllPath = %s" srcPath dllPath
    let errors, exitCode, asm =
        let runtimePath =
            let runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
            fun dll -> Path.Combine(runtimeDir, dll)
        scs.CompileToDynamicAssembly
            ([| "fsc.exe"
                "--noframework"
                "-r"
                (typeof<unit>).Assembly.Location
                "-r"
                runtimePath "mscorlib.dll"
                "-r"
                runtimePath "netstandard.dll"
                "-r"
                runtimePath "System.Runtime.dll"
                "-r"
                runtimePath "System.Runtime.Numerics.dll"
                "-r"
                runtimePath "System.Private.CoreLib.dll"
                "-r"
                runtimePath "System.Collections.dll"
                "-r"
                runtimePath "System.Net.Requests.dll"
                "-r"
                runtimePath "System.Net.WebClient.dll"
                "-o"
                dllPath
                "-a"
                srcPath |], execute = None)
        |> Async.RunSynchronously
    printfn "exitCode = %A" exitCode
    printfn "errors = %A" errors
    let errorJson =
        errors
        |> Array.map (sprintf "\"%O\"")
        |> String.concat ","
        |> sprintf "[%s]"
    asm
    |> Option.iter (fun asm ->
        asm.GetTypes()
        |> Array.filter FSharpType.IsModule
        |> Array.collect (fun t -> t.GetMethods())
        |> Array.filter (fun m -> m.IsStatic)
        |> Array.iter (printfn "%O"))
    match asm with
    | Some asm -> Ok asm
    | None -> Error errorJson
