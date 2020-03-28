open System

[<EntryPoint>]
let main args =

    let server = Server.Server ()
    let listener =
        let port =
            let port = Environment.GetEnvironmentVariable "PORT"
            if isNull port then "8080" else port
        printfn "port = %s" port
        let listener = new System.Net.HttpListener()
        listener.Prefixes.Add <| sprintf "http://*:%s/" port
        listener

    listener.Start()

    while true do
        let con = listener.GetContext()
        try
            server.OnRequest con
        with e ->
            printfn "error: %A" e
        
        con.Response.Close()

    0
