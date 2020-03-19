import React from 'react';
import TypedInput from './TypedInput'
import * as yaxel from './yaxel'

interface FunArgInputProps {
    params: yaxel.TypedItem[] | undefined;
    onSubmit: ((args: any[]) => void);
}

class FunArgsInput extends React.Component<FunArgInputProps, { args: any[] }> {
    constructor(props: FunArgInputProps) {
        super(props);
        this.state = { args: new Array<any>(props.params?.length) };

    }
    private onChange(i: number, x: any) {
        console.log("FunArgsInput#inChange(" + i + ", " + JSON.stringify(x) + ")");
        const state = this.state;
        state.args[i] = x;
        this.setState(state);
    }
    render() {
        return (<form>
            {this.props.params?.map((p, i) => <TypedInput name={p.name} type={p.type} onChange={(x) => this.onChange(i, x)} />)}
            <input type="button" onClick={(e) => this.props.onSubmit(this.state.args)} value="Execute"></input>
        </form>);
    }
}

interface FunctionProps {
    name: string
}

interface FunctionState {
    func: yaxel.Fun | null
}

class Function extends React.Component<FunctionProps, FunctionState> {
    constructor(props: FunctionProps) {
        super(props);
        this.state = { func: null };
    }
    private invoke(value: any) {
        console.log("Function#invoke(" + value + ")");
    }
    componentDidMount() {
        fetch('function/' + this.props.name)
            .then(response => response?.text())
            .then(t => this.setState({ func: JSON.parse(t) }));
    }
    render() {
        return (
            <div className="Function">
                <hr></hr>
                <h2>'<span className="Function-name">{this.state.func?.name}</span>' function</h2>
                <h3>params</h3>
                <FunArgsInput params={this.state.func?.params} onSubmit={this.invoke}></FunArgsInput>
                <h3>return</h3>
                <p>{JSON.stringify(this.state.func?.ret)}</p>
            </div>
        );
    }
}

export default Function;
