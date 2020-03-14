open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.FSharp.Reflection

module Functions =

    type Material =
        | SUS304
        | SPCC

    type Input = {
        A: int
        B: double
        C: Material
    }

    let hoge (x: Input) =
        x.A + 10

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
        let path = con.Request.RawUrl.TrimStart '/'
        let out = con.Response.OutputStream

        let pathes = path.Split([|'/'|], StringSplitOptions.RemoveEmptyEntries)
        if pathes.Length > 0 && pathes.[0] = "function" then
            use writer = new StreamWriter (out)
            if pathes.Length = 1 then
                let methods =
                    funcModule.GetMethods()
                    |> Array.filter (fun m -> m.IsStatic)
                writer.WriteLine "<html><body><ul>"
                methods |> Seq.iter (fun method ->
                    sprintf "<li><a href='%s'>%A</a></li>" method.Name method
                    |> writer.WriteLine)
                writer.WriteLine "</ul></body></html>"
            else
                let method = funcModule.GetMethod pathes.[1]
                sprintf "method: %A" method |> writer.WriteLine
                method.ReturnType |> Meta.toMetaType |> sprintf "return type = %A" |> writer.WriteLine
                method.GetParameters() |> Seq.iteri (fun i param ->
                    sprintf "parameter type [%d] = %A" i (Meta.toMetaType param.ParameterType)
                    |> writer.WriteLine
                )
        else
            let path =
                if path = "" then "index.html" else path
            let path = Path.Combine (basePath, path)
            if IO.File.Exists path then
                let content = IO.File.ReadAllBytes path
                out.Write(content, 0, content.Length)
            else
                printfn "file not found: %A" path
        
        con.Response.Close()

    0
