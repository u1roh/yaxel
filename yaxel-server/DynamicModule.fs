[<AutoOpen>]
module Yaxel.DynamicModule

open System
open System.IO
open FSharp.Data
open Microsoft.FSharp.Reflection

type Result<'a> = Result<'a, JsonValue>

let rec valueToJson (value: obj) =
    if isNull value then
        JsonValue.Null
    else
        match value with
        | :? unit -> JsonValue.Null
        | :? int as x -> JsonValue.Number(decimal x)
        | :? float as x -> JsonValue.Number(decimal x)
        | :? string as x -> JsonValue.String x
        | :? JsonValue as x -> x
        | _ ->
            let typ = value.GetType()
            if typ.IsArray then
                value :?> obj []
                |> Array.map valueToJson
                |> JsonValue.Array
            elif FSharpType.IsUnion typ then
                let case, objs = FSharpValue.GetUnionFields(value, typ)

                let value =
                    if isNull objs || objs.Length = 0 then
                        JsonValue.Null
                    elif objs.Length = 1 then
                        valueToJson objs.[0]
                    else
                        objs
                        |> Array.map valueToJson
                        |> JsonValue.Array
                [| "name", JsonValue.String case.Name
                   "value", value |]
                |> JsonValue.Record
            else
                JsonValue.String(value.ToString())

type DynamicModule(name) =
    static let dirPath = Path.Combine(IO.Directory.GetCurrentDirectory(), "../yaxel-user/") |> Path.GetFullPath
    static do printfn "DynamicModule: dirPath = %s" dirPath

    let path = Path.Combine(dirPath, name + ".fs")
    let build() = DynamicCompilation.fromSourceFile path |> Result.map (fun asm -> asm.GetType name)

    let mutable result = build()
    let mutable breathCount = 0

    let watcher =
        let watcher =
            new FileSystemWatcher(Path = dirPath, Filter = "*.fs", NotifyFilter = NotifyFilters.LastWrite,
                                  EnableRaisingEvents = true)
        watcher.Changed
        |> Observable.map (fun e ->
            printfn "watcher.Changed: e.Name = %s" e.Name
            e)
        |> Observable.filter (fun e -> e.Name = name + ".fs")
        |> Observable.subscribe (fun _ ->
            printfn "building..."
            result <- build()
            breathCount <- breathCount + 1)

    static member SourceDirectory = dirPath
    member this.Name = name

    member this.BreathCount: Result<int> = Ok breathCount

    member this.FunctionList: Result<string []> =
        result
        |> Result.map (fun funcModule ->
            funcModule.GetMethods()
            |> Array.filter (fun m -> m.IsStatic)
            |> Array.map (fun m -> m.Name))

    member this.GetFuction funcName =
        result
        |> Result.map (fun funcModule ->
            funcModule.GetMethod funcName
            |> Meta.ofMethod
            |> Meta.funToJsonValue)

    member this.InvokeFunction(funcName, args: JsonValue) =
        printfn "InvokeFunction (%s, %A)" funcName args
        result
        |> Result.map (fun funcModule ->
            let method = funcModule.GetMethod funcName
            match args with
            | JsonValue.Array a ->
                let args =
                    (Meta.ofMethod method).FunParams
                    |> List.toArray
                    |> Array.zip a
                    |> Array.map (fun (json, param) -> Meta.deserialize param.Type json)
                method.Invoke(Unchecked.defaultof<_>, args) |> valueToJson
            | _ -> failwith "JSON is not array")

    member this.GetUserCode() =
        try
            File.ReadAllText path
            |> JsonValue.String
            |> Ok
        with e ->
            e.ToString()
            |> JsonValue.String
            |> Error

    member this.UpdateUserCode userCode =
        try
            File.WriteAllText(path, userCode)
            result <- build()
            breathCount <- breathCount + 1
            result |> Result.map ignore
        with e ->
            e.ToString()
            |> JsonValue.String
            |> Error
