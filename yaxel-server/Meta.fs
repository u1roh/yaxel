module Meta

open System.Reflection
open Microsoft.FSharp.Reflection

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

type JsonValue =
    | JsonNull
    | JsonNumber of float
    | JsonString of string
    | JsonArray of JsonValue []
    | JsonObject of Map<string, JsonValue>
    override this.ToString() =
        match this with
        | JsonNull -> "null"
        | JsonNumber x -> x.ToString()
        | JsonString s -> sprintf "\"%s\"" s
        | JsonArray a ->
            a
            |> Array.map (fun x -> x.ToString())
            |> String.concat ","
            |> sprintf "[%s]"
        | JsonObject x ->
            x
            |> Map.toSeq
            |> Seq.map (fun (key, value) -> sprintf "\"%s\":%O" key value)
            |> String.concat ","
            |> sprintf "{%s}"


let rec toJsonValue (t: Type) =
    match t with
    | Int -> JsonString "int"
    | Float -> JsonString "float"
    | Bool -> JsonString "bool"
    | String -> JsonString "string"
    | Tuple x ->
        printfn "toJsonValue: Tuple %A" x
        match x with
        | [] -> JsonNull
        | [ t ] -> toJsonValue t
        | _ ->
            x
            |> List.map toJsonValue
            |> List.toArray
            |> JsonArray
    | List t ->
        [ "tag", JsonString "list"
          "type", toJsonValue t ]
        |> Map.ofList
        |> JsonObject
    | Fun(paramType, returnType) ->
        [ "tag", JsonString "fun"
          "param", toJsonValue paramType
          "ret", toJsonValue returnType ]
        |> Map.ofList
        |> JsonObject
    | Record r ->
        [ "tag", JsonString "record"
          "name", JsonString r.RecordName
          "fields",
          r.RecordFields
          |> List.map (fun item ->
              [ "name", JsonString item.Name
                "type", toJsonValue item.Type ]
              |> Map.ofList
              |> JsonObject)
          |> List.toArray
          |> JsonArray ]
        |> Map.ofList
        |> JsonObject
    | Union u ->
        [ "tag", JsonString "union"
          "name", JsonString u.UnionName
          "cases",
          u.UnionCases
          |> List.map (fun item ->
              [ "name", JsonString item.Name
                "type", toJsonValue item.Type ]
              |> Map.ofList
              |> JsonObject)
          |> List.toArray
          |> JsonArray ]
        |> Map.ofList
        |> JsonObject
    | Unknown t -> t.FullName |> JsonString

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
    [ "name", JsonString f.FunName
      "ret", toJsonValue f.FunReturnType
      "params",
      f.FunParams
      |> List.map (fun p ->
          [ "name", JsonString p.Name
            "type", toJsonValue p.Type ]
          |> Map.ofList
          |> JsonObject)
      |> List.toArray
      |> JsonArray ]
    |> Map.ofList
    |> JsonObject
