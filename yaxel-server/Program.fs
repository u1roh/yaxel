open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks

module Functions =

    let hoge (x: int) =
        x + 10

[<EntryPoint>]
let main args =
    let basePath = IO.Path.Combine(IO.Directory.GetCurrentDirectory(), "../yaxel-client/build")
    let basePath = IO.Path.GetFullPath basePath
    printfn "basePath = %s" basePath

    let listener = new System.Net.HttpListener()
    listener.Prefixes.Add "http://*/"
    listener.Start()
    while true do
        let con = listener.GetContext()
        let path = con.Request.RawUrl.TrimStart '/'
        let out = con.Response.OutputStream

        if path.StartsWith "function/" then
            let pathes = path.Split '/'
            printfn "pathes = %A" pathes
            let funcname = pathes.[1]
            ()
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
