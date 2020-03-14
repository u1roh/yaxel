module Meta

open Microsoft.FSharp.Reflection

type Type =
    | Int
    | Float
    | Bool
    | String
    | Option of Type
    | List of Type
    | Record of RecordType
    | Union of UnionType
    | Unknown of System.Type

and RecordType =
    { RecordName: string
      RecordFields: RecordField list }

and RecordField =
    { FieldName: string
      FieldType: Type }

and UnionType =
    { UnionName: string
      UnionCases: UnionCase list }

and UnionCase =
    { CaseName: string
      CaseType: Type option }


let private primitiveTypes =
    [ typeof<int>, Type.Int
      typeof<float>, Type.Float
      typeof<bool>, Type.Bool
      typeof<string>, Type.String ]
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
                  { FieldName = prop.Name
                    FieldType = ofSystemType prop.PropertyType })
              |> Array.toList }
        |> Record
    elif FSharpType.IsUnion t then
        { UnionName = t.Name
          UnionCases =
              FSharpType.GetUnionCases t
              |> Array.map (fun case ->
                  { CaseName = case.Name
                    CaseType = None })
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
    | Option t ->
        [ "union",
          [ "Some", toJsonValue t
            "None", JsonNull ]
          |> Map.ofList
          |> JsonObject ]
        |> Map.ofList
        |> JsonObject
    | List t ->
        [ "list", toJsonValue t ]
        |> Map.ofList
        |> JsonObject
    | Record r ->
        [ "record",
          r.RecordFields
          |> List.map (fun field -> field.FieldName, toJsonValue field.FieldType)
          |> Map.ofList
          |> JsonObject ]
        |> Map.ofList
        |> JsonObject
    | Union u ->
        [ "union",
          u.UnionCases
          |> List.map (fun case -> case.CaseName, JsonNull)
          |> Map.ofList
          |> JsonObject ]
        |> Map.ofList
        |> JsonObject
    | Unknown t -> t.FullName |> JsonString
