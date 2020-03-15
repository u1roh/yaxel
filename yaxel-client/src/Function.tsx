import React from 'react';

type Type =
    | "int"
    | "float"
    | "bool"
    | "string"
    | TupleType
    | ListType
    | RecordType
    | UnionType

interface TupleType {
    types: Type[]
}

interface ListType {
    type: Type
}

interface TypedItem {
    name: string
    type: Type
}

interface RecordType {
    name: string
    fields: TypedItem[]
}

interface UnionType {
    name: string
    cases: TypedItem[]
}

function jsonToType(json: any): Type | null {
    switch (json) {
        case "int":
            return "int";
        default:
            return null;
    }
}

interface Fun {
    name: string
    ret: Type
    params: TypedItem[]
}

function jsonToFun(json: any): Fun | null {
    if (json.name === undefined) return null;
    if (json.ret === undefined) return null;
    if (json.params === undefined) return null;
    if (!Array.isArray(json.params)) return null;
    const ret = jsonToType(json.ret);
    if (ret == null) return null;
    return {
        name: json.name,
        ret: ret, params: new Array<TypedItem>()
    };
}


interface Props {
    name: string
}

interface State {
    text: string
    func: Function | null
}

class Function extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);
        this.state = { text: "hoge", func: null };
    }
    componentDidMount() {
        fetch('function/' + this.props.name)
            .then(response => response?.text())
            .then(t => {
                let json = JSON.parse(t);
                console.log(json);
                this.setState({ text: t, func: null });
            });
    }
    render() {
        return (
            <div className="Function">
                <div className="Function-name">{this.props.name}</div>
                <p>{this.state.text}</p>
                <p>{this.state.func}</p>
            </div>
        );
    }
}

export default Function;
