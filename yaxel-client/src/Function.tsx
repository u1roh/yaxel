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

/*
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
*/


interface Props {
    name: string
}

interface State {
    func: Fun | null
}

class Function extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);
        this.state = { func: null };
    }
    componentDidMount() {
        fetch('function/' + this.props.name)
            .then(response => response?.text())
            .then(t => {
                let json = JSON.parse(t);
                console.log(json);
                /*
                let fun = jsonToFun(json);
                console.log(fun);
                this.setState({ func: fun });
                */
                this.setState({ func: json });
            });
    }
    render() {
        //<p>{this.state.func}</p>
        return (
            <div className="Function">
                <h1>'<span className="Function-name">{this.state.func?.name}</span>' function</h1>
                <h2>return</h2>
                <p>{JSON.stringify(this.state.func?.ret)}</p>
                <h2>params</h2>
                <ul>
                    {this.state.func?.params.map(p => <li>{p.name}</li>)}
                </ul>
            </div>
        );
    }
}

export default Function;
