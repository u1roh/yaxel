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
    { RecordName: string
      RecordFields: TypedItem list }

and UnionType =
    { UnionName: string
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
        { RecordName = t.Name
          RecordFields =
              FSharpType.GetRecordFields t
              |> Array.map (fun prop ->
                  { Name = prop.Name
                    Type = ofSystemType prop.PropertyType })
              |> Array.toList }
        |> Record
    elif FSharpType.IsUnion t then
        { UnionName = t.Name
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
