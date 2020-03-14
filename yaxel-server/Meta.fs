module Meta

type Type =
    | Int
    | Float
    | Bool
    | String
    | Option of Type
    | List of Type
    | Record of RecordType

and RecordType =
    { Name: string
      Fields: RecordField list }

and RecordField =
    { Name: string
      Type: Type }

and UnionType =
    { Name: string
      Cases: UnionCase list }

and UnionCase =
    { Name: string }
