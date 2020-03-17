import React from 'react';

type Type =
    | "int"
    | "float"
    | "bool"
    | "string"
    | Type[]    // tuple, or unit when the array is empty
    | ListType
    | RecordType
    | UnionType

interface ListType {
    tag: 'list'
    type: Type
}

interface RecordType {
    tag: 'record'
    name: string
    fields: TypedItem[]
}

interface UnionType {
    tag: 'union'
    name: string
    cases: TypedItem[]
}

interface TypedItem {
    name: string
    type: Type
}

interface Fun {
    name: string
    ret: Type
    params: TypedItem[]
}



interface TypedInputState { }

class TypedInput extends React.Component<TypedItem, TypedInputState> {
    constructor(props: TypedItem) {
        super(props);
        this.state = {};
    }
    render() {
        switch (this.props.type) {
            case "int":
            case "float":
            case "bool":
            case "string":
                return <span>{this.props.name} : <input></input></span>;
            default:
                if (Array.isArray(this.props.type)) {
                    return JSON.stringify(this.props.type);
                }
                switch (this.props.type.tag) {
                    case 'record':
                        return (<div>
                            <div>{this.props.name} : {this.props.type.name}</div>
                            {this.props.type.fields.map(item =>
                                <div><TypedInput name={item.name} type={item.type} /></div>
                            )}
                        </div>);
                    case 'union':
                        return (<div>
                            {this.props.name} :
                            <select>
                                {this.props.type.cases.map(item =>
                                    <option value={item.name}>{item.name}</option>)}
                            </select>
                        </div>);
                    default:
                        return JSON.stringify(this.props.type);
                }
        }
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
