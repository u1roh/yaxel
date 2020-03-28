import React from 'react';
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

interface FunctionProps {
    name: string
}

interface FunctionState {
    func: yaxel.Fun | null;
    args: any[];
    result: any;
}

class Function extends React.Component<FunctionProps, FunctionState> {
    private breathCount: number = 0;
    constructor(props: FunctionProps) {
        super(props);
        this.state = { func: null, args: new Array<any>(0), result: null };
    }
    private invoke(args: any[]): Promise<any> {
        return api.invokeFunction(this.props.name, args)
    }
    private updateArgs(args: any[]) {
        this.invoke(args).then(result => this.setState({
            func: this.state.func,
            result: result
        }));
    }
    private async fetchFunction() {
        const fun = await api.fetchFunction(this.props.name);
        const args = yaxel.defaultArgsOf(fun);
        const result = await this.invoke(args);
        const state: FunctionState = { func: fun, args: args, result: result };
        this.setState(state);
    }
    componentDidMount() {
        this.fetchFunction();
    }
    async componentWillUpdate() {
        const breath = await api.fetchBreathCount();
        if (breath !== this.breathCount) {
            console.log("breath = " + breath);
            this.breathCount = breath;
            this.fetchFunction();
        }
    }
    render() {
        return (
            <div className="Function">
                <hr></hr>
                <h2>'<span className="Function-name">{this.state.func?.name}</span>' function</h2>
                <h3>params</h3>
                {
                    this.state.func === null ? <span></span> :
                        <FunArgsInput
                            params={this.state.func.params}
                            args={yaxel.defaultArgsOf(this.state.func)}
                            onSubmit={x => this.updateArgs(x)}></FunArgsInput>
                }
                <h3>return</h3>
                <p>{JSON.stringify(this.state.result)}</p>
            </div>
        );
    }
}

export default Function;
