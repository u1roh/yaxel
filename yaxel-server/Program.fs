open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.FSharp.Reflection
open FSharp.Data

module Functions =

    type Material =
        | SUS304
        | SPCC

    type Foo = {
        X: int
        Y: bool
    }
    type Input = {
        A: int
        B: double
        C: Material
        D: string option
        E: Foo option
    }

    let hoge (x: Input) =
        x.A + 10

    let piyo (x: Material) =
        "buzz"

    let simple (x: int) =
        x * 2

[<EntryPoint>]
let main args =
    let basePath = IO.Path.Combine(IO.Directory.GetCurrentDirectory(), "../yaxel-client/build")
    let basePath = IO.Path.GetFullPath basePath
    printfn "basePath = %s" basePath

    let asm = System.Reflection.Assembly.GetExecutingAssembly()
    asm.GetTypes() |> printfn "%A"
    let funcModule = asm.GetTypes() |> Array.find (fun t -> t.Name = "Functions")
    
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
            elif pathes.Length >= 2 && pathes.[0] = "invoke" then
                use reader = new StreamReader(con.Request.InputStream)
                let args = reader.ReadToEnd()
                printfn "invoke: func = %s, args = %s" pathes.[1] args
                let json = JsonValue.Parse args
                printfn "json = %A" json
                let method = funcModule.GetMethod pathes.[1]
                match json with
                | JsonValue.Array a ->
                    let args =
                        (Meta.ofMethod method).FunParams
                        |> List.toArray
                        |> Array.zip a
                        |> Array.map (fun (json, param) -> Meta.deserialize param.Type json)
                    printfn "args = %A" args
                    let ret = method.Invoke (Unchecked.defaultof<_>, args)
                    printfn "%O" ret
                    use writer = new StreamWriter (out)
                    sprintf "%A" ret
                    |> writer.Write
                | _ ->
                    failwith "JSON is not array"
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
