module Meta

open System.Reflection
open Microsoft.FSharp.Reflection
open FSharp.Data

type Type =
    | Int
    | Float
    | Bool
    | String
    | Tuple of Type list // unit if list is empty
    | List of Type
    | Fun of Type * Type
    | Record of RecordType
    | Union of UnionType
    | Unknown of System.Type

and TypedItem =
    { Name: string
      Type: Type }

and RecordType =
    { RecordSystemType: System.Type
      RecordName: string
      RecordFields: TypedItem list }

and UnionType =
    { UnionSystemType: System.Type
      UnionName: string
      UnionCases: TypedItem list }

let private primitiveTypes =
    [ typeof<int>, Int
      typeof<float>, Float
      typeof<bool>, Bool
      typeof<string>, String
      typeof<unit>, Tuple [] ]
    |> readOnlyDict

let rec ofSystemType (t: System.Type) =
    let contains, found = primitiveTypes.TryGetValue t
    if contains then
        found
    elif FSharpType.IsRecord t then
        { RecordSystemType = t
          RecordName = t.Name
          RecordFields =
              FSharpType.GetRecordFields t
              |> Array.map (fun prop ->
                  { Name = prop.Name
                    Type = ofSystemType prop.PropertyType })
              |> Array.toList }
        |> Record
    elif FSharpType.IsUnion t then
        { UnionSystemType = t
          UnionName = t.Name
          UnionCases =
              FSharpType.GetUnionCases t
              |> Array.map (fun case ->
                  { Name = case.Name
                    Type =
                        case.GetFields()
                        |> Array.map (fun x ->
                            printfn "case field: %A" x
                            x)
                        |> Array.map (fun prop -> ofSystemType prop.PropertyType)
                        |> Array.toList
                        |> Tuple })
              |> Array.toList }
        |> Union
    else
        Unknown t

let rec toJsonValue (t: Type) =
    match t with
    | Int -> JsonValue.String "int"
    | Float -> JsonValue.String "float"
    | Bool -> JsonValue.String "bool"
    | String -> JsonValue.String "string"
    | Tuple x ->
        printfn "toJsonValue: Tuple %A" x
        match x with
        | [] -> JsonValue.Null
        | [ t ] -> toJsonValue t
        | _ ->
            x
            |> List.map toJsonValue
            |> List.toArray
            |> JsonValue.Array
    | List t ->
        [| "tag", JsonValue.String "list"
           "type", toJsonValue t |]
        |> JsonValue.Record
    | Fun(paramType, returnType) ->
        [| "tag", JsonValue.String "fun"
           "param", toJsonValue paramType
           "ret", toJsonValue returnType |]
        |> JsonValue.Record
    | Record r ->
        [| "tag", JsonValue.String "record"
           "name", JsonValue.String r.RecordName
           "fields",
           r.RecordFields
           |> List.map (fun item ->
               [| "name", JsonValue.String item.Name
                  "type", toJsonValue item.Type |]
               |> JsonValue.Record)
           |> List.toArray
           |> JsonValue.Array |]
        |> JsonValue.Record
    | Union u ->
        [| "tag", JsonValue.String "union"
           "name", JsonValue.String u.UnionName
           "cases",
           u.UnionCases
           |> List.map (fun item ->
               [| "name", JsonValue.String item.Name
                  "type", toJsonValue item.Type |]
               |> JsonValue.Record)
           |> List.toArray
           |> JsonValue.Array |]
        |> JsonValue.Record
    | Unknown t -> t.FullName |> JsonValue.String

type Function =
    { FunName: string
      FunParams: TypedItem list
      FunReturnType: Type }

let ofMethod (m: MethodInfo) =
    { FunName = m.Name
      FunReturnType = ofSystemType m.ReturnType
      FunParams =
          m.GetParameters()
          |> Array.map (fun p ->
              { Name = p.Name
                Type = ofSystemType p.ParameterType })
          |> Array.toList }

let funToJsonValue f =
    [| "name", JsonValue.String f.FunName
       "ret", toJsonValue f.FunReturnType
       "params",
       f.FunParams
       |> List.map (fun p ->
           [| "name", JsonValue.String p.Name
              "type", toJsonValue p.Type |]
           |> JsonValue.Record)
       |> List.toArray
       |> JsonValue.Array |]
    |> JsonValue.Record

let rec deserialize (t: Type) (json: JsonValue) =
    match t, json with
    | Type.Int, JsonValue.Number x -> x |> int :> obj
    | Type.Float, JsonValue.Number x -> x |> float :> obj
    | Type.Bool, JsonValue.Boolean x -> x :> obj
    | Type.String, JsonValue.String x -> x :> obj
    | Type.Tuple [], JsonValue.Null -> Unchecked.defaultof<obj>
    | Type.Tuple ts, JsonValue.Array x -> failwith "not implemented"
    | Type.List t, JsonValue.Array x ->
        x
        |> Array.map (deserialize t)
        |> Array.toList :> obj
    | Type.Record t, JsonValue.Record x ->
        let values =
            x
            |> Array.map (fun (name, json) ->
                let field = t.RecordFields |> List.find (fun field -> field.Name = name)
                deserialize field.Type json)
        FSharpValue.MakeRecord(t.RecordSystemType, values)
    | Type.Union t, JsonValue.Record x ->
        let name =
            x
            |> Array.find (fst >> (=) "name")
            |> snd
            |> function
            | JsonValue.String name -> name
            | _ -> failwith "`name` not found"
        let args =
            x
            |> Array.tryFind (fst >> (=) "value")
            |> Option.map snd
            |> Option.filter ((<>) JsonValue.Null)
            |> Option.map (deserialize (t.UnionCases |> List.find (fun case -> case.Name = name)).Type)
            |> function
            | Some(x) -> [| x |]
            | None -> [||]

        let case = FSharpType.GetUnionCases t.UnionSystemType |> Array.find (fun case -> case.Name = name)
        FSharpValue.MakeUnion(case, args)
    | _ -> failwithf "Meta.deserialize: invalid input > t = %A, json = %A" t json
