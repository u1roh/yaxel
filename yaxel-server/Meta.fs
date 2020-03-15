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
                    Type = Tuple [] }) // not implemented
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
        match x with
        | [] -> JsonNull
        | [ t ] -> toJsonValue t
        | _ ->
            x
            |> List.map toJsonValue
            |> List.toArray
            |> JsonArray
    | List t ->
        [ "list", toJsonValue t ]
        |> Map.ofList
        |> JsonObject
    | Fun(paramType, returnType) ->
        [ "fun",
          [| paramType; returnType |]
          |> Array.map toJsonValue
          |> JsonArray ]
        |> Map.ofList
        |> JsonObject
    | Record r ->
        [ "name", JsonString r.RecordName
          "record",
          r.RecordFields
          |> List.map (fun field -> field.Name, toJsonValue field.Type)
          |> Map.ofList
          |> JsonObject ]
        |> Map.ofList
        |> JsonObject
    | Union u ->
        [ "name", JsonString u.UnionName
          "union",
          u.UnionCases
          |> List.map (fun case -> case.Name, toJsonValue case.Type)
          |> Map.ofList
          |> JsonObject ]
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
      "return", toJsonValue f.FunReturnType
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
