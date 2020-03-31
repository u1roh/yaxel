import React, { useState, useEffect } from 'react';
import TypedInput from './TypedInput'
import * as yaxel from './yaxel'
import * as api from './api'

interface FunArgInputProps {
    params: yaxel.TypedItem[];
    args: any[];
    onSubmit: ((args: any[]) => void);
}

class FunArgsInput extends React.Component<FunArgInputProps, { args: any[] }> {
    constructor(props: FunArgInputProps) {
        super(props);
        console.log("FunArgsInput constructor");
        this.state = { args: props.args };
    }
    private update(args: any[]) {
        this.setState({ args: args });
        this.props.onSubmit(this.state.args);
    }
    private onChange(i: number, x: any) {
        console.log("FunArgsInput#inChange(" + i + ", " + JSON.stringify(x) + ")");
        const args = this.state.args;
        args[i] = x;
        this.update(args);
    }
    render() {
        return (<form>
            {this.props.params?.map((p, i) => <TypedInput name={p.name} type={p.type} value={this.state.args[i]} onChange={(x) => this.onChange(i, x)} />)}
        </form>);
    }
}

function Function(props: { func: yaxel.Fun }) {
    const [args, setArgs] = useState(yaxel.defaultArgsOf(props.func));
    const [result, setResult] = useState(null as any);
    const updateArgs = async (args: any[]) => {
        setArgs(args);
        setResult(await api.invokeFunction(props.func.name, args));
    }
    return (
        <div className="Function">
            <hr></hr>
            <h2>'<span className="Function-name">{props.func.name}</span>' function</h2>
            <h3>params</h3>
            {
                <FunArgsInput
                    params={props.func.params}
                    args={args}
                    onSubmit={updateArgs}></FunArgsInput>
            }
            <h3>return</h3>
            <p>{JSON.stringify(result)}</p>
        </div>
    );
}

export default Function;
