[<AutoOpen>]
module Yaxel.DynamicModule

open System
open System.IO
open FSharp.Data
open Microsoft.FSharp.Reflection

let rec private valueToJson (value: obj) =
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

    let path = Path.Combine(IO.Directory.GetCurrentDirectory(), "../yaxel-user/", name + ".fs") |> Path.GetFullPath
    let build() = DynamicCompilation.fromSourceFile path |> Result.map (fun asm -> asm.GetType name)

    let mutable result = build()
    let mutable breathCount = 0

    let watcher =
        let watcher =
            new FileSystemWatcher(Path = Path.GetDirectoryName path, Filter = "*.fs",
                                  NotifyFilter = NotifyFilters.LastWrite, EnableRaisingEvents = true)
        watcher.Changed
        |> Event.filter (fun e -> e.Name = name + ".fs")
        |> Event.add (fun _ ->
            result <- build()
            breathCount <- breathCount + 1)
        watcher

    member this.BreathCount = breathCount
    member this.Result = result

    member this.FunctionList =
        match result with
        | Ok funcModule ->
            funcModule.GetMethods()
            |> Array.filter (fun m -> m.IsStatic)
            |> Array.map (fun m -> JsonValue.String m.Name)
            |> JsonValue.Array
        | Error e -> JsonValue.String e

    member this.GetFuction funcName =
        match result with
        | Ok funcModule ->
            funcModule.GetMethod funcName
            |> Meta.ofMethod
            |> Meta.funToJsonValue
        | Error e -> JsonValue.String e

    member this.InvokeFunction(funcName, args: JsonValue) =
        printfn "InvokeFunction (%s, %A)" funcName args
        match result with
        | Ok funcModule ->
            let method = funcModule.GetMethod funcName
            match args with
            | JsonValue.Array a ->
                let args =
                    (Meta.ofMethod method).FunParams
                    |> List.toArray
                    |> Array.zip a
                    |> Array.map (fun (json, param) -> Meta.deserialize param.Type json)
                method.Invoke(Unchecked.defaultof<_>, args) |> valueToJson
            | _ -> failwith "JSON is not array"
        | Error e -> JsonValue.String e

    member this.GetUserCode() = File.ReadAllText path

    member this.UpdateUserCode userCode = File.WriteAllText(path, userCode)
