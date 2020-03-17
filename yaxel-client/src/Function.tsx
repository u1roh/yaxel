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


interface TypedInputState { }

class TypedInput extends React.Component<TypedItem, TypedInputState> {
    constructor(props: TypedItem) {
        super(props);
        this.state = {};
    }
    render() {
        return (<div className="TypedInput">
            <span>{this.props.name} : </span>
            {
                (this.props.type == "int") ? <input></input> : <p>{JSON.stringify(this.props.type)}</p>
            }
        </div>);
    }
}


interface FunctionProps {
    name: string
}

interface FunctionState {
    func: Fun | null
}

class Function extends React.Component<FunctionProps, FunctionState> {
    constructor(props: FunctionProps) {
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
                <h2>'<span className="Function-name">{this.state.func?.name}</span>' function</h2>
                <h3>return</h3>
                <p>{JSON.stringify(this.state.func?.ret)}</p>
                <h3>params</h3>
                {this.state.func?.params.map(p =>
                    <div>
                        <TypedInput name={p.name} type={p.type} />
                    </div>)}
            </div>
        );
    }
}

export default Function;
