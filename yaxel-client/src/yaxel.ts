
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

export function defaultValueOf(type: Type): any {
    if (type === null) return null;
    switch (type) {
        case "int": return 0;
        case "float": return 0.0;
        case "bool": return false;
        case "string": return "";
    }
    if (Array.isArray(type)) {
        return type.map(t => defaultValueOf(t));
    }
    switch (type.tag) {
        case "list": return [];
        case "record":
            {
                let obj: any = {};
                for (let f of type.fields) {
                    obj[f.name] = defaultValueOf(f.type);
                }
                return obj;
            }
        case "union":
            if (type.cases.length == 0) {
                return { name: null, value: null };
            } else {
                return { name: type.cases[0].name, value: defaultValueOf(type.cases[0].type) };
            }
    }

}
