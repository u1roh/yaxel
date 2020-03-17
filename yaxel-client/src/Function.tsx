import React from 'react';
import TypedInput from './TypedInput'
import * as yaxel from './yaxel'

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
