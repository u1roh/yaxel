
export type Type =
    | "int"
    | "float"
    | "bool"
    | "string"
    | Type[]    // tuple, or unit when the array is empty
    | ListType
    | RecordType
    | UnionType

export interface ListType {
    tag: 'list'
    type: Type
}

export interface RecordType {
    tag: 'record'
    name: string
    fields: TypedItem[]
}

export interface UnionType {
    tag: 'union'
    name: string
    cases: TypedItem[]
}

export interface TypedItem {
    name: string
    type: Type
}

export interface Fun {
    name: string
    ret: Type
    params: TypedItem[]
}
